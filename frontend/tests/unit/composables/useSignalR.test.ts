import { describe, it, expect, beforeEach, vi } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { api } from '@/api/client'

const {
  mockGameStore,
  mockAssignmentStore,
  mockTagStore,
  mockLeaderboardStore,
  mockAuthStore,
  mockRouter,
} = vi.hoisted(() => ({
  mockGameStore: {
    currentGame: null as { id: string } | null,
    setGame: vi.fn(),
    loadGame: vi.fn(),
  },
  mockAssignmentStore: {
    loadAssignment: vi.fn(),
    setAssignment: vi.fn(),
  },
  mockTagStore: {
    setPendingTag: vi.fn(),
    setPendingOutgoingTag: vi.fn(),
    queuePendingTag: vi.fn(),
    clearPendingOutgoingTag: vi.fn(),
  },
  mockLeaderboardStore: {
    loadLeaderboard: vi.fn(),
  },
  mockAuthStore: {
    player: null as { id: string } | null,
  },
  mockRouter: {
    currentRoute: { value: { name: 'home' as string } },
    push: vi.fn(),
  },
}))

// Mock stores before importing the composable
vi.mock('@/stores', () => ({
  useGameStore: vi.fn(() => mockGameStore),
  useAssignmentStore: vi.fn(() => mockAssignmentStore),
  useTagStore: vi.fn(() => mockTagStore),
  useLeaderboardStore: vi.fn(() => mockLeaderboardStore),
  useAuthStore: vi.fn(() => mockAuthStore),
}))

vi.mock('vue-router', () => ({
  useRouter: () => mockRouter,
}))

vi.mock('@/api/client', () => ({
  api: {
    getToken: vi.fn(),
  },
}))

// Factory to create a mock hub instance
function createMockHub(startImpl?: () => Promise<void>) {
  const handlers: Record<string, (...args: unknown[]) => void> = {}
  return {
    on: vi.fn((event: string, handler: (...args: unknown[]) => void) => {
      handlers[event] = handler
    }),
    off: vi.fn(),
    start: vi.fn(startImpl || (() => Promise.resolve())),
    stop: vi.fn().mockResolvedValue(undefined),
    invoke: vi.fn().mockResolvedValue(undefined),
    state: 'Disconnected',
    onclose: vi.fn(),
    onreconnecting: vi.fn(),
    onreconnected: vi.fn(),
    _trigger: (event: string, ...args: unknown[]) => {
      handlers[event]?.(...args)
    },
  }
}

type MockHub = ReturnType<typeof createMockHub>

// Track hub instances per-test
let mockHubInstances: MockHub[] = []
let startShouldFail = false

function MockHubConnectionBuilder() {
  const hub = createMockHub(
    startShouldFail ? () => Promise.reject(new Error('Connection failed')) : undefined,
  )
  mockHubInstances.push(hub)
  return {
    withUrl: function () {
      return this
    },
    configureLogging: function () {
      return this
    },
    withAutomaticReconnect: function () {
      return this
    },
    build: function () {
      return hub
    },
  }
}

vi.mock('@microsoft/signalr', () => ({
  HubConnectionBuilder: MockHubConnectionBuilder,
  LogLevel: {
    Information: 2,
  },
}))

beforeEach(() => {
  setActivePinia(createPinia())
  vi.clearAllMocks()
  mockHubInstances = []
  startShouldFail = false
  mockGameStore.currentGame = null
  mockAuthStore.player = null
  mockRouter.currentRoute.value.name = 'home'
})

