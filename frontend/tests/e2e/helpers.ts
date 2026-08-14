import { Page, expect } from '@playwright/test'
import type {
  GameDto,
  PlayerDto,
  AssignmentDto,
  ConditionDto,
  TagSubmissionDto,
  LeaderboardEntryDto,
} from '../../src/types/index'

// ---------------------------------------------------------------------------
// Test data factories
// ---------------------------------------------------------------------------

export const TEST_EMAIL = 'player@test.com'
export const TEST_OTP_CODE = '123456'
export const TEST_TOKEN = 'test-jwt-token-abc123'
export const INVITE_CODE = 'ABC123'

let playerCounter = 0

export function createPlayer(overrides?: Partial<PlayerDto>): PlayerDto {
  playerCounter++
  return {
    id: `player-${playerCounter}`,
    email: TEST_EMAIL,
    displayName: 'Test Player',
    ...overrides,
  }
}

let gameCounter = 0

export function createGameDto(overrides?: Partial<GameDto>): GameDto {
  gameCounter++
  return {
    id: `game-${gameCounter}`,
    name: `Test Game ${gameCounter}`,
    inviteCode: INVITE_CODE,
    status: 0, // NotStarted
    createdAt: new Date().toISOString(),
    maxPlayers: 20,
    basePointsPerTag: 100,
    confirmationTimeout: '00:05:00',
    assignmentCooldownMinutes: 30,
    playerCount: 1,
    participantCount: 1,
    isParticipating: true,
    myRole: 1, // Creator
    safeTimeBlocks: [],
    ...overrides,
  }
}

export function createAssignment(targetName = 'Target Player'): AssignmentDto {
  return {
    id: `assignment-${Date.now()}`,
    target: {
      id: 'target-1',
      displayName: targetName,
    },
    conditions: [
      {
        id: 'condition-1',
        type: 0, // WithSpecificPerson
        description: 'Tag them while they are with Jane',
        targetPersonName: 'Jane',
      },
      {
        id: 'condition-2',
        type: 1, // Alone
        description: 'Tag them when they are alone',
      },
    ],
    assignedAt: new Date().toISOString(),
  }
}

export function createTagSubmission(overrides?: Partial<TagSubmissionDto>): TagSubmissionDto {
  return {
    id: 'tag-1',
    assignmentId: 'assignment-1',
    hunterId: 'player-1',
    targetId: 'target-1',
    conditionId: 'condition-1',
    status: 0, // Pending
    submittedAt: new Date().toISOString(),
    ...overrides,
  }
}

export function createLeaderboardEntries(count = 3): LeaderboardEntryDto[] {
  return Array.from({ length: count }, (_, i) => ({
    player: {
      id: `lb-player-${i + 1}`,
      email: `player${i + 1}@test.com`,
      displayName: `Player ${i + 1}`,
    },
    score: (count - i) * 100,
    tags: count - i,
  }))
}

// ---------------------------------------------------------------------------
// Mock API routes
// ---------------------------------------------------------------------------

/**
 * Sets up route interception to mock all backend API calls.
 * Call this in each test's `beforeEach`.
 */
