import { describe, it, expect, beforeEach, vi } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { useGameStore, type RecentGame } from '@/stores/game'
import { api } from '@/api/client'
import { GameRole, GameStatus, type GameDto } from '@/types'
import type { GamePlayerDto } from '@/types'

vi.mock('@/api/client', () => ({
  api: {
    createGame: vi.fn(),
    getMyGames: vi.fn(),
    joinGame: vi.fn(),
    getGame: vi.fn(),
    startGame: vi.fn(),
    endGame: vi.fn(),
    leaveGame: vi.fn(),
    rejoinGame: vi.fn(),
    getGamePlayers: vi.fn(),
    addAdmin: vi.fn(),
    removeAdmin: vi.fn(),
    addSafeTime: vi.fn(),
    removeSafeTime: vi.fn(),
    addCondition: vi.fn(),
  },
}))

function makeGame(overrides: Partial<GameDto> = {}): GameDto {
  return {
    id: 'g1',
    name: 'Test Game',
    inviteCode: 'ABC123',
    status: GameStatus.NotStarted,
    createdAt: '2024-01-01T00:00:00Z',
    maxPlayers: 10,
    basePointsPerTag: 100,
    confirmationTimeout: '01:00:00',
    playerCount: 1,
    myRole: 1 as GameRole,
    safeTimeBlocks: [],
    ...overrides,
  }
}

beforeEach(() => {
  setActivePinia(createPinia())
  vi.clearAllMocks()
  localStorage.clear()
})