describe('useSignalR', () => {
  async function loadComposable() {
    const mod = await import('@/composables/useSignalR')
    return { useSignalR: mod.useSignalR }
  }

  describe('connection lifecycle', () => {
    it('does not connect when there is no token', async () => {
      vi.mocked(api.getToken).mockReturnValue(null)
      const { useSignalR } = await loadComposable()
      const signalR = useSignalR()

      await signalR.start()

      expect(signalR.isConnected.value).toBe(false)
      expect(mockHubInstances.length).toBe(0)
    })

    it('builds connection and starts when token is available', async () => {
      vi.mocked(api.getToken).mockReturnValue('valid-token')
      const { useSignalR } = await loadComposable()
      const signalR = useSignalR()

      await signalR.start()

      expect(mockHubInstances).toHaveLength(1)
      const hub = mockHubInstances[0]
      expect(hub.start).toHaveBeenCalled()
      expect(signalR.isConnected.value).toBe(true)
      expect(signalR.connection.value).toStrictEqual(hub)
    })

    it('sets error when connection fails', async () => {
      vi.mocked(api.getToken).mockReturnValue('valid-token')
      startShouldFail = true

      const { useSignalR } = await loadComposable()
      const signalR = useSignalR()

      await signalR.start()

      expect(mockHubInstances).toHaveLength(1)
      expect(signalR.error.value).toBe('Connection failed')
      expect(signalR.isConnected.value).toBe(false)
    })

    it('stop disconnects and clears connection', async () => {
      vi.mocked(api.getToken).mockReturnValue('valid-token')
      const { useSignalR } = await loadComposable()
      const signalR = useSignalR()
      await signalR.start()

      expect(signalR.isConnected.value).toBe(true)

      await signalR.stop()

      const hub = mockHubInstances[0]
      expect(hub.stop).toHaveBeenCalled()
      expect(signalR.connection.value).toBeNull()
      expect(signalR.isConnected.value).toBe(false)
    })
  })

  describe('event handlers', () => {
    it('registers ScoreUpdated handler that reloads leaderboard', async () => {
      vi.mocked(api.getToken).mockReturnValue('valid-token')
      const { useSignalR } = await loadComposable()
      const signalR = useSignalR()
      await signalR.start()

      expect(mockHubInstances).toHaveLength(1)
      const hub = mockHubInstances[0]
      expect(hub.on).toHaveBeenCalledWith('ScoreUpdated', expect.any(Function))
    })

    it('start registers all expected event handlers', async () => {
      vi.mocked(api.getToken).mockReturnValue('valid-token')
      const { useSignalR } = await loadComposable()
      const signalR = useSignalR()
      await signalR.start()

      expect(mockHubInstances).toHaveLength(1)
      const hub = mockHubInstances[0]
      const events = hub.on.mock.calls.map((c: [string]) => c[0])
      expect(events).toContain('ScoreUpdated')
      expect(events).toContain('TagSubmitted')
      expect(events).toContain('TagResolved')
      expect(events).toContain('GameStarted')
      expect(events).toContain('GameEnded')
      expect(events).toContain('AssignmentChanged')
      expect(events).toContain('PlayerLeft')
    })

    it('registers onclose and onreconnecting/reconnected callbacks', async () => {
      vi.mocked(api.getToken).mockReturnValue('valid-token')
      const { useSignalR } = await loadComposable()
      const signalR = useSignalR()
      await signalR.start()

      expect(mockHubInstances).toHaveLength(1)
      const hub = mockHubInstances[0]
      expect(hub.onclose).toHaveBeenCalled()
      expect(hub.onreconnecting).toHaveBeenCalled()
      expect(hub.onreconnected).toHaveBeenCalled()
    })

    describe('TagSubmitted handler', () => {
      const tag = {
        id: 'tag1',
        assignmentId: 'a1',
        hunterId: 'hunter1',
        targetId: 'target1',
        conditionId: 'c1',
        status: 0,
        submittedAt: '2024-01-01T00:00:00Z',
      }

      async function startWithPlayer(playerId: string) {
        vi.mocked(api.getToken).mockReturnValue('valid-token')
        mockGameStore.currentGame = { id: 'g1' }
        mockAuthStore.player = { id: playerId }
        const { useSignalR } = await loadComposable()
        const signalR = useSignalR()
        await signalR.start()
        return mockHubInstances[0]
      }

      it('redirects the target to the tag confirmation page', async () => {
        const hub = await startWithPlayer('target1')

        hub._trigger('TagSubmitted', 'g1', tag)

        expect(mockTagStore.setPendingTag).toHaveBeenCalledWith(tag)
        expect(mockRouter.push).toHaveBeenCalledWith('/games/g1/tag/tag1')
      })

      it('queues a tag when the target is already confirming a tag', async () => {
        mockRouter.currentRoute.value.name = 'tag-confirm'
        const hub = await startWithPlayer('target1')

        hub._trigger('TagSubmitted', 'g1', tag)

        expect(mockTagStore.queuePendingTag).toHaveBeenCalledWith('g1', tag)
        expect(mockRouter.push).not.toHaveBeenCalled()
      })

      it('sets the outgoing tag for the hunter without redirecting', async () => {
        const hub = await startWithPlayer('hunter1')

        hub._trigger('TagSubmitted', 'g1', tag)

        expect(mockTagStore.setPendingOutgoingTag).toHaveBeenCalledWith(tag)
        expect(mockRouter.push).not.toHaveBeenCalled()
      })
    })
  })

  describe('reconnection', () => {
    it('sets isConnected to false on close', async () => {
      vi.mocked(api.getToken).mockReturnValue('valid-token')
      const { useSignalR } = await loadComposable()
      const signalR = useSignalR()
      await signalR.start()

      const hub = mockHubInstances[0]
      const closeCb = hub.onclose.mock.calls[0][0]
      closeCb()
      expect(signalR.isConnected.value).toBe(false)
    })

    it('sets isConnected to false on reconnecting', async () => {
      vi.mocked(api.getToken).mockReturnValue('valid-token')
      const { useSignalR } = await loadComposable()
      const signalR = useSignalR()
      await signalR.start()

      const hub = mockHubInstances[0]
      const reconnectingCb = hub.onreconnecting.mock.calls[0][0]
      reconnectingCb()
      expect(signalR.isConnected.value).toBe(false)
    })

    it('sets isConnected to true on reconnected', async () => {
      vi.mocked(api.getToken).mockReturnValue('valid-token')
      const { useSignalR } = await loadComposable()
      const signalR = useSignalR()
      await signalR.start()
      signalR.isConnected.value = false

      const hub = mockHubInstances[0]
      const reconnectedCb = hub.onreconnected.mock.calls[0][0]
      reconnectedCb()
      expect(signalR.isConnected.value).toBe(true)
    })
  })

  describe('joinGame / leaveGame', () => {
    it('calls invoke JoinGame when connected', async () => {
      vi.mocked(api.getToken).mockReturnValue('valid-token')
      const { useSignalR } = await loadComposable()
      const signalR = useSignalR()
      await signalR.start()

      const hub = mockHubInstances[0]
      hub.state = 'Connected'

      await signalR.joinGame('g1')

      expect(hub.invoke).toHaveBeenCalledWith('JoinGame', 'g1')
    })

    it('does not call invoke JoinGame when not connected', async () => {
      vi.mocked(api.getToken).mockReturnValue(null)
      const { useSignalR } = await loadComposable()
      const signalR = useSignalR()

      await signalR.joinGame('g1')

      expect(mockHubInstances.length).toBe(0)
    })

    it('calls invoke LeaveGame when connected', async () => {
      vi.mocked(api.getToken).mockReturnValue('valid-token')
      const { useSignalR } = await loadComposable()
      const signalR = useSignalR()
      await signalR.start()

      const hub = mockHubInstances[0]
      hub.state = 'Connected'

      await signalR.leaveGame('g1')

      expect(hub.invoke).toHaveBeenCalledWith('LeaveGame', 'g1')
    })

    it('does not call invoke LeaveGame when not connected', async () => {
      vi.mocked(api.getToken).mockReturnValue(null)
      const { useSignalR } = await loadComposable()
      const signalR = useSignalR()

      await signalR.leaveGame('g1')

      expect(mockHubInstances.length).toBe(0)
    })
  })
})