export async function setupApiMocks(page: Page, player?: PlayerDto) {
  const currentPlayer = player ?? createPlayer()
  const storedToken = { value: TEST_TOKEN }

  // Helper to check auth header
  function isAuthenticated(request: { headers: () => Record<string, string> }) {
    const auth = request.headers()['authorization'] ?? ''
    return auth === `Bearer ${TEST_TOKEN}`
  }

  // ---------- Auth endpoints ----------

  await page.route('**/api/auth/send-otp', async (route) => {
    const body = JSON.parse(route.request().postData() || '{}')
    if (body.email === TEST_EMAIL) {
      await route.fulfill({ status: 200 })
    } else {
      await route.fulfill({
        status: 400,
        contentType: 'application/json',
        body: JSON.stringify({ message: 'Invalid email address' }),
      })
    }
  })

  await page.route('**/api/auth/verify-otp', async (route) => {
    const body = JSON.parse(route.request().postData() || '{}')
    if (body.email === TEST_EMAIL && body.code === TEST_OTP_CODE) {
      await page.evaluate((t) => localStorage.setItem('hakwadag_token', t), TEST_TOKEN)
      storedToken.value = TEST_TOKEN
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ token: TEST_TOKEN, player: currentPlayer }),
      })
    } else {
      await route.fulfill({
        status: 401,
        contentType: 'application/json',
        body: JSON.stringify({ message: 'Invalid or expired code' }),
      })
    }
  })

  await page.route('**/api/auth/me', async (route) => {
    if (isAuthenticated(route.request())) {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(currentPlayer),
      })
    } else {
      await route.fulfill({ status: 401 })
    }
  })

  // ---------- Game endpoints ----------

  const gamesMap = new Map<string, GameDto>()

  await page.route('**/api/games', async (route, request) => {
    if (!isAuthenticated(request)) {
      await route.fulfill({ status: 401 })
      return
    }

    if (request.method() === 'POST') {
      const body = JSON.parse(request.postData() || '{}')
      const game = createGameDto({
        name: body.name || 'Test Game',
        maxPlayers: body.maxPlayers || 20,
        basePointsPerTag: body.basePointsPerTag || 100,
        confirmationTimeout: `00:${String(body.confirmationTimeoutMinutes || 5).padStart(2, '0')}:00`,
        durationHours: body.durationHours || 24,
      })
      gamesMap.set(game.id, game)
      await route.fulfill({
        status: 201,
        contentType: 'application/json',
        body: JSON.stringify(game),
      })
    } else if (request.method() === 'GET') {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(Array.from(gamesMap.values())),
      })
    } else {
      await route.fulfill({ status: 405 })
    }
  })

  await page.route('**/api/games/join/**', async (route, request) => {
    if (!isAuthenticated(request)) {
      await route.fulfill({ status: 401 })
      return
    }

    const url = request.url()
    const code = url.split('/join/').pop()?.split('?')[0] ?? ''

    if (code === INVITE_CODE) {
      const game = createGameDto({
        playerCount: 2,
        myRole: 0, // Player
      })
      gamesMap.set(game.id, game)
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(game),
      })
    } else {
      await route.fulfill({
        status: 404,
        contentType: 'application/json',
        body: JSON.stringify({ message: 'Invalid invite code' }),
      })
    }
  })

  await page.route(/\/api\/games\/(?!join\/)([^/]+)$/, async (route, request) => {
    if (!isAuthenticated(request)) {
      await route.fulfill({ status: 401 })
      return
    }

    const match = request.url().match(/\/api\/games\/([^/]+)$/)
    const gameId = match?.[1] ?? ''
    const game = gamesMap.get(gameId) ?? createGameDto({ id: gameId })
    gamesMap.set(game.id, game)
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(game),
    })
  })

  // Game actions: start, end, leave
  await page.route(/\/api\/games\/([^/]+)\/(start|end|leave)/, async (route, request) => {
    if (!isAuthenticated(request)) {
      await route.fulfill({ status: 401 })
      return
    }

    const match = request.url().match(/\/api\/games\/([^/]+)\/(start|end|leave)/)
    const gameId = match?.[1] ?? ''
    const action = match?.[2] ?? ''
    let game = gamesMap.get(gameId)
    if (!game) {
      game = createGameDto({ id: gameId })
      gamesMap.set(gameId, game)
    }

    if (action === 'start') {
      game = { ...game, status: 1 } // Active
      gamesMap.set(gameId, game)
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(game),
      })
    } else if (action === 'end') {
      game = { ...game, status: 2 } // Ended
      gamesMap.set(gameId, game)
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(game),
      })
    } else if (action === 'leave') {
      gamesMap.delete(gameId)
      await route.fulfill({ status: 204 })
    } else {
      await route.fulfill({ status: 400 })
    }
  })

  // ---------- Assignment endpoints ----------

  await page.route('**/api/games/*/assignments/me', async (route, request) => {
    if (!isAuthenticated(request)) {
      await route.fulfill({ status: 401 })
      return
    }
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(createAssignment()),
    })
  })

  // ---------- Tag endpoints ----------

  const tagMap = new Map<string, TagSubmissionDto>()

  await page.route('**/api/games/*/tag/pending', async (route, request) => {
    if (!isAuthenticated(request)) {
      await route.fulfill({ status: 401 })
      return
    }
    const pending = Array.from(tagMap.values()).find((t) => t.status === 0)
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(pending ?? null),
    })
  })

  await page.route('**/api/games/*/tag', async (route, request) => {
    if (!isAuthenticated(request)) {
      await route.fulfill({ status: 401 })
      return
    }

    if (request.method() === 'POST') {
      // Check for duplicate pending tag
      const hasPending = Array.from(tagMap.values()).some((t) => t.status === 0)
      if (hasPending) {
        await route.fulfill({
          status: 409,
          contentType: 'application/json',
          body: JSON.stringify({ message: 'A tag is already pending' }),
        })
        return
      }

      const tag = createTagSubmission()
      tagMap.set(tag.id, tag)
      await route.fulfill({
        status: 201,
        contentType: 'application/json',
        body: JSON.stringify(tag),
      })
    } else {
      await route.fulfill({ status: 405 })
    }
  })

  await page.route(
    /\/api\/games\/([^/]+)\/tag\/([^/]+)\/(confirm|deny|void)/,
    async (route, request) => {
      if (!isAuthenticated(request)) {
        await route.fulfill({ status: 401 })
        return
      }

      const match = request.url().match(/\/api\/games\/([^/]+)\/tag\/([^/]+)\/(confirm|deny|void)/)
      const tagId = match?.[2] ?? ''
      const action = match?.[3] ?? ''
      let tag = tagMap.get(tagId) ?? createTagSubmission({ id: tagId })
      tagMap.set(tag.id, tag)

      if (action === 'confirm') {
        tag = { ...tag, status: 1, resolvedAt: new Date().toISOString() }
      } else if (action === 'deny') {
        tag = { ...tag, status: 2, resolvedAt: new Date().toISOString() }
      } else if (action === 'void') {
        tag = { ...tag, status: 3, resolvedAt: new Date().toISOString() }
      }
      tagMap.set(tag.id, tag)

      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(tag),
      })
    },
  )

  // ---------- Leaderboard endpoints ----------

  await page.route('**/api/games/*/leaderboard', async (route, request) => {
    if (!isAuthenticated(request)) {
      await route.fulfill({ status: 401 })
      return
    }
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(createLeaderboardEntries(3)),
    })
  })

  // ---------- Admin endpoints ----------

  await page.route('**/api/games/*/players', async (route, request) => {
    if (!isAuthenticated(request)) {
      await route.fulfill({ status: 401 })
      return
    }
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify([
        {
          playerId: currentPlayer.id,
          displayName: currentPlayer.displayName,
          email: currentPlayer.email,
          role: 1, // Creator
        },
      ]),
    })
  })

  await page.route('**/api/games/*/admin/confirmation-timeout', async (route, request) => {
    if (!isAuthenticated(request)) {
      await route.fulfill({ status: 401 })
      return
    }
    if (request.method() === 'PUT') {
      const body = JSON.parse(request.postData() || '{}')
      const gameId = request.url().match(/\/api\/games\/([^/]+)\/admin/)?.[1] ?? ''
      const game = gamesMap.get(gameId) ?? createGameDto({ id: gameId })
      const minutes = Number(body.minutes ?? 5)
      const hours = Math.floor(minutes / 60)
      const mins = minutes % 60
      const updated: GameDto = {
        ...game,
        confirmationTimeout: `${String(hours).padStart(2, '0')}:${String(mins).padStart(2, '0')}:00`,
      }
      gamesMap.set(gameId, updated)
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(updated),
      })
    } else {
      await route.fulfill({ status: 405 })
    }
  })

  await page.route('**/api/games/*/admin/assignment-cooldown', async (route, request) => {
    if (!isAuthenticated(request)) {
      await route.fulfill({ status: 401 })
      return
    }
    if (request.method() === 'PUT') {
      const body = JSON.parse(request.postData() || '{}')
      const gameId = request.url().match(/\/api\/games\/([^/]+)\/admin/)?.[1] ?? ''
      const game = gamesMap.get(gameId) ?? createGameDto({ id: gameId })
      const updated: GameDto = {
        ...game,
        assignmentCooldownMinutes: Number(body.minutes ?? 30),
      }
      gamesMap.set(gameId, updated)
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(updated),
      })
    } else {
      await route.fulfill({ status: 405 })
    }
  })

  await page.route('**/api/games/*/admins', async (route, request) => {
    if (!isAuthenticated(request)) {
      await route.fulfill({ status: 401 })
      return
    }
    if (request.method() === 'POST') {
      await route.fulfill({ status: 200 })
    } else {
      await route.fulfill({ status: 405 })
    }
  })

  await page.route(/\/api\/games\/([^/]+)\/admins\/([^/]+)/, async (route, request) => {
    if (!isAuthenticated(request)) {
      await route.fulfill({ status: 401 })
      return
    }
    if (request.method() === 'DELETE') {
      await route.fulfill({ status: 204 })
    } else {
      await route.fulfill({ status: 405 })
    }
  })

  await page.route('**/api/games/*/safe-times', async (route, request) => {
    if (!isAuthenticated(request)) {
      await route.fulfill({ status: 401 })
      return
    }
    if (request.method() === 'POST') {
      await route.fulfill({
        status: 201,
        contentType: 'application/json',
        body: JSON.stringify({ blockId: 'safe-time-1' }),
      })
    } else {
      await route.fulfill({ status: 405 })
    }
  })

  await page.route(/\/api\/games\/([^/]+)\/safe-times\/([^/]+)/, async (route, request) => {
    if (!isAuthenticated(request)) {
      await route.fulfill({ status: 401 })
      return
    }
    if (request.method() === 'DELETE') {
      await route.fulfill({ status: 204 })
    } else {
      await route.fulfill({ status: 405 })
    }
  })

  await page.route('**/api/games/*/conditions', async (route, request) => {
    if (!isAuthenticated(request)) {
      await route.fulfill({ status: 401 })
      return
    }
    if (request.method() === 'POST') {
      const body = JSON.parse(request.postData() || '{}')
      const condition: ConditionDto = {
        id: 'custom-condition-1',
        type: 4, // Custom
        description: body.description || 'Custom condition',
      }
      await route.fulfill({
        status: 201,
        contentType: 'application/json',
        body: JSON.stringify(condition),
      })
    } else {
      await route.fulfill({ status: 405 })
    }
  })

  // ---------- Push endpoints ----------

  await page.route('**/api/push/vapid-public-key', async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        publicKey: 'BNd8P3qLuGKr5Lm6EwNhG7YxRfIkJzQvSgWjTnMpVkHc9Fb2A4sD6fHgJkLzXcVbNm',
      }),
    })
  })

  await page.route('**/api/push/subscribe', async (route) => {
    await route.fulfill({ status: 200 })
  })

  // ---------- SignalR hub (return 404 to avoid errors) ----------

  await page.route('**/hubs/game*', async (route) => {
    await route.fulfill({ status: 404 })
  })

  return { player: currentPlayer, gamesMap, tagMap, storedToken }
}

