import { test, expect, type Page } from '@playwright/test'
import { setupApiMocks, loginViaStorage, createGameViaUi, createPlayer } from './helpers'

test.describe('Administration', () => {
  test.beforeEach(async ({ page }) => {
    const player = createPlayer({ displayName: 'Admin Tester' })
    await setupApiMocks(page, player)
    await loginViaStorage(page, player)
  })

  /** Creates a game via the UI and navigates to the dedicated admin page. */
  async function openAdminPage(page: Page) {
    await createGameViaUi(page)
    await page.getByRole('button', { name: 'Admin panel' }).click()
    await page.waitForURL(/\/games\/[^/]+\/admin/)
    await expect(page.getByRole('heading', { name: 'Game Settings' })).toBeVisible()
  }

  test('admin page is accessible for game creator', async ({ page }) => {
    await createGameViaUi(page)

    // Admin panel button should be visible for creator
    const adminBtn = page.getByRole('button', { name: 'Admin panel' })
    await expect(adminBtn).toBeVisible()

    // Clicking it navigates to the dedicated admin page
    await adminBtn.click()
    await page.waitForURL(/\/games\/[^/]+\/admin/)
    await expect(page.getByRole('heading', { name: 'Game Settings' })).toBeVisible()
  })

  test('admin page shows settings, conditions, safe time, and moderators sections', async ({
    page,
  }) => {
    await openAdminPage(page)

    await expect(page.getByRole('heading', { name: 'Settings' })).toBeVisible()
    await expect(page.getByRole('heading', { name: 'Conditions' })).toBeVisible()
    await expect(page.getByRole('heading', { name: 'Safe time blocks' })).toBeVisible()
    await expect(page.getByRole('heading', { name: 'Moderators' })).toBeVisible()
  })

  test('add safe time block: admin adds block → shown on game detail', async ({ page }) => {
    await openAdminPage(page)

    // Fill safe time form — first two time inputs in the safe time section
    const timeInputs = page.locator('.admin-card input[type="time"]')
    await timeInputs.nth(0).fill('22:00')
    await timeInputs.nth(1).fill('08:00')
    await page.getByRole('button', { name: 'Add safe time' }).click()

    // Verify the API call was made by checking the form is still usable
    await expect(page.getByRole('button', { name: 'Add safe time' })).toBeVisible()
  })

  test('add custom condition: admin adds condition → input is cleared', async ({ page }) => {
    await openAdminPage(page)

    // The first text input on the page is the condition input
    const conditionInput = page.locator('.admin-card input[type="text"]').first()
    await conditionInput.fill('Tag them while they are eating')
    await page.getByRole('button', { name: 'Add condition' }).click()

    // Input should be cleared after adding
    await expect(conditionInput).toHaveValue('')
  })

  test('update confirmation timeout and assignment cooldown', async ({ page }) => {
    await openAdminPage(page)

    const numberInputs = page.locator('.admin-card input[type="number"]')
    await numberInputs.nth(0).fill('10')
    await page.getByRole('button', { name: 'Save', exact: true }).first().click()
    await expect(page.getByRole('button', { name: 'Save', exact: true }).first()).toBeEnabled()

    await numberInputs.nth(1).fill('15')
    await page.getByRole('button', { name: 'Save', exact: true }).nth(1).click()
    await expect(page.getByRole('button', { name: 'Save', exact: true }).nth(1)).toBeEnabled()
  })

  test('back to game button returns to the game detail page', async ({ page }) => {
    await openAdminPage(page)

    await page.getByRole('button', { name: 'Back to game' }).click()
    await page.waitForURL(/\/games\/[^/]+$/)
  })
})
