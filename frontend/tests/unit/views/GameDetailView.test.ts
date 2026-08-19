import { beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { ref } from 'vue'

import { api } from '@/api/client'
import { clearToasts } from '@/composables/useToast'
import { useSafeTime } from '@/composables/useSafeTime'
import GameDetailView from '@/views/GameDetailView.vue'
import { GameStatus, type GameDto } from '@/types'
import { withI18n } from '../helpers/i18n'

vi.mock('@/api/client', () => ({
  api: {
    getToken: vi.fn(() => null),
    getGame: vi.fn(),
    getLeaderboard: vi.fn(),
  },
}))

vi.mock('@/composables/useSignalR', () => ({
  useGameSignalR: vi.fn(),
}))

vi.mock('@/composables/useSafeTime', () => ({
  useSafeTime: vi.fn(),
  formatTimeOfDay: vi.fn((iso: string) => {
    const date = new Date(iso)
    return `${String(date.getHours()).padStart(2, '0')}:${String(date.getMinutes()).padStart(2, '0')}`
  }),
}))

vi.mock('vue-router', () => ({
  useRoute: () => ({ params: { id: 'g1' } }),
  useRouter: () => ({ push: vi.fn() }),
}))

function makeGame(overrides: Partial<GameDto> = {}): GameDto {
  return {
    id: 'g1',
    name: 'Test Game',
    inviteCode: 'ABC123',
    status: GameStatus.Active,
    createdAt: '2024-01-01T00:00:00Z',
    maxPlayers: 10,
    basePointsPerTag: 100,
    confirmationTimeout: '00:05:00',
    assignmentCooldownMinutes: 30,
    playerCount: 5,
    participantCount: 4,
    isParticipating: true,
    myRole: 0,
    safeTimeBlocks: [],
    ...overrides,
  }
}

beforeEach(() => {
  setActivePinia(createPinia())
  vi.clearAllMocks()
  clearToasts()

  vi.mocked(api.getGame).mockResolvedValue(makeGame())
  vi.mocked(api.getLeaderboard).mockResolvedValue([])
  vi.mocked(useSafeTime).mockReturnValue({
    isInSafeTime: ref(false),
    currentBlock: ref(null),
  })
})

async function mountView() {
  const i18nOptions = withI18n()
  const wrapper = mount(GameDetailView, {
    ...i18nOptions,
    global: {
      ...i18nOptions.global,
      stubs: {
        Input: true,
      },
    },
  })
  await flushPromises()
  return wrapper
}

describe('GameDetailView.vue', () => {
  it('shows the safe time banner when safe time is active and the game is active', async () => {
    vi.mocked(useSafeTime).mockReturnValue({
      isInSafeTime: ref(true),
      currentBlock: ref({ id: 'safe-1', startTime: '2025-06-15T22:00:00+00:00', endTime: '2025-06-15T06:00:00+00:00' }),
    })
    vi.mocked(api.getGame).mockResolvedValue(
      makeGame({
        safeTimeBlocks: [{ id: 'safe-1', startTime: '2025-06-15T22:00:00+00:00', endTime: '2025-06-15T06:00:00+00:00' }],
      }),
    )

    const wrapper = await mountView()
    const banner = wrapper.find('.safe-time-banner')

    expect(banner.exists()).toBe(true)
    const expectedEnd = new Date('2025-06-15T06:00:00+00:00')
    const expectedEndText = `${String(expectedEnd.getHours()).padStart(2, '0')}:${String(expectedEnd.getMinutes()).padStart(2, '0')}`
    expect(banner.text()).toContain(`Safe time is active until ${expectedEndText}`)

    wrapper.unmount()
  })

  it('does not show the safe time banner when safe time is not active', async () => {
    vi.mocked(useSafeTime).mockReturnValue({
      isInSafeTime: ref(false),
      currentBlock: ref(null),
    })

    const wrapper = await mountView()

    expect(wrapper.find('.safe-time-banner').exists()).toBe(false)

    wrapper.unmount()
  })

  it('does not show the safe time banner when the game is not active', async () => {
    vi.mocked(useSafeTime).mockReturnValue({
      isInSafeTime: ref(true),
      currentBlock: ref({ id: 'safe-1', startTime: '2025-06-15T22:00:00+00:00', endTime: '2025-06-15T06:00:00+00:00' }),
    })
    vi.mocked(api.getGame).mockResolvedValue(makeGame({ status: GameStatus.NotStarted }))

    const wrapper = await mountView()

    expect(wrapper.find('.safe-time-banner').exists()).toBe(false)

    wrapper.unmount()
  })

  it('disables the assignment button when safe time is active', async () => {
    vi.mocked(useSafeTime).mockReturnValue({
      isInSafeTime: ref(true),
      currentBlock: ref({ id: 'safe-1', startTime: '2025-06-15T22:00:00+00:00', endTime: '2025-06-15T06:00:00+00:00' }),
    })

    const wrapper = await mountView()
    const assignmentButton = wrapper.find('.assignment-button')

    expect(assignmentButton.exists()).toBe(true)
    expect(assignmentButton.attributes('disabled')).toBeDefined()

    wrapper.unmount()
  })
})