// ---------------------------------------------------------------------------
// Auth helpers – higher-level flows for re-use in tests
// ---------------------------------------------------------------------------

/** Log the test player in via the UI (visit /login → send OTP → verify). */
export async function loginViaUi(page: Page, email = TEST_EMAIL, code = TEST_OTP_CODE) {
  await page.goto('/login')
  await page.waitForSelector('h1')

  // Step 1: enter email
  const emailInput = page.locator('input[type="email"]')
  await emailInput.fill(email)
  await page.getByRole('button', { name: 'Code versturen' }).click()

  // Step 2: enter OTP code
  await page.waitForSelector('input[inputmode="numeric"]')
  const codeInput = page.locator('input[inputmode="numeric"]')
  await codeInput.fill(code)
  await page.getByRole('button', { name: 'Verifiëren' }).click()

  // Should land on home
  await expect(page).toHaveURL('/')
}

/** Set auth token and player in localStorage, then visit the given path. */
export async function loginViaStorage(page: Page, player?: PlayerDto) {
  const p = player ?? createPlayer()
  await page.goto('/')
  await page.evaluate(
    ({ token, player }) => {
      localStorage.setItem('hakwadag_token', token)
      localStorage.setItem('pinia_auth', JSON.stringify({ token, player }))
    },
    { token: TEST_TOKEN, player: p },
  )
  // Reload so the app reads the stored token
  await page.reload()
  await page.waitForLoadState('networkidle')
  return p
}

