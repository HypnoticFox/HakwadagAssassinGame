import { expect, test, type Page } from '@playwright/test'

import {
  createGameViaUi,
  createPlayer,
  loginViaStorage,
  setupApiMocks,
  startGameViaUi,
} from './helpers'
import type { SafeTimeBlockDto } from '../../src/types'

test.use({ timezoneId: 'UTC' })

test.describe('Safe time blocks', () => {
  test.beforeEach(async ({ page }) => {
    await page.clock.install()
  })

  async function setupGameWithSafeTime(
    page: Page,
    blocks: SafeTimeBlockDto[],
    timeBeforeReload?: Date,
  ) {
    const player = createPlayer({ displayName: 'Safe Time Tester' })
    const { gamesMap } = await setupApiMocks(page, player)
    await loginViaStorage(page, player)

    await createGameViaUi(page)
    await startGameViaUi(page)

    const gameId = page.url().match(/\/games\/([^/]+)$/)?.[1] ?? ''
    const game = gamesMap.get(gameId)
    if (game) {
      gamesMap.set(gameId, { ...game, safeTimeBlocks: blocks })
    }

    // The setup flow itself can take longer than the five-second boundary case.
    // Reset immediately before loading the updated game so the composable starts
    // inside the safe-time block and can schedule its boundary timer.
    if (timeBeforeReload) {
      await page.clock.setSystemTime(timeBeforeReload)
    }
    await page.reload()
    await page.waitForLoadState('networkidle')

    return gameId
  }

  test('banner shows and assignment button is disabled during safe time', async ({ page }) => {
    await page.clock.setSystemTime(new Date('2025-06-15T23:00:00Z'))
    await setupGameWithSafeTime(page, [{ id: 'st-1', startTime: '2025-06-15T22:00:00+00:00', endTime: '2025-06-15T06:00:00+00:00' }])

    const banner = page.locator('.safe-time-banner')
    await expect(banner).toBeVisible()
    await expect(banner).toContainText('06:00')
    await expect(page.getByRole('button', { name: 'Mijn opdracht' })).toBeDisabled()
  })

  test('banner disappears and button enables when safe time ends', async ({ page }) => {
    const timeBeforeBoundary = new Date('2025-06-15T05:59:55Z')
    await page.clock.setSystemTime(timeBeforeBoundary)
    await setupGameWithSafeTime(
      page,
      [{ id: 'st-1', startTime: '2025-06-15T22:00:00+00:00', endTime: '2025-06-15T06:00:00+00:00' }],
      timeBeforeBoundary,
    )

    await expect(page.locator('.safe-time-banner')).toBeVisible()
    const assignmentButton = page.getByRole('button', { name: 'Mijn opdracht' })
    await expect(assignmentButton).toBeDisabled()

    await page.clock.fastForward(7000)

    await expect(page.locator('.safe-time-banner')).not.toBeVisible()
    await expect(assignmentButton).toBeEnabled()
  })

  test('assignment page shows safe time message during safe time', async ({ page }) => {
    await page.clock.setSystemTime(new Date('2025-06-15T23:00:00Z'))
    const gameId = await setupGameWithSafeTime(page, [
      { id: 'st-1', startTime: '2025-06-15T22:00:00+00:00', endTime: '2025-06-15T06:00:00+00:00' },
    ])

    await page.goto(`/games/${gameId}/assignment`)

    const safeTimeMessage = page.locator('.safe-time-message')
    await expect(safeTimeMessage).toBeVisible()
    await expect(safeTimeMessage).toContainText('Veilige tijd is actief')
    await expect(page.locator('.target-card')).not.toBeVisible()
  })

  test('no banner when no safe time blocks are configured', async ({ page }) => {
    await page.clock.setSystemTime(new Date('2025-06-15T23:00:00Z'))
    await setupGameWithSafeTime(page, [])

    await expect(page.locator('.safe-time-banner')).not.toBeVisible()
    await expect(page.getByRole('button', { name: 'Mijn opdracht' })).toBeEnabled()
  })
})
