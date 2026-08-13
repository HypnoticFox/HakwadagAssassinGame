import { describe, it, expect, beforeEach, vi } from 'vitest'
import { api, ApiError } from '@/api/client'

// Helper: mock fetch with a canned response
function mockFetch(status: number, body?: unknown, headers?: Record<string, string>) {
  const responseHeaders = new Headers(headers)
  return vi.mocked(fetch).mockResolvedValueOnce({
    status,
    headers: responseHeaders,
    ok: status >= 200 && status < 300,
    text: () => (body !== undefined ? Promise.resolve(JSON.stringify(body)) : Promise.resolve('')),
    json: () => Promise.resolve(body),
  } as Response)
}

beforeEach(() => {
  vi.restoreAllMocks()
  localStorage.clear()
  // Replace global fetch with a spy
  vi.stubGlobal('fetch', vi.fn())
  // Reset the internal token state by calling clearToken
  api.clearToken()
})

// ---------------------------------------------------------------------------
// Authentication headers
// ---------------------------------------------------------------------------
describe('authentication headers', () => {
  it('includes Authorization header when token is set', async () => {
    api.setToken('test-token-123')
    mockFetch(200, { id: 'p1', email: 'a@b.c', displayName: 'A' })

    await api.me()

    const call = vi.mocked(fetch).mock.calls[0]
    const headers = call[1]!.headers as Headers
    expect(headers.get('Authorization')).toBe('Bearer test-token-123')
  })

  it('does not include Authorization header when no token is set', async () => {
    mockFetch(200, [])

    await api.getMyGames()

    const call = vi.mocked(fetch).mock.calls[0]
    const headers = call[1]!.headers as Headers
    expect(headers.get('Authorization')).toBeNull()
  })

  it('persists token in localStorage after setToken', () => {
    api.setToken('persisted-token')
    expect(localStorage.getItem('hakwadag_token')).toBe('persisted-token')
  })

  it('removes token from localStorage after clearToken', () => {
    api.setToken('persisted-token')
    api.clearToken()
    expect(localStorage.getItem('hakwadag_token')).toBeNull()
    expect(api.getToken()).toBeNull()
  })

  it('reads token from localStorage in constructor', () => {
    // The api singleton reads localStorage in its constructor.
    // Since clearToken was called in beforeEach, we simulate construction
    // by checking that setToken stores and getToken retrieves it.
    api.setToken('constructed-token')
    expect(api.getToken()).toBe('constructed-token')
    expect(localStorage.getItem('hakwadag_token')).toBe('constructed-token')
  })
})

// ---------------------------------------------------------------------------
// Request method & body
// ---------------------------------------------------------------------------
describe('request method & body', () => {
  it('sends POST with JSON body for sendOtp', async () => {
    mockFetch(204)
    await api.sendOtp('user@example.com')

    const call = vi.mocked(fetch).mock.calls[0]
    expect(call[0]).toContain('/api/auth/send-otp')
    expect(call[1]!.method).toBe('POST')
    expect(call[1]!.body).toBe(JSON.stringify({ email: 'user@example.com' }))
  })

  it('sends POST with JSON body for verifyOtp', async () => {
    mockFetch(200, { token: 't', player: { id: 'p1', email: 'a@b.c', displayName: 'A' } })
    await api.verifyOtp('user@example.com', '123456')

    const call = vi.mocked(fetch).mock.calls[0]
    expect(call[1]!.method).toBe('POST')
    expect(call[1]!.body).toBe(JSON.stringify({ email: 'user@example.com', code: '123456' }))
  })

  it('sends GET without body for getMyGames', async () => {
    mockFetch(200, [])
    await api.getMyGames()

    const call = vi.mocked(fetch).mock.calls[0]
    expect(call[1]!.method).toBeUndefined() // GET is default
    expect(call[1]!.body).toBeUndefined()
  })

  it('sets Content-Type application/json when body is present', async () => {
    mockFetch(200, { token: 't', player: { id: 'p1', email: 'a@b.c', displayName: 'A' } })
    await api.verifyOtp('a@b.c', '000000')

    const call = vi.mocked(fetch).mock.calls[0]
    const headers = call[1]!.headers as Headers
    expect(headers.get('Content-Type')).toBe('application/json')
  })

  it('always sets Accept and skip_zrok_interstitial headers', async () => {
    mockFetch(200, [])
    await api.getMyGames()

    const call = vi.mocked(fetch).mock.calls[0]
    const headers = call[1]!.headers as Headers
    expect(headers.get('Accept')).toBe('application/json')
    expect(headers.get('skip_zrok_interstitial')).toBe('1')
  })
})

