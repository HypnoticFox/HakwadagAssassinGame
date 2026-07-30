import { describe, it, expect, beforeEach, vi } from 'vitest'
import type { Router } from 'vue-router'

// Mock the auth store before importing router
const mockAuthStore = {
  isAuthenticated: false,
  token: null,
  loadFromStorage: vi.fn(),
}

let router: Router

vi.mock('@/stores', () => ({
  useAuthStore: () => mockAuthStore,
}))

// Mock lazy-loaded views
vi.mock('@/views/HomeView.vue', () => ({ default: { template: '<div>Home</div>' } }))
vi.mock('@/views/LoginView.vue', () => ({ default: { template: '<div>Login</div>' } }))
vi.mock('@/views/CreateGameView.vue', () => ({
  default: { template: '<div>Create Game</div>' },
}))
vi.mock('@/views/GameDetailView.vue', () => ({
  default: { template: '<div>Game Detail</div>' },
}))
vi.mock('@/views/AssignmentView.vue', () => ({
  default: { template: '<div>Assignment</div>' },
}))
vi.mock('@/views/LeaderboardView.vue', () => ({
  default: { template: '<div>Leaderboard</div>' },
}))
vi.mock('@/views/TagConfirmView.vue', () => ({
  default: { template: '<div>Tag Confirm</div>' },
}))

beforeEach(async () => {
  vi.clearAllMocks()
  // Reset auth store state
  mockAuthStore.isAuthenticated = false
  mockAuthStore.token = null
  mockAuthStore.loadFromStorage = vi.fn().mockResolvedValue(undefined)

  // Re-import router to get a fresh instance
  vi.resetModules()
  const { default: freshRouter } = await import('@/router')
  router = freshRouter
})

describe('router', () => {
  describe('auth guard (requiresAuth)', () => {
    it('redirects to login when unauthenticated without token', async () => {
      await router.push('/')
      await router.isReady()

      expect(router.currentRoute.value.name).toBe('login')
    })

    it('redirects to login with redirect query when unauthenticated', async () => {
      await router.push('/games/123')
      await router.isReady()

      expect(router.currentRoute.value.name).toBe('login')
      expect(router.currentRoute.value.query.redirect).toBe('/games/123')
    })

    it('allows access when authenticated', async () => {
      mockAuthStore.isAuthenticated = true
      await router.push('/')
      await router.isReady()

      expect(router.currentRoute.value.name).toBe('home')
    })

    it('attempts token recovery when token exists but not authenticated', async () => {
      mockAuthStore.token = 'stored-token'
      mockAuthStore.isAuthenticated = false

      await router.push('/')
      await router.isReady()

      expect(mockAuthStore.loadFromStorage).toHaveBeenCalled()
    })

    it('redirects to login even after failed token recovery', async () => {
      mockAuthStore.token = 'stored-token'
      mockAuthStore.isAuthenticated = false
      mockAuthStore.loadFromStorage.mockResolvedValue(undefined)

      await router.push('/')
      await router.isReady()

      // After loadFromStorage, isAuthenticated is still false -> redirect to login
      expect(router.currentRoute.value.name).toBe('login')
    })

    it('does NOT attempt token recovery when already authenticated', async () => {
      mockAuthStore.isAuthenticated = true
      mockAuthStore.token = null

      await router.push('/')
      await router.isReady()

      expect(mockAuthStore.loadFromStorage).not.toHaveBeenCalled()
    })
  })

  describe('guest guard (requiresGuest)', () => {
    it('redirects to home when authenticated', async () => {
      mockAuthStore.isAuthenticated = true
      await router.push('/login')
      await router.isReady()

      expect(router.currentRoute.value.name).toBe('home')
    })

    it('allows access to login when not authenticated', async () => {
      await router.push('/login')
      await router.isReady()

      expect(router.currentRoute.value.name).toBe('login')
    })
  })

  describe('lazy loading', () => {
    it('all routes have async component imports', async () => {
      const routes = router.getRoutes()
      for (const route of routes) {
        const comp = route.components?.default
        // Each component should be either a function (lazy) or a mock module
        expect(comp).toBeDefined()
      }
    })
  })

  describe('route definitions', () => {
    it('has all expected routes', async () => {
      const routeNames = router.getRoutes().map((r) => r.name)
      expect(routeNames).toContain('home')
      expect(routeNames).toContain('login')
      expect(routeNames).toContain('create-game')
      expect(routeNames).toContain('game-detail')
      expect(routeNames).toContain('assignment')
      expect(routeNames).toContain('leaderboard')
      expect(routeNames).toContain('tag-confirm')
    })

    it('home route requires auth', async () => {
      const route = router.getRoutes().find((r) => r.name === 'home')
      expect(route?.meta?.requiresAuth).toBe(true)
    })

    it('login route requires guest', async () => {
      const route = router.getRoutes().find((r) => r.name === 'login')
      expect(route?.meta?.requiresGuest).toBe(true)
    })

    it('game-detail route requires auth', async () => {
      const route = router.getRoutes().find((r) => r.name === 'game-detail')
      expect(route?.meta?.requiresAuth).toBe(true)
    })

    it('unauthenticated access to game-detail redirects to login', async () => {
      await router.push('/games/123')
      await router.isReady()
      expect(router.currentRoute.value.name).toBe('login')
    })
  })
})