// ---------------------------------------------------------------------------
// Game helpers
// ---------------------------------------------------------------------------

export async function createGameViaUi(page: Page) {
  await page.goto('/games/create')
  await page.waitForSelector('h1')

  // Use positional locators because Input component doesn't associate label with input via for/id
  const inputs = page.locator('.create-form input')
  await inputs.nth(0).fill('Friday Night Assassin')
  await inputs.nth(1).fill('48')
  await inputs.nth(2).fill('10')
  await inputs.nth(3).fill('100')
  await inputs.nth(4).fill('5')
  await page.getByRole('button', { name: 'Spel aanmaken' }).click()

  await page.waitForURL(/\/games\//)
  return page.url()
}

export async function joinGameViaUi(page: Page, inviteCode = INVITE_CODE) {
  await page.goto('/')
  await page.waitForSelector('h1')

  await page.getByRole('button', { name: 'Deelnemen aan spel' }).click()
  await page.waitForSelector('h2')

  const inputs = page.locator('.modal-body input')
  await inputs.nth(0).fill(inviteCode)
  await inputs.nth(1).fill('Joining Player')
  await page.getByRole('button', { name: 'Deelnemen', exact: true }).click()

  await page.waitForURL(/\/games\//)
  return page.url()
}

/**
 * Clicks the "Start game" button and accepts the confirmation dialog.
 */
export async function startGameViaUi(page: Page) {
  page.once('dialog', (dialog) => dialog.accept())
  await page.getByRole('button', { name: 'Spel starten' }).click()
}
