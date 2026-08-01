import { describe, it, expect, beforeEach, vi } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'

import DevPlayerSwitcher from '@/components/DevPlayerSwitcher.vue'

const { mockApi, mockAuthStore, mockRouterPush } = vi.hoisted(() => {
  return {
    mockApi: {
      devLogin: vi.fn(),
      seedGame: vi.fn(),
      setToken: vi.fn(),
      devGetGamePlayers: vi.fn(),
      devGetGameAssignments: vi.fn(),
      devGetGameTags: vi.fn(),
      devSubmitTag: vi.fn(),
      devConfirmTag: vi.fn(),
      devDenyTag: vi.fn(),
      devEndGame: vi.fn(),
    },
    mockAuthStore: {
      player: null,
      devLogin: vi.fn(),
    },
    mockRouterPush: vi.fn(),
  }
})

vi.mock('@/api/client', () => ({
  api: mockApi,
}))

vi.mock('@/stores', () => ({
  useAuthStore: () => mockAuthStore,
}))

vi.mock('vue-router', () => ({
  useRouter: () => ({
    push: mockRouterPush,
  }),
}))

beforeEach(() => {
  vi.clearAllMocks()
  mockAuthStore.player = null
  vi.stubEnv('DEV', true)
  localStorage.clear()
})

function mockPlayer() {
  return {
    id: 'p1',
    email: 'dev@example.com',
    displayName: 'Dev Player',
  }
}

describe('DevPlayerSwitcher.vue', () => {
  it('does not render in production mode', () => {
    vi.stubEnv('DEV', false)
    const wrapper = mount(DevPlayerSwitcher)
    expect(wrapper.find('.dev-switcher').exists()).toBe(false)
  })

  it('renders the toggle button in dev mode', () => {
    const wrapper = mount(DevPlayerSwitcher)
    expect(wrapper.find('.dev-switcher__toggle').exists()).toBe(true)
    expect(wrapper.find('.dev-switcher__icon').exists()).toBe(true)
  })

  it('expands when the toggle is clicked', async () => {
    const wrapper = mount(DevPlayerSwitcher)
    await wrapper.find('.dev-switcher__toggle').trigger('click')
    expect(wrapper.find('.dev-switcher__panel').exists()).toBe(true)
  })

  it('shows current player info when authenticated', async () => {
    mockAuthStore.player = mockPlayer()
    const wrapper = mount(DevPlayerSwitcher)
    await wrapper.find('.dev-switcher__toggle').trigger('click')
    await flushPromises()

    expect(wrapper.text()).toContain('Dev Player')
    expect(wrapper.text()).toContain('dev@example.com')
  })

  it('shows not logged in when no player', async () => {
    const wrapper = mount(DevPlayerSwitcher)
    await wrapper.find('.dev-switcher__toggle').trigger('click')
    await flushPromises()

    expect(wrapper.text()).toContain('Not logged in')
  })

  it('calls devLogin with the email when the dev login button is clicked', async () => {
    const wrapper = mount(DevPlayerSwitcher)
    await wrapper.find('.dev-switcher__toggle').trigger('click')

    const input = wrapper.find('#dev-email')
    await input.setValue('tester@example.com')
    await wrapper.find('#dev-login-button').trigger('click')

    expect(mockAuthStore.devLogin).toHaveBeenCalledWith('tester@example.com')
  })

  it('calls seedGame and shows the seeded players list', async () => {
    const seeded = [
      { player: { id: 'p1', email: 'a@b.c', displayName: 'Alice' }, token: 't1' },
      { player: { id: 'p2', email: 'b@b.c', displayName: 'Bob' }, token: 't2' },
    ]
    mockApi.seedGame.mockResolvedValue({ game: { id: 'g1' }, players: seeded })

    const wrapper = mount(DevPlayerSwitcher)
    await wrapper.find('.dev-switcher__toggle').trigger('click')

    await wrapper.find('#dev-player-count').setValue('3')
    await wrapper.find('#dev-seed-game-button').trigger('click')
    await flushPromises()

    expect(mockApi.seedGame).toHaveBeenCalledWith(3)
    expect(wrapper.text()).toContain('Alice')
    expect(wrapper.text()).toContain('Bob')
  })

  it('sets token and reloads when switching to a seeded player', async () => {
    const player = mockPlayer()
    mockApi.seedGame.mockResolvedValue({
      game: { id: 'g1' },
      players: [{ player, token: 'seed-token' }],
    })
    const reload = vi.fn()
    Object.defineProperty(window, 'location', {
      value: { reload },
      writable: true,
    })

    const wrapper = mount(DevPlayerSwitcher)
    await wrapper.find('.dev-switcher__toggle').trigger('click')
    await wrapper.find('#dev-seed-game-button').trigger('click')
    await flushPromises()

    await wrapper.find('.dev-switcher__switch-button').trigger('click')

    expect(mockApi.setToken).toHaveBeenCalledWith('seed-token')
    expect(mockAuthStore.player).toEqual(player)
    expect(reload).toHaveBeenCalled()
  })

  it('shows an error when devLogin fails', async () => {
    mockAuthStore.devLogin.mockRejectedValue(new Error('Dev login failed'))
    const wrapper = mount(DevPlayerSwitcher)
    await wrapper.find('.dev-switcher__toggle').trigger('click')
    await wrapper.find('#dev-login-button').trigger('click')
    await flushPromises()

    expect(wrapper.text()).toContain('Dev login failed')
  })

  it('navigates to the dev dashboard when the dashboard link is clicked', async () => {
    const wrapper = mount(DevPlayerSwitcher)
    await wrapper.find('.dev-switcher__toggle').trigger('click')
    await wrapper.find('#dev-open-dashboard').trigger('click')

    expect(mockRouterPush).toHaveBeenCalledWith('/dev/dashboard')
  })

  it('disables quick action buttons when no game ID is set', async () => {
    const wrapper = mount(DevPlayerSwitcher)
    await wrapper.find('.dev-switcher__toggle').trigger('click')

    const buttons = wrapper.findAll('.dev-switcher__quick-button')
    expect(buttons.length).toBe(4)
    for (const button of buttons) {
      expect(button.attributes('disabled')).toBeDefined()
    }
  })

  it('ends the current game when the end game button is clicked', async () => {
    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(true)
    mockApi.devEndGame.mockResolvedValue({ id: 'g1', status: 2 })

    const wrapper = mount(DevPlayerSwitcher)
    await wrapper.find('.dev-switcher__toggle').trigger('click')
    await wrapper.find('#dev-quick-game-id').setValue('g1')

    const endGameButton = wrapper.findAll('.dev-switcher__quick-button')[3]
    await endGameButton.trigger('click')
    await flushPromises()

    expect(confirmSpy).toHaveBeenCalledWith('End the current game? This cannot be undone.')
    expect(mockApi.devEndGame).toHaveBeenCalledWith('g1')
    expect(wrapper.text()).toContain('Game ended')

    confirmSpy.mockRestore()
  })
})
