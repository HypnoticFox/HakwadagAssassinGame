import { describe, it, expect, beforeEach, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'

import { clearToasts } from '@/composables/useToast'
import HomeView from '@/views/HomeView.vue'
import { withI18n } from '../helpers/i18n'

const { mockApi, mockAuthStore, mockGameStore, mockRouterPush } = vi.hoisted(() => {
  return {
    mockApi: {
      updatePlayer: vi.fn(),
    },
    mockAuthStore: {
      player: null as { id: string; email: string; displayName: string } | null,
      setPlayer: vi.fn(),
    },
    mockGameStore: {
      recentGames: [],
      isLoading: false,
      loadMyGames: vi.fn(),
      joinGame: vi.fn(),
    },
    mockRouterPush: vi.fn(),
  }
})

vi.mock('@/api/client', () => ({
  api: mockApi,
}))

vi.mock('@/stores', () => ({
  useAuthStore: () => mockAuthStore,
  useGameStore: () => mockGameStore,
}))

vi.mock('vue-router', () => ({
  useRouter: () => ({
    push: mockRouterPush,
  }),
}))

vi.mock('@/composables/usePushNotifications', () => ({
  usePushNotifications: () => ({
    isSupported: { value: false },
    registerSubscription: vi.fn(),
  }),
}))

function mockPlayer() {
  return { id: 'p1', email: 'alice@example.com', displayName: 'Alice' }
}

beforeEach(() => {
  vi.clearAllMocks()
  clearToasts()
  mockAuthStore.player = mockPlayer()
  mockGameStore.recentGames = []
  mockGameStore.isLoading = false
})

describe('HomeView.vue', () => {
  it('renders the player display name', () => {
    const wrapper = mount(HomeView, withI18n())
    expect(wrapper.text()).toContain('Alice')
  })

  it('shows an inline input when the edit button is clicked', async () => {
    const wrapper = mount(HomeView, withI18n())

    await wrapper.find('.player-chip-action').trigger('click')

    const input = wrapper.find('.player-chip-input')
    expect(input.exists()).toBe(true)
    expect((input.element as HTMLInputElement).value).toBe('Alice')
    expect(wrapper.find('.player-chip-action--save').exists()).toBe(true)
    expect(wrapper.find('.player-chip-action--cancel').exists()).toBe(true)
  })

  it('calls updatePlayer and updates the store on save', async () => {
    const updated = { id: 'p1', email: 'alice@example.com', displayName: 'Ally' }
    mockApi.updatePlayer.mockResolvedValue(updated)

    const wrapper = mount(HomeView, withI18n())
    await wrapper.find('.player-chip-action').trigger('click')

    const input = wrapper.find('.player-chip-input')
    await input.setValue('Ally')
    await wrapper.find('.player-chip-action--save').trigger('click')
    await flushPromises()

    expect(mockApi.updatePlayer).toHaveBeenCalledWith('Ally')
    expect(mockAuthStore.setPlayer).toHaveBeenCalledWith(updated)
    expect(wrapper.find('.player-chip-input').exists()).toBe(false)
  })

  it('shows an error when updatePlayer fails', async () => {
    mockApi.updatePlayer.mockRejectedValue(new Error('Name taken'))

    const wrapper = mount(HomeView, withI18n())
    await wrapper.find('.player-chip-action').trigger('click')

    const input = wrapper.find('.player-chip-input')
    await input.setValue('Taken')
    await wrapper.find('.player-chip-action--save').trigger('click')
    await flushPromises()

    expect(wrapper.text()).toContain('Name taken')
    expect(mockAuthStore.setPlayer).not.toHaveBeenCalled()
  })

  it('returns to display mode without saving when cancel is clicked', async () => {
    const wrapper = mount(HomeView, withI18n())
    await wrapper.find('.player-chip-action').trigger('click')

    const input = wrapper.find('.player-chip-input')
    await input.setValue('Ignored')
    await wrapper.find('.player-chip-action--cancel').trigger('click')

    expect(mockApi.updatePlayer).not.toHaveBeenCalled()
    expect(wrapper.find('.player-chip-input').exists()).toBe(false)
    expect(wrapper.text()).toContain('Alice')
  })
})
