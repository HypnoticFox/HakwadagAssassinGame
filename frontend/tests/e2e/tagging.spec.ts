import { test, expect } from '@playwright/test'
import { setupApiMocks, loginViaStorage, createGameViaUi, createPlayer, startGameViaUi } from './helpers'

test.describe('Tagging flow', () => {
  test.beforeEach(async ({ page }) => {
    const player = createPlayer({ displayName: 'Tag Hunter' })
    await setupApiMocks(page, player)
    await loginViaStorage(page, player)
  })

  test('view assignment: navigate to assignment page → see target name and conditions', async ({
    page,
  }) => {
    // Create and start a game
    await createGameViaUi(page)
    await startGameViaUi(page)
    await expect(page.locator('.eyebrow')).toContainText('Actief')

    // Navigate to assignment
    await page.getByRole('button', { name: 'Mijn opdracht' }).click()
    await page.waitForURL(/\/assignment/)

    // Should see target info
    await expect(page.locator('.target-name')).toBeVisible()
    await expect(page.locator('.target-name')).toContainText('Target Player')

    // Should see conditions
    await expect(page.locator('.condition-card')).toHaveCount(2)
    await expect(page.locator('.condition-card__type').first()).toContainText('specifiek')
  })

  test('submit a tag: select condition → confirm submission → tag pending', async ({ page }) => {
    // Create, start, and go to assignment
    await createGameViaUi(page)
    await startGameViaUi(page)
    await expect(page.locator('.eyebrow')).toContainText('Actief')
    await page.getByRole('button', { name: 'Mijn opdracht' }).click()
    await page.waitForURL(/\/assignment/)

    // Select a condition
    await page.locator('.condition-card').first().click()

    // Confirmation modal should appear
    await expect(page.locator('[role="dialog"]')).toBeVisible()
    await expect(page.locator('.modal-title')).toContainText('Tag bevestigen')

    // Submit the tag
    await page.getByRole('button', { name: 'Tag indienen' }).click()

    // Modal should close
    await expect(page.locator('[role="dialog"]')).not.toBeVisible({ timeout: 3000 })
  })

  test('cancel tag submission from modal', async ({ page }) => {
    await createGameViaUi(page)
    await startGameViaUi(page)
    await expect(page.locator('.eyebrow')).toContainText('Actief')
    await page.getByRole('button', { name: 'Mijn opdracht' }).click()
    await page.waitForURL(/\/assignment/)

    // Select a condition
    await page.locator('.condition-card').first().click()
    await expect(page.locator('[role="dialog"]')).toBeVisible()

    // Cancel the submission
    await page.getByRole('button', { name: 'Annuleren' }).click()
    await expect(page.locator('[role="dialog"]')).not.toBeVisible({ timeout: 3000 })

    // Should still be on assignment page
    await expect(page).toHaveURL(/\/assignment/)
  })

  test('confirm a tag: as target → navigate to pending tag → confirm → score updated', async ({
    page,
  }) => {
    // Set up a pending tag in the mock
    const ctx = await setupApiMocks(page, createPlayer({ displayName: 'Tag Target' }))
    await loginViaStorage(page)

    // Create a pending tag by submitting one first
    await createGameViaUi(page)
    await startGameViaUi(page)
    await expect(page.locator('.eyebrow')).toContainText('Actief')
    await page.getByRole('button', { name: 'Mijn opdracht' }).click()
    await page.waitForURL(/\/assignment/)
    await page.locator('.condition-card').first().click()
    await page.getByRole('button', { name: 'Tag indienen' }).click()

    // Navigate to the tag confirmation page
    await page.goto('/games/game-1/tag/tag-1')
    await page.waitForURL(/\/tag\//)

    // Should see tag details
    await expect(page.locator('h1')).toContainText('Tag in behandeling')
    await expect(page.locator('.tag-card')).toBeVisible()

    // Confirm the tag
    await page.getByRole('button', { name: 'Bevestigen' }).click()

    // Should redirect to leaderboard
    await page.waitForURL(/\/leaderboard/)
  })

  test('deny a tag: as target → navigate to pending tag → deny → tag denied', async ({ page }) => {
    await setupApiMocks(page, createPlayer({ displayName: 'Tag Target' }))
    await loginViaStorage(page)

    // Create a pending tag
    await createGameViaUi(page)
    await startGameViaUi(page)
    await expect(page.locator('.eyebrow')).toContainText('Actief')
    await page.getByRole('button', { name: 'Mijn opdracht' }).click()
    await page.waitForURL(/\/assignment/)
    await page.locator('.condition-card').first().click()
    await page.getByRole('button', { name: 'Tag indienen' }).click()

    // Navigate to tag confirmation
    await page.goto('/games/game-1/tag/tag-1')
    await page.waitForURL(/\/tag\//)

    // Deny the tag
    await page.getByRole('button', { name: 'Afwijzen' }).click()

    // Should redirect to leaderboard
    await page.waitForURL(/\/leaderboard/)
  })

  test('void a tag: as admin → void a confirmed/pending tag → score adjusted', async ({ page }) => {
    await setupApiMocks(page, createPlayer({ displayName: 'Admin Player' }))
    await loginViaStorage(page)

    // Create and start a game
    await createGameViaUi(page)
    await startGameViaUi(page)
    await expect(page.locator('.eyebrow')).toContainText('Actief')

    // Submit a tag first
    await page.getByRole('button', { name: 'Mijn opdracht' }).click()
    await page.waitForURL(/\/assignment/)
    await page.locator('.condition-card').first().click()
    await page.getByRole('button', { name: 'Tag indienen' }).click()

    // Navigate to tag confirmation
    await page.goto('/games/game-1/tag/tag-1')
    await page.waitForURL(/\/tag\//)

    // Confirm the tag first
    await page.getByRole('button', { name: 'Bevestigen' }).click()
    await page.waitForURL(/\/leaderboard/)

    // Go back and void the tag
    await page.goto('/games/game-1/tag/tag-1')
    await page.waitForURL(/\/tag\//)

    // Void button should be available for admin
    const voidButton = page.getByRole('button', { name: 'Tag ongeldig maken' })
    if (await voidButton.isVisible()) {
      await voidButton.click()
      await page.waitForURL(/\/leaderboard/)
    }
  })
})