describe('game store', () => {
  describe('initial state', () => {
    it('has default values', () => {
      const store = useGameStore()
      expect(store.currentGame).toBeNull()
      expect(store.recentGames).toEqual([])
      expect(store.gamePlayers).toEqual([])
      expect(store.isLoading).toBe(false)
      expect(store.error).toBeNull()
    })

    it('reads recent games from localStorage', () => {
      const recent: RecentGame[] = [
        { id: 'g1', name: 'Old Game', status: GameStatus.Ended, myRole: 0, joinedAt: '' },
      ]
      localStorage.setItem('hakwadag_recent_games', JSON.stringify(recent))
      const store = useGameStore()
      expect(store.recentGames).toHaveLength(1)
      expect(store.recentGames[0].id).toBe('g1')
    })

    it('handles invalid localStorage gracefully', () => {
      localStorage.setItem('hakwadag_recent_games', 'not-json')
      const store = useGameStore()
      expect(store.recentGames).toEqual([])
    })
  })

  describe('computed properties', () => {
    it('isActive is true when game status is Active', () => {
      const store = useGameStore()
      store.currentGame = makeGame({ status: GameStatus.Active })
      expect(store.isActive).toBe(true)
    })

    it('isActive is false when game status is not Active', () => {
      const store = useGameStore()
      store.currentGame = makeGame({ status: GameStatus.NotStarted })
      expect(store.isActive).toBe(false)
    })

    it('isCreator is true when myRole is 1 (Creator)', () => {
      const store = useGameStore()
      store.currentGame = makeGame({ myRole: 1 as GameRole })
      expect(store.isCreator).toBe(true)
    })

    it('isAdmin is true for Creator role', () => {
      const store = useGameStore()
      store.currentGame = makeGame({ myRole: 1 as GameRole })
      expect(store.isAdmin).toBe(true)
    })

    it('isAdmin is true for CoAdmin role', () => {
      const store = useGameStore()
      store.currentGame = makeGame({ myRole: 2 as GameRole })
      expect(store.isAdmin).toBe(true)
    })

    it('isAdmin is false for Player role', () => {
      const store = useGameStore()
      store.currentGame = makeGame({ myRole: 0 as GameRole })
      expect(store.isAdmin).toBe(false)
    })

    it('isAdmin is false when no current game', () => {
      const store = useGameStore()
      expect(store.isAdmin).toBe(false)
    })
  })

  describe('createGame', () => {
    it('calls api.createGame and updates currentGame', async () => {
      const game = makeGame()
      vi.mocked(api.createGame).mockResolvedValue(game)
      const store = useGameStore()

      const result = await store.createGame({
        name: 'New Game',
        durationHours: 2,
        basePointsPerTag: 100,
        confirmationTimeoutMinutes: 30,
      })

      expect(result).toStrictEqual(game)
      expect(store.currentGame).toStrictEqual(game)
      expect(store.recentGames).toHaveLength(1)
      expect(store.recentGames[0].id).toBe('g1')
    })

    it('sets error and rethrows on failure', async () => {
      vi.mocked(api.createGame).mockRejectedValue(new Error('Creation failed'))
      const store = useGameStore()

      await expect(
        store.createGame({
          name: 'X',
          durationHours: 1,
          basePointsPerTag: 50,
          confirmationTimeoutMinutes: 15,
        }),
      ).rejects.toThrow('Creation failed')
      expect(store.error).toBe('Creation failed')
    })
  })

  describe('loadMyGames', () => {
    it('calls api.getMyGames and updates recentGames', async () => {
      const games = [makeGame({ id: 'g1', name: 'Game 1' }), makeGame({ id: 'g2', name: 'Game 2' })]
      vi.mocked(api.getMyGames).mockResolvedValue(games)
      const store = useGameStore()

      const result = await store.loadMyGames()

      expect(result).toBe(games)
      expect(store.recentGames).toHaveLength(2)
      expect(store.recentGames[0].id).toBe('g1')
      expect(store.recentGames[1].id).toBe('g2')
    })

    it('persists recent games to localStorage', async () => {
      vi.mocked(api.getMyGames).mockResolvedValue([makeGame()])
      const store = useGameStore()
      await store.loadMyGames()

      const stored = JSON.parse(localStorage.getItem('hakwadag_recent_games')!)
      expect(stored).toHaveLength(1)
      expect(stored[0].id).toBe('g1')
    })
  })

  describe('joinGame', () => {
    it('calls api.joinGame and updates currentGame', async () => {
      const game = makeGame()
      vi.mocked(api.joinGame).mockResolvedValue(game)
      const store = useGameStore()

      const result = await store.joinGame('INVITE', 'Bob')

      expect(result).toStrictEqual(game)
      expect(store.currentGame).toStrictEqual(game)
      expect(store.recentGames).toHaveLength(1)
    })
  })

  describe('loadGame', () => {
    it('calls api.getGame and updates currentGame', async () => {
      const game = makeGame()
      vi.mocked(api.getGame).mockResolvedValue(game)
      const store = useGameStore()

      const result = await store.loadGame('g1')

      expect(result).toStrictEqual(game)
      expect(store.currentGame).toStrictEqual(game)
    })

    it('does not update currentGame when game is null', async () => {
      vi.mocked(api.getGame).mockResolvedValue(null as unknown as GameDto)
      const store = useGameStore()
      store.currentGame = makeGame()

      await store.loadGame('g1')

      // currentGame should remain unchanged
      expect(store.currentGame).not.toBeNull()
    })
  })

  describe('startGame', () => {
    it('calls api.startGame and updates currentGame', async () => {
      const started = makeGame({ status: GameStatus.Active })
      vi.mocked(api.startGame).mockResolvedValue(started)
      const store = useGameStore()

      const result = await store.startGame('g1')

      expect(result).toBe(started)
      expect(store.currentGame?.status).toBe(GameStatus.Active)
    })
  })

  describe('endGame', () => {
    it('calls api.endGame and updates currentGame', async () => {
      const ended = makeGame({ status: GameStatus.Ended })
      vi.mocked(api.endGame).mockResolvedValue(ended)
      const store = useGameStore()

      const result = await store.endGame('g1')

      expect(result).toBe(ended)
      expect(store.currentGame?.status).toBe(GameStatus.Ended)
    })
  })

  describe('leaveGame', () => {
    it('clears currentGame when leaving a game that has not started', async () => {
      vi.mocked(api.leaveGame).mockResolvedValue(undefined)
      const store = useGameStore()
      store.currentGame = makeGame({ status: GameStatus.NotStarted })
      store.recentGames = [{ id: 'g1', name: 'G', status: 0, myRole: 0, joinedAt: '' }]

      await store.leaveGame('g1')

      expect(api.leaveGame).toHaveBeenCalledWith('g1')
      expect(store.currentGame).toBeNull()
      expect(store.recentGames).toHaveLength(0)
    })

    it('reloads game when leaving an active game so player remains a member', async () => {
      const activeGame = makeGame({ status: GameStatus.Active, isParticipating: true })
      const leftGame = makeGame({ status: GameStatus.Active, isParticipating: false })
      vi.mocked(api.leaveGame).mockResolvedValue(undefined)
      vi.mocked(api.getGame).mockResolvedValue(leftGame)
      const store = useGameStore()
      store.currentGame = activeGame
      store.recentGames = [{ id: 'g1', name: 'G', status: 1, myRole: 0, joinedAt: '' }]

      await store.leaveGame('g1')

      expect(api.leaveGame).toHaveBeenCalledWith('g1')
      expect(api.getGame).toHaveBeenCalledWith('g1')
      expect(store.currentGame).toStrictEqual(leftGame)
      expect(store.recentGames).toHaveLength(1)
      expect(store.recentGames[0].id).toBe('g1')
    })
  })

  describe('rejoinGame', () => {
    it('calls api.rejoinGame and updates currentGame', async () => {
      const rejoined = makeGame({ status: GameStatus.Active, isParticipating: true })
      vi.mocked(api.rejoinGame).mockResolvedValue(rejoined)
      const store = useGameStore()

      const result = await store.rejoinGame('g1')

      expect(api.rejoinGame).toHaveBeenCalledWith('g1')
      expect(result).toStrictEqual(rejoined)
      expect(store.currentGame).toStrictEqual(rejoined)
      expect(store.recentGames).toHaveLength(1)
      expect(store.recentGames[0].id).toBe('g1')
    })

    it('sets error and rethrows on failure', async () => {
      vi.mocked(api.rejoinGame).mockRejectedValue(new Error('Rejoin failed'))
      const store = useGameStore()

      await expect(store.rejoinGame('g1')).rejects.toThrow('Rejoin failed')
      expect(store.error).toBe('Rejoin failed')
    })
  })

  describe('loadGamePlayers', () => {
    it('calls api.getGamePlayers and updates gamePlayers', async () => {
      const players: GamePlayerDto[] = [
        {
          playerId: 'p1',
          displayName: 'Alice',
          email: 'alice@example.com',
          role: GameRole.Creator,
        },
        { playerId: 'p2', displayName: 'Bob', email: 'bob@example.com', role: GameRole.Player },
      ]
      vi.mocked(api.getGamePlayers).mockResolvedValue(players)
      const store = useGameStore()

      await store.loadGamePlayers('g1')

      expect(api.getGamePlayers).toHaveBeenCalledWith('g1')
      expect(store.gamePlayers).toStrictEqual(players)
    })
  })

  describe('addAdmin / removeAdmin / addSafeTime / removeSafeTime / addCondition', () => {
    it('addAdmin delegates to api', async () => {
      vi.mocked(api.addAdmin).mockResolvedValue(undefined)
      const store = useGameStore()
      await store.addAdmin('g1', 'p1')
      expect(api.addAdmin).toHaveBeenCalledWith('g1', 'p1')
    })

    it('removeAdmin delegates to api', async () => {
      vi.mocked(api.removeAdmin).mockResolvedValue(undefined)
      const store = useGameStore()
      await store.removeAdmin('g1', 'p1')
      expect(api.removeAdmin).toHaveBeenCalledWith('g1', 'p1')
    })

    it('addSafeTime delegates to api', async () => {
      vi.mocked(api.addSafeTime).mockResolvedValue('b1')
      const store = useGameStore()
      await store.addSafeTime('g1', { startTime: '08:00', endTime: '17:00' })
      expect(api.addSafeTime).toHaveBeenCalledWith('g1', {
        startTime: '08:00',
        endTime: '17:00',
      })
    })

    it('removeSafeTime delegates to api', async () => {
      vi.mocked(api.removeSafeTime).mockResolvedValue(undefined)
      const store = useGameStore()
      await store.removeSafeTime('g1', 'b1')
      expect(api.removeSafeTime).toHaveBeenCalledWith('g1', 'b1')
    })

    it('addCondition delegates to api and returns result', async () => {
      const condition = { id: 'c1', type: 4 as const, description: 'Jump!' }
      vi.mocked(api.addCondition).mockResolvedValue(condition)
      const store = useGameStore()
      const result = await store.addCondition('g1', 'Jump!')
      expect(api.addCondition).toHaveBeenCalledWith('g1', 'Jump!')
      expect(result).toBe(condition)
    })
  })

  describe('setGame', () => {
    it('sets currentGame and adds to recent', () => {
      const store = useGameStore()
      const game = makeGame()
      store.setGame(game)
      expect(store.currentGame).toStrictEqual(game)
      expect(store.recentGames).toHaveLength(1)
    })
  })
})