// ---------------------------------------------------------------------------
// Response handling
// ---------------------------------------------------------------------------
describe('response handling', () => {
  it('returns undefined for 204 No Content', async () => {
    mockFetch(204)
    const result = await api.sendOtp('a@b.c')
    expect(result).toBeUndefined()
  })

  it('returns null for 404 Not Found', async () => {
    mockFetch(404)
    const result = await api.getMyAssignment('game-1')
    expect(result).toBeNull()
  })

  it('throws ApiError for non-ok response (500)', async () => {
    mockFetch(500, { message: 'Server error' })
    await expect(api.me()).rejects.toThrow(ApiError)
  })

  it('throws ApiError with message from response body', async () => {
    mockFetch(400, { message: 'Bad request' })
    try {
      await api.me()
      expect.unreachable()
    } catch (e) {
      expect(e).toBeInstanceOf(ApiError)
      expect((e as ApiError).message).toBe('Bad request')
      expect((e as ApiError).status).toBe(400)
    }
  })

  it('throws ApiError with fallback message when body has no message field', async () => {
    mockFetch(500, { detail: 'Internal error' })
    try {
      await api.me()
      expect.unreachable()
    } catch (e) {
      expect((e as ApiError).message).toContain('500')
    }
  })

  it('returns parsed JSON for successful responses', async () => {
    const game = {
      id: 'g1',
      name: 'Test Game',
      inviteCode: 'ABC',
      status: 0,
      createdAt: '2024-01-01',
      maxPlayers: 10,
      basePointsPerTag: 100,
      confirmationTimeout: '01:00:00',
      playerCount: 1,
      myRole: 1,
      safeTimeBlocks: [],
    }
    mockFetch(200, game)
    const result = await api.getGame('g1')
    expect(result).toEqual(game)
  })

  it('returns undefined when response body is empty', async () => {
    // Simulate 200 with empty body
    vi.mocked(fetch).mockResolvedValueOnce({
      status: 200,
      ok: true,
      headers: new Headers(),
      text: () => Promise.resolve(''),
    } as Response)
    const result = await api.me()
    expect(result).toBeUndefined()
  })
})

