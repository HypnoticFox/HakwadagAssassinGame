import { describe, it, expect, beforeEach, vi } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'

import DevDashboardView from '@/views/DevDashboardView.vue'
import { withI18n } from '../helpers/i18n'

const { mockApi } = vi.hoisted(() => {
  return {
    mockApi: {
      devGetGames: vi.fn(),
      devGetGamePlayers: vi.fn(),
      devGetGameAssignments: vi.fn(),
      devGetGameTags: vi.fn(),
    },
  }
})

vi.mock('@/api/client', () => ({
  api: mockApi,
}))

beforeEach(() => {
  vi.clearAllMocks()
})

function mockGame() {
  return {
    id: 'g1',
    name: 'Test Game',
    status: 1,
    playerCount: 2,
    createdAt: '2026-01-01T00:00:00Z',
  }
}

describe('DevDashboardView.vue', () => {
  it('loads and renders the list of games on mount', async () => {
    mockApi.devGetGames.mockResolvedValue([mockGame()])

    const wrapper = mount(DevDashboardView, withI18n())
    await flushPromises()

    expect(mockApi.devGetGames).toHaveBeenCalled()
    expect(wrapper.text()).toContain('Test Game')
    expect(wrapper.text()).toContain('2 players')
  })

  it('expands a game and loads its details when clicked', async () => {
    mockApi.devGetGames.mockResolvedValue([mockGame()])
    mockApi.devGetGamePlayers.mockResolvedValue([
      {
        playerId: 'p1',
        email: 'a@b.c',
        displayName: 'Alice',
        role: 0,
        score: 10,
        isActive: true,
        isParticipating: true,
      },
    ])
    mockApi.devGetGameAssignments.mockResolvedValue([])
    mockApi.devGetGameTags.mockResolvedValue([])

    const wrapper = mount(DevDashboardView, withI18n())
    await flushPromises()

    await wrapper.find('.dev-dashboard__game-header').trigger('click')
    await flushPromises()

    expect(mockApi.devGetGamePlayers).toHaveBeenCalledWith('g1')
    expect(mockApi.devGetGameAssignments).toHaveBeenCalledWith('g1')
    expect(mockApi.devGetGameTags).toHaveBeenCalledWith('g1')
    expect(wrapper.text()).toContain('Alice')
  })

  it('displays an error when loading games fails', async () => {
    mockApi.devGetGames.mockRejectedValue(new Error('Network error'))

    const wrapper = mount(DevDashboardView, withI18n())
    await flushPromises()

    expect(wrapper.text()).toContain('Network error')
  })
})
