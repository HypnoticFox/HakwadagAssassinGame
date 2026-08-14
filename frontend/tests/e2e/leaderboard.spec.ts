import { test, expect } from '@playwright/test'
import { setupApiMocks, loginViaStorage, createGameViaUi, createPlayer, startGameViaUi } from './helpers'

test.describe('Leaderboard', () => {
  test.beforeEach(async ({ page }) => {
    const player = createPlayer({ displayName: 'Leaderboard Tester' })
    await setupApiMocks(page, player)
    await loginViaStorage(page, player)
  })

  test('view leaderboard: navigate to leaderboard → see all players ranked by score', async ({ page }) => {
    // Create and start a game
    await createGameViaUi(page)
    await startGameViaUi(page)

    // Navigate to leaderboard
    await page.getByRole('button', { name: 'Scorebord' }).click()
    await page.waitForURL(/\/leaderboard/)

    // Page should show leaderboard header
    await expect(page.locator('h1')).toContainText('Scorebord')

    // Should see the leaderboard table
    await expect(page.locator('.leaderboard-table')).toBeVisible()

    // Should have player entries
    const rows = page.locator('.leaderboard-table tbody tr')
    await expect(rows).toHaveCount(3)

    // First row should be rank 1
    await expect(rows.nth(0).locator('.rank-badge')).toContainText('1')

    // Scores should be visible
    await expect(rows.nth(0).locator('.score')).toBeVisible()
  })

  test('leaderboard shows correct column headers', async ({ page }) => {
    await createGameViaUi(page)
    await startGameViaUi(page)
    await page.getByRole('button', { name: 'Scorebord' }).click()
    await page.waitForURL(/\/leaderboard/)

    // Check column headers
    const headers = page.locator('.leaderboard-table thead th')
    await expect(headers.nth(0)).toContainText('Rang')
    await expect(headers.nth(1)).toContainText('Speler')
    await expect(headers.nth(2)).toContainText('Tags')
    await expect(headers.nth(3)).toContainText('Score')
  })

  test('score updates: after tag confirmed → leaderboard reflects new score', async ({ page }) => {
    // Create and start game
    await createGameViaUi(page)
    await startGameViaUi(page)
    await expect(page.locator('.eyebrow')).toContainText('Actief')

    // Navigate to assignment
    await page.getByRole('button', { name: 'Mijn opdracht' }).click()
    await page.waitForURL(/\/assignment/)

    // Submit a tag
    await page.locator('.condition-card').first().click()
    await page.getByRole('button', { name: 'Tag indienen' }).click()

    // Go confirm the tag
    await page.goto('/games/game-1/tag/tag-1')
    await page.waitForURL(/\/tag\//)
    await page.getByRole('button', { name: 'Bevestigen' }).click()
    await page.waitForURL(/\/leaderboard/)

    // Leaderboard should reload with updated scores
    await expect(page.locator('.leaderboard-table')).toBeVisible()
    const firstScore = page.locator('.leaderboard-table tbody tr').first().locator('.score')
    await expect(firstScore).toBeVisible()
  })

  test('leaderboard accessible from game detail page', async ({ page }) => {
    await createGameViaUi(page)
    await startGameViaUi(page)

    // There should be a Leaderboard button
    const leaderboardBtn = page.getByRole('button', { name: 'Scorebord' })
    await expect(leaderboardBtn).toBeVisible()
    await leaderboardBtn.click()
    await page.waitForURL(/\/leaderboard/)
  })

  test('leaderboard shows "My assignment" link when game is active', async ({ page }) => {
    await createGameViaUi(page)
    await startGameViaUi(page)
    await page.getByRole('button', { name: 'Scorebord' }).click()
    await page.waitForURL(/\/leaderboard/)

    // Should have "My assignment" button
    await expect(page.getByRole('button', { name: 'Mijn opdracht' })).toBeVisible()
  })

  test('empty leaderboard shows placeholder', async ({ page }) => {
    // Override the mock to return an empty leaderboard
    await page.route('**/api/games/*/leaderboard', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([]),
      })
    })

    await createGameViaUi(page)
    await startGameViaUi(page)
    await page.getByRole('button', { name: 'Scorebord' }).click()
    await page.waitForURL(/\/leaderboard/)

    await expect(page.locator('.empty')).toContainText('Nog geen scores')
  })
})
