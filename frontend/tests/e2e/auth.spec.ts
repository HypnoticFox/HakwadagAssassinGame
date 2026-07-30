import { test, expect } from '@playwright/test'
import { setupApiMocks, loginViaUi, loginViaStorage, TEST_EMAIL, TEST_OTP_CODE, TEST_TOKEN, createPlayer } from './helpers'

test.describe('Authentication flow', () => {
  test.beforeEach(async ({ page }) => {
    await setupApiMocks(page)
  })

  test('full login flow: visit /login → enter email → receive OTP → enter OTP → redirected to home', async ({ page }) => {
    await loginViaUi(page)

    // Should be on home page
    await expect(page.locator('h1')).toContainText('Welcome')
    // Token should be persisted
    const storedToken = await page.evaluate(() => localStorage.getItem('hakwadag_token'))
    expect(storedToken).toBe(TEST_TOKEN)
  })

  test('unauthenticated redirect: visit / while not logged in → redirected to /login?redirect=/', async ({ page }) => {
    await page.goto('/')
    await page.waitForURL(/\/login/)
    const url = page.url()
    expect(url).toContain('/login')
    expect(url).toContain('redirect=/')
  })

  test('unauthenticated redirect preserves the attempted path', async ({ page }) => {
    await page.goto('/games/some-game-id')
    await page.waitForURL(/\/login/)
    expect(page.url()).toContain('redirect=/games/some-game-id')
  })

  test('authenticated users on guest routes are redirected to home', async ({ page }) => {
    await loginViaStorage(page)
    await page.goto('/login')
    await expect(page).toHaveURL('/')
  })

  test('login with invalid email shows error', async ({ page }) => {
    await page.goto('/login')
    await page.waitForSelector('h1')

    const emailInput = page.locator('input[type="email"]')
    await emailInput.fill('wrong@test.com')
    await page.getByRole('button', { name: 'Send code' }).click()

    await expect(page.locator('[role="alert"]')).toBeVisible()
    // Should still be on the email step
    await expect(page.locator('input[type="email"]')).toBeVisible()
  })

  test('login with invalid OTP code shows error', async ({ page }) => {
    await page.goto('/login')
    await page.waitForSelector('h1')

    // Step 1 - send OTP
    await page.locator('input[type="email"]').fill(TEST_EMAIL)
    await page.getByRole('button', { name: 'Send code' }).click()

    // Step 2 - enter wrong code
    await page.waitForSelector('input[inputmode="numeric"]')
    await page.locator('input[inputmode="numeric"]').fill('000000')
    await page.getByRole('button', { name: 'Verify' }).click()

    await expect(page.locator('[role="alert"]')).toBeVisible()
    // Should stay on login
    expect(page.url()).toContain('/login')
  })

  test('back button on OTP step returns to email step', async ({ page }) => {
    await page.goto('/login')
    await page.waitForSelector('h1')

    // Send OTP first
    await page.locator('input[type="email"]').fill(TEST_EMAIL)
    await page.getByRole('button', { name: 'Send code' }).click()
    await page.waitForSelector('input[inputmode="numeric"]')

    // Click "Use a different email"
    await page.getByRole('button', { name: 'Use a different email' }).click()
    await expect(page.locator('input[type="email"]')).toBeVisible()
  })

  test('token persistence: login → reload → still authenticated', async ({ page }) => {
    await loginViaUi(page)

    // Reload the page
    await page.reload()
    await page.waitForLoadState('networkidle')

    // Should still be on home (not redirected to login)
    await expect(page).toHaveURL('/')
    await expect(page.locator('h1')).toContainText('Welcome')
  })

  test('logout: click logout → redirected to login, protected routes inaccessible', async ({ page }) => {
    await loginViaUi(page)

    // Click the logout button in the nav
    await page.getByRole('button', { name: 'Log out' }).click()

    // Should be redirected to login
    await expect(page).toHaveURL('/login')

    // Protected routes should redirect back to login
    await page.goto('/')
    await page.waitForURL(/\/login/)
    expect(page.url()).toContain('/login')
  })

  test('logout clears auth token from localStorage', async ({ page }) => {
    await loginViaUi(page)

    // Verify token exists
    let storedToken = await page.evaluate(() => localStorage.getItem('hakwadag_token'))
    expect(storedToken).toBeTruthy()

    // Log out
    await page.getByRole('button', { name: 'Log out' }).click()

    // Token should be cleared
    storedToken = await page.evaluate(() => localStorage.getItem('hakwadag_token'))
    expect(storedToken).toBeNull()
  })

  test('login with redirect parameter navigates to original destination', async ({ page }) => {
    // Visit a protected route
    await page.goto('/games/create')
    await page.waitForURL(/\/login/)

    // Login
    await page.locator('input[type="email"]').fill(TEST_EMAIL)
    await page.getByRole('button', { name: 'Send code' }).click()
    await page.locator('input[inputmode="numeric"]').fill(TEST_OTP_CODE)
    await page.getByRole('button', { name: 'Verify' }).click()

    // Should redirect to create game page (based on the redirect param in the URL)
    await expect(page).toHaveURL(/\/games\/create/)
  })
})