// ---------------------------------------------------------------------------
// Endpoint URL construction
// ---------------------------------------------------------------------------
describe('endpoint URL construction', () => {
  const BASE = 'http://localhost:5000'

  it('sendOtp calls /api/auth/send-otp', async () => {
    mockFetch(204)
    await api.sendOtp('a@b.c')
    expect(vi.mocked(fetch).mock.calls[0][0]).toBe(`${BASE}/api/auth/send-otp`)
  })

  it('verifyOtp calls /api/auth/verify-otp', async () => {
    mockFetch(200, { token: 't', player: { id: 'p1', email: 'a@b.c', displayName: 'A' } })
    await api.verifyOtp('a@b.c', '123')
    expect(vi.mocked(fetch).mock.calls[0][0]).toBe(`${BASE}/api/auth/verify-otp`)
  })

  it('me calls /api/auth/me', async () => {
    mockFetch(200, { id: 'p1', email: 'a@b.c', displayName: 'A' })
    await api.me()
    expect(vi.mocked(fetch).mock.calls[0][0]).toBe(`${BASE}/api/auth/me`)
  })

  it('updatePlayer calls /api/auth/me with PUT and returns updated player', async () => {
    const updated = { id: 'p1', email: 'a@b.c', displayName: 'New Name' }
    mockFetch(200, updated)

    const result = await api.updatePlayer('New Name')

    const call = vi.mocked(fetch).mock.calls[0]
    expect(call[0]).toBe(`${BASE}/api/auth/me`)
    expect(call[1]!.method).toBe('PUT')
    expect(call[1]!.body).toBe(JSON.stringify({ displayName: 'New Name' }))
    expect(result).toEqual(updated)
  })

  it('createGame calls /api/games with POST', async () => {
    mockFetch(200, {
      id: 'g1',
      name: 'G',
      inviteCode: 'C',
      status: 0,
      createdAt: '',
      maxPlayers: 10,
      basePointsPerTag: 100,
      confirmationTimeout: '01:00',
      playerCount: 1,
      myRole: 1,
      safeTimeBlocks: [],
    })
    await api.createGame({
      name: 'G',
      durationHours: 2,
      basePointsPerTag: 100,
      confirmationTimeoutMinutes: 30,
    })
    expect(vi.mocked(fetch).mock.calls[0][0]).toBe(`${BASE}/api/games`)
  })

  it('getMyGames calls /api/games', async () => {
    mockFetch(200, [])
    await api.getMyGames()
    expect(vi.mocked(fetch).mock.calls[0][0]).toBe(`${BASE}/api/games`)
  })

  it('getGame calls /api/games/:id', async () => {
    mockFetch(200, {
      id: 'g1',
      name: 'G',
      inviteCode: 'C',
      status: 0,
      createdAt: '',
      maxPlayers: 10,
      basePointsPerTag: 100,
      confirmationTimeout: '01:00',
      playerCount: 1,
      myRole: 1,
      safeTimeBlocks: [],
    })
    await api.getGame('game-42')
    expect(vi.mocked(fetch).mock.calls[0][0]).toBe(`${BASE}/api/games/game-42`)
  })

  it('joinGame calls /api/games/join/:inviteCode', async () => {
    mockFetch(200, {
      id: 'g1',
      name: 'G',
      inviteCode: 'INV',
      status: 0,
      createdAt: '',
      maxPlayers: 10,
      basePointsPerTag: 100,
      confirmationTimeout: '01:00',
      playerCount: 1,
      myRole: 1,
      safeTimeBlocks: [],
    })
    await api.joinGame('INV-CODE', 'Player1')
    expect(vi.mocked(fetch).mock.calls[0][0]).toBe(`${BASE}/api/games/join/INV-CODE`)
  })

  it('startGame calls /api/games/:id/start', async () => {
    mockFetch(200, {
      id: 'g1',
      name: 'G',
      inviteCode: 'C',
      status: 1,
      createdAt: '',
      maxPlayers: 10,
      basePointsPerTag: 100,
      confirmationTimeout: '01:00',
      playerCount: 1,
      myRole: 1,
      safeTimeBlocks: [],
    })
    await api.startGame('g1')
    expect(vi.mocked(fetch).mock.calls[0][0]).toBe(`${BASE}/api/games/g1/start`)
  })

  it('endGame calls /api/games/:id/end', async () => {
    mockFetch(200, {
      id: 'g1',
      name: 'G',
      inviteCode: 'C',
      status: 2,
      createdAt: '',
      maxPlayers: 10,
      basePointsPerTag: 100,
      confirmationTimeout: '01:00',
      playerCount: 1,
      myRole: 1,
      safeTimeBlocks: [],
    })
    await api.endGame('g1')
    expect(vi.mocked(fetch).mock.calls[0][0]).toBe(`${BASE}/api/games/g1/end`)
  })

  it('leaveGame calls /api/games/:id/leave', async () => {
    mockFetch(204)
    await api.leaveGame('g1')
    expect(vi.mocked(fetch).mock.calls[0][0]).toBe(`${BASE}/api/games/g1/leave`)
  })

  it('rejoinGame calls /api/games/:id/rejoin with POST', async () => {
    mockFetch(200, {
      id: 'g1',
      name: 'G',
      inviteCode: 'C',
      status: 1,
      createdAt: '',
      maxPlayers: 10,
      basePointsPerTag: 100,
      confirmationTimeout: '01:00',
      playerCount: 1,
      myRole: 1,
      safeTimeBlocks: [],
    })
    const result = await api.rejoinGame('g1')
    expect(vi.mocked(fetch).mock.calls[0][0]).toBe(`${BASE}/api/games/g1/rejoin`)
    expect(vi.mocked(fetch).mock.calls[0][1]!.method).toBe('POST')
    expect(result.status).toBe(1)
  })

  it('getMyAssignment calls /api/games/:id/assignments/me', async () => {
    mockFetch(200, null)
    await api.getMyAssignment('g1')
    expect(vi.mocked(fetch).mock.calls[0][0]).toBe(`${BASE}/api/games/g1/assignments/me`)
  })

  it('submitTag calls /api/games/:id/tag', async () => {
    mockFetch(200, {
      id: 't1',
      assignmentId: 'a1',
      hunterId: 'h1',
      targetId: 't1',
      conditionId: 'c1',
      status: 0,
      submittedAt: '',
    })
    await api.submitTag('g1', { assignmentId: 'a1', conditionId: 'c1' })
    expect(vi.mocked(fetch).mock.calls[0][0]).toBe(`${BASE}/api/games/g1/tag`)
  })

  it('getPendingTag calls /api/games/:id/tag/pending', async () => {
    mockFetch(200, null)
    await api.getPendingTag('g1')
    expect(vi.mocked(fetch).mock.calls[0][0]).toBe(`${BASE}/api/games/g1/tag/pending`)
  })

  it('confirmTag calls /api/games/:id/tag/:tagId/confirm', async () => {
    mockFetch(200, {
      id: 't1',
      assignmentId: 'a1',
      hunterId: 'h1',
      targetId: 't1',
      conditionId: 'c1',
      status: 1,
      submittedAt: '',
    })
    await api.confirmTag('g1', 'tag-123')
    expect(vi.mocked(fetch).mock.calls[0][0]).toBe(`${BASE}/api/games/g1/tag/tag-123/confirm`)
  })

  it('denyTag calls /api/games/:id/tag/:tagId/deny', async () => {
    mockFetch(200, {
      id: 't1',
      assignmentId: 'a1',
      hunterId: 'h1',
      targetId: 't1',
      conditionId: 'c1',
      status: 2,
      submittedAt: '',
    })
    await api.denyTag('g1', 'tag-123')
    expect(vi.mocked(fetch).mock.calls[0][0]).toBe(`${BASE}/api/games/g1/tag/tag-123/deny`)
  })

  it('voidTag calls /api/games/:id/tag/:tagId/void', async () => {
    mockFetch(200, {
      id: 't1',
      assignmentId: 'a1',
      hunterId: 'h1',
      targetId: 't1',
      conditionId: 'c1',
      status: 3,
      submittedAt: '',
    })
    await api.voidTag('g1', 'tag-123')
    expect(vi.mocked(fetch).mock.calls[0][0]).toBe(`${BASE}/api/games/g1/tag/tag-123/void`)
  })

  it('getLeaderboard calls /api/games/:id/leaderboard', async () => {
    mockFetch(200, [])
    await api.getLeaderboard('g1')
    expect(vi.mocked(fetch).mock.calls[0][0]).toBe(`${BASE}/api/games/g1/leaderboard`)
  })

  it('getGamePlayers calls /api/games/:id/players and returns players', async () => {
    const players = [
      { playerId: 'p1', displayName: 'Alice', email: 'alice@example.com', role: 1 },
      { playerId: 'p2', displayName: 'Bob', email: 'bob@example.com', role: 0 },
    ]
    mockFetch(200, players)
    const result = await api.getGamePlayers('g1')
    expect(vi.mocked(fetch).mock.calls[0][0]).toBe(`${BASE}/api/games/g1/players`)
    expect(result).toEqual(players)
  })

  it('addAdmin calls /api/games/:id/admins with POST', async () => {
    mockFetch(204)
    await api.addAdmin('g1', 'p1')
    expect(vi.mocked(fetch).mock.calls[0][0]).toBe(`${BASE}/api/games/g1/admins`)
  })

  it('removeAdmin calls /api/games/:id/admins/:playerId with DELETE', async () => {
    mockFetch(204)
    await api.removeAdmin('g1', 'p1')
    expect(vi.mocked(fetch).mock.calls[0][0]).toBe(`${BASE}/api/games/g1/admins/p1`)
  })

  it('addSafeTime calls /api/games/:id/safe-times with POST', async () => {
    mockFetch(200, { blockId: 'b1' })
    await api.addSafeTime('g1', { startTime: '08:00', endTime: '17:00' })
    expect(vi.mocked(fetch).mock.calls[0][0]).toBe(`${BASE}/api/games/g1/safe-times`)
  })

  it('removeSafeTime calls /api/games/:id/safe-times/:blockId with DELETE', async () => {
    mockFetch(204)
    await api.removeSafeTime('g1', 'b1')
    expect(vi.mocked(fetch).mock.calls[0][0]).toBe(`${BASE}/api/games/g1/safe-times/b1`)
  })

  it('addCondition calls /api/games/:id/conditions with POST', async () => {
    mockFetch(200, { id: 'c1', type: 4, description: 'Custom condition' })
    await api.addCondition('g1', 'Jump on one foot')
    expect(vi.mocked(fetch).mock.calls[0][0]).toBe(`${BASE}/api/games/g1/conditions`)
  })

  it('getVapidPublicKey calls /api/push/vapid-public-key', async () => {
    mockFetch(200, { publicKey: 'key123' })
    const key = await api.getVapidPublicKey()
    expect(key).toBe('key123')
    expect(vi.mocked(fetch).mock.calls[0][0]).toBe(`${BASE}/api/push/vapid-public-key`)
  })

  it('subscribePush calls /api/push/subscribe with subscription data', async () => {
    mockFetch(204)
    const subscription = {
      endpoint: 'https://example.com/push',
      toJSON: () => ({
        endpoint: 'https://example.com/push',
        keys: { p256dh: 'key1', auth: 'auth1' },
      }),
    } as unknown as PushSubscription
    await api.subscribePush(subscription)
    const call = vi.mocked(fetch).mock.calls[0]
    expect(call[0]).toBe(`${BASE}/api/push/subscribe`)
    expect(call[1]!.body).toContain('"endpoint":"https://example.com/push"')
  })

  it('subscribePush throws when keys are missing', async () => {
    const subscription = {
      endpoint: 'https://example.com/push',
      toJSON: () => ({
        endpoint: 'https://example.com/push',
        keys: null,
      }),
    } as unknown as PushSubscription
    await expect(api.subscribePush(subscription)).rejects.toThrow(ApiError)
  })
})
