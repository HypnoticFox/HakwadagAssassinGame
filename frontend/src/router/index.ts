import { createRouter, createWebHistory } from 'vue-router'

import { useAuthStore } from '@/stores'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      name: 'home',
      component: () => import('@/views/HomeView.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/login',
      name: 'login',
      component: () => import('@/views/LoginView.vue'),
      meta: { requiresGuest: true },
    },
    {
      path: '/games/create',
      name: 'create-game',
      component: () => import('@/views/CreateGameView.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/invite/:inviteCode',
      name: 'invite',
      component: () => import('@/views/InviteView.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/games/:id',
      name: 'game-detail',
      component: () => import('@/views/GameDetailView.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/games/:id/assignment',
      name: 'assignment',
      component: () => import('@/views/AssignmentView.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/games/:id/leaderboard',
      name: 'leaderboard',
      component: () => import('@/views/LeaderboardView.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/games/:id/tag/:tagId',
      name: 'tag-confirm',
      component: () => import('@/views/TagConfirmView.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/dev/dashboard',
      name: 'dev-dashboard',
      component: () => import('@/views/DevDashboardView.vue'),
      meta: { requiresAuth: true, devOnly: true },
    },
  ],
})

router.beforeEach(async (to) => {
  const authStore = useAuthStore()

  if (!authStore.isAuthenticated && authStore.token) {
    await authStore.loadFromStorage()
  }

  if (to.meta.requiresAuth && !authStore.isAuthenticated) {
    return { name: 'login', query: { redirect: to.fullPath } }
  }

  if (to.meta.requiresGuest && authStore.isAuthenticated) {
    return { name: 'home' }
  }

  if (to.meta.devOnly && !import.meta.env.DEV) {
    return { name: 'home' }
  }
})

export default router
