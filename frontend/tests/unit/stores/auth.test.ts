import { describe, it, expect, beforeEach, vi } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { useAuthStore } from '@/stores/auth'
import { api } from '@/api/client'
import type { AuthResponse } from '@/types'

vi.mock('@/api/client', () => ({
  api: {
    getToken: vi.fn(),
    setToken: vi.fn(),
    clearToken: vi.fn(),
    sendOtp: vi.fn(),
    verifyOtp: vi.fn(),
    me: vi.fn(),
  },
}))

beforeEach(() => {
  setActivePinia(createPinia())
  vi.clearAllMocks()
  localStorage.clear()
})

function mockPlayer() {
  return { id: 'p1', email: 'a@b.c', displayName: 'Alice' }
}

describe('auth store', () => {
  describe('initial state', () => {
    it('has default values when no token is stored', () => {
      vi.mocked(api.getToken).mockReturnValue(null)
      const store = useAuthStore()
      expect(store.token).toBeNull()
      expect(store.player).toBeNull()
      expect(store.isLoading).toBe(false)
      expect(store.error).toBeNull()
      expect(store.isAuthenticated).toBe(false)
    })

    it('reads token from api on init', () => {
      vi.mocked(api.getToken).mockReturnValue('stored-token')
      const store = useAuthStore()
      expect(store.token).toBe('stored-token')
    })
  })

  describe('loadFromStorage', () => {
    it('does nothing when no token exists', async () => {
      vi.mocked(api.getToken).mockReturnValue(null)
      const store = useAuthStore()
      await store.loadFromStorage()
      expect(api.me).not.toHaveBeenCalled()
      expect(store.isAuthenticated).toBe(false)
    })

    it('sets player on successful me() call', async () => {
      vi.mocked(api.getToken).mockReturnValue('valid-token')
      const player = mockPlayer()
      vi.mocked(api.me).mockResolvedValue(player)
      const store = useAuthStore()
      await store.loadFromStorage()

      expect(store.player).toEqual(player)
      expect(store.isAuthenticated).toBe(true)
      expect(store.isLoading).toBe(false)
    })

    it('clears token on me() failure', async () => {
      vi.mocked(api.getToken).mockReturnValue('invalid-token')
      vi.mocked(api.me).mockRejectedValue(new Error('Unauthorized'))
      const store = useAuthStore()
      await store.loadFromStorage()

      expect(store.player).toBeNull()
      expect(store.token).toBeNull()
      expect(api.clearToken).toHaveBeenCalled()
      expect(store.error).toBe('Unauthorized')
    })
  })

  describe('sendOtp', () => {
    it('calls api.sendOtp and manages loading state', async () => {
      vi.mocked(api.sendOtp).mockResolvedValue(undefined)
      const store = useAuthStore()
      const promise = store.sendOtp('user@example.com')

      expect(store.isLoading).toBe(true)
      await promise

      expect(api.sendOtp).toHaveBeenCalledWith('user@example.com')
      expect(store.isLoading).toBe(false)
      expect(store.error).toBeNull()
    })

    it('sets error and rethrows on failure', async () => {
      vi.mocked(api.sendOtp).mockRejectedValue(new Error('Network error'))
      const store = useAuthStore()

      await expect(store.sendOtp('user@example.com')).rejects.toThrow('Network error')
      expect(store.error).toBe('Network error')
      expect(store.isLoading).toBe(false)
    })
  })

  describe('verifyOtp', () => {
    it('stores token and player on success', async () => {
      const response: AuthResponse = {
        token: 'new-token',
        player: mockPlayer(),
      }
      vi.mocked(api.verifyOtp).mockResolvedValue(response)
      const store = useAuthStore()

      await store.verifyOtp('user@example.com', '123456')

      expect(store.token).toBe('new-token')
      expect(store.player).toEqual(mockPlayer())
      expect(api.setToken).toHaveBeenCalledWith('new-token')
      expect(store.isAuthenticated).toBe(true)
    })

    it('sets error and rethrows on failure', async () => {
      vi.mocked(api.verifyOtp).mockRejectedValue(new Error('Invalid code'))
      const store = useAuthStore()

      await expect(store.verifyOtp('a@b.c', '000')).rejects.toThrow('Invalid code')
      expect(store.error).toBe('Invalid code')
      expect(store.isLoading).toBe(false)
    })
  })

  describe('logout', () => {
    it('clears token, player, and recent games', () => {
      vi.mocked(api.getToken).mockReturnValue('some-token')
      const store = useAuthStore()
      store.player = mockPlayer()
      localStorage.setItem('hakwadag_recent_games', '[...]')

      store.logout()

      expect(store.token).toBeNull()
      expect(store.player).toBeNull()
      expect(api.clearToken).toHaveBeenCalled()
      expect(localStorage.getItem('hakwadag_recent_games')).toBeNull()
    })
  })

  describe('isAuthenticated', () => {
    it('returns true when both token and player are set', () => {
      vi.mocked(api.getToken).mockReturnValue('t')
      const store = useAuthStore()
      store.player = mockPlayer()
      expect(store.isAuthenticated).toBe(true)
    })

    it('returns false when token is null', () => {
      vi.mocked(api.getToken).mockReturnValue(null)
      const store = useAuthStore()
      store.player = mockPlayer()
      expect(store.isAuthenticated).toBe(false)
    })

    it('returns false when player is null', () => {
      vi.mocked(api.getToken).mockReturnValue('t')
      const store = useAuthStore()
      expect(store.isAuthenticated).toBe(false)
    })
  })

  describe('setPlayer', () => {
    it('updates the player ref', () => {
      vi.mocked(api.getToken).mockReturnValue(null)
      const store = useAuthStore()
      store.setPlayer(mockPlayer())
      expect(store.player).toEqual(mockPlayer())
    })
  })
})
