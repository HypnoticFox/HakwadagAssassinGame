import { describe, it, expect, beforeEach, vi } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { useLeaderboardStore } from '@/stores/leaderboard'
import { api } from '@/api/client'
import type { LeaderboardEntryDto } from '@/types'

vi.mock('@/api/client', () => ({
  api: {
    getLeaderboard: vi.fn(),
  },
}))

function makeEntry(overrides: Partial<LeaderboardEntryDto> = {}): LeaderboardEntryDto {
  return {
    player: { id: 'p1', email: 'a@b.c', displayName: 'Alice' },
    score: 100,
    tags: 2,
    ...overrides,
  }
}

beforeEach(() => {
  setActivePinia(createPinia())
  vi.clearAllMocks()
})

describe('leaderboard store', () => {
  describe('initial state', () => {
    it('has default values', () => {
      const store = useLeaderboardStore()
      expect(store.entries).toEqual([])
      expect(store.isLoading).toBe(false)
      expect(store.error).toBeNull()
    })
  })

  describe('loadLeaderboard', () => {
    it('sets entries on success', async () => {
      const entries = [makeEntry(), makeEntry({ player: { id: 'p2', email: 'b@c.d', displayName: 'Bob' }, score: 50, tags: 1 })]
      vi.mocked(api.getLeaderboard).mockResolvedValue(entries)
      const store = useLeaderboardStore()

      const result = await store.loadLeaderboard('g1')

      expect(result).toBe(entries)
      expect(store.entries).toEqual(entries)
      expect(store.isLoading).toBe(false)
    })

    it('sets empty array when api returns empty', async () => {
      vi.mocked(api.getLeaderboard).mockResolvedValue([])
      const store = useLeaderboardStore()

      const result = await store.loadLeaderboard('g1')

      expect(result).toEqual([])
      expect(store.entries).toEqual([])
    })

    it('sets error and rethrows on failure', async () => {
      vi.mocked(api.getLeaderboard).mockRejectedValue(new Error('Forbidden'))
      const store = useLeaderboardStore()

      await expect(store.loadLeaderboard('g1')).rejects.toThrow('Forbidden')
      expect(store.error).toBe('Forbidden')
      expect(store.isLoading).toBe(false)
    })

    it('manages loading state', async () => {
      vi.mocked(api.getLeaderboard).mockResolvedValue([])
      const store = useLeaderboardStore()

      const promise = store.loadLeaderboard('g1')
      expect(store.isLoading).toBe(true)
      await promise
      expect(store.isLoading).toBe(false)
    })
  })

  describe('setEntries', () => {
    it('replaces entries', () => {
      const store = useLeaderboardStore()
      const entries = [makeEntry()]
      store.setEntries(entries)
      expect(store.entries).toStrictEqual(entries)
    })
  })
})
