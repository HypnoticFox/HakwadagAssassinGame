import { beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { ref } from 'vue'

import { api } from '@/api/client'
import { clearToasts } from '@/composables/useToast'
import { useSafeTime } from '@/composables/useSafeTime'
import Modal from '@/components/Modal.vue'
import GameDetailView from '@/views/GameDetailView.vue'
import { GameRole, GameStatus, type GameDto } from '@/types'
import { withI18n } from '../helpers/i18n'

vi.mock('@/api/client', () => ({
  api: {
    getToken: vi.fn(() => null),
    getGame: vi.fn(),
    getLeaderboard: vi.fn(),
    endGame: vi.fn(),
    leaveGame: vi.fn(),
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

  describe('End game confirmation', () => {
    it('places the End Game button at the bottom, not in the header', async () => {
      vi.mocked(api.getGame).mockResolvedValue(
        makeGame({ myRole: GameRole.Creator, status: GameStatus.Active }),
      )

      const wrapper = await mountView()

      const endGameSection = wrapper.find('.end-game-action')
      expect(endGameSection.exists()).toBe(true)
      expect(endGameSection.text()).toContain('End game')
      expect(wrapper.find('.game-actions').text()).not.toContain('End game')

      wrapper.unmount()
    })

    it('does not show the End Game button for non-admins', async () => {
      vi.mocked(api.getGame).mockResolvedValue(
        makeGame({ myRole: GameRole.Player, status: GameStatus.Active }),
      )

      const wrapper = await mountView()

      expect(wrapper.find('.end-game-action').exists()).toBe(false)

      wrapper.unmount()
    })

    it('opens a confirmation modal instead of ending the game immediately', async () => {
      vi.mocked(api.getGame).mockResolvedValue(
        makeGame({ myRole: GameRole.Creator, status: GameStatus.Active }),
      )

      const wrapper = await mountView()

      await wrapper.find('.end-game-action button').trigger('click')
      await flushPromises()

      const modal = wrapper.findComponent(Modal)
      expect(modal.props('open')).toBe(true)
      expect(api.endGame).not.toHaveBeenCalled()

      wrapper.unmount()
    })

    it('calls endGame when the confirm button is clicked', async () => {
      vi.mocked(api.getGame).mockResolvedValue(
        makeGame({ myRole: GameRole.Creator, status: GameStatus.Active }),
      )
      vi.mocked(api.endGame).mockResolvedValue(
        makeGame({ myRole: GameRole.Creator, status: GameStatus.Ended }),
      )

      const wrapper = await mountView()

      await wrapper.find('.end-game-action button').trigger('click')
      await flushPromises()

      const footerButtons = document.body.querySelectorAll('.modal-footer button')
      expect(footerButtons).toHaveLength(2)
      ;(footerButtons[1] as HTMLElement).click()
      await flushPromises()

      expect(api.endGame).toHaveBeenCalledWith('g1')

      wrapper.unmount()
    })

    it('does not call endGame when the cancel button is clicked', async () => {
      vi.mocked(api.getGame).mockResolvedValue(
        makeGame({ myRole: GameRole.Creator, status: GameStatus.Active }),
      )

      const wrapper = await mountView()

      await wrapper.find('.end-game-action button').trigger('click')
      await flushPromises()

      const footerButtons = document.body.querySelectorAll('.modal-footer button')
      expect(footerButtons).toHaveLength(2)
      ;(footerButtons[0] as HTMLElement).click()
      await flushPromises()

      expect(api.endGame).not.toHaveBeenCalled()

      wrapper.unmount()
    })
  })

  describe('Leave button visibility', () => {
    it('shows the Leave button when the game is active and participating', async () => {
      vi.mocked(api.getGame).mockResolvedValue(
        makeGame({ myRole: GameRole.Player, status: GameStatus.Active, isParticipating: true }),
      )

      const wrapper = await mountView()

      expect(wrapper.find('.leave-section').exists()).toBe(true)
      expect(wrapper.find('.leave-section').text()).toContain('Leave game')

      wrapper.unmount()
    })

    it('does not show the Leave button when the game has not started', async () => {
      vi.mocked(api.getGame).mockResolvedValue(
        makeGame({ myRole: GameRole.Player, status: GameStatus.NotStarted }),
      )

      const wrapper = await mountView()

      expect(wrapper.find('.leave-section').exists()).toBe(false)

      wrapper.unmount()
    })

    it('does not show the Leave button when the game has ended', async () => {
      vi.mocked(api.getGame).mockResolvedValue(
        makeGame({ myRole: GameRole.Player, status: GameStatus.Ended }),
      )

      const wrapper = await mountView()

      expect(wrapper.find('.leave-section').exists()).toBe(false)

      wrapper.unmount()
    })
  })

  describe('Leave game confirmation', () => {
    it('opens a confirmation modal instead of leaving immediately', async () => {
      vi.mocked(api.getGame).mockResolvedValue(
        makeGame({ myRole: GameRole.Player, status: GameStatus.Active, isParticipating: true }),
      )

      const wrapper = await mountView()

      await wrapper.find('.leave-section button').trigger('click')
      await flushPromises()

      const modals = wrapper.findAllComponents(Modal)
      const openModal = modals.find((m) => m.props('open') === true)
      expect(openModal).toBeDefined()
      expect(api.leaveGame).not.toHaveBeenCalled()

      wrapper.unmount()
    })

    it('calls leaveGame when the confirm button is clicked', async () => {
      vi.mocked(api.getGame).mockResolvedValue(
        makeGame({ myRole: GameRole.Player, status: GameStatus.Active, isParticipating: true }),
      )
      vi.mocked(api.leaveGame).mockResolvedValue(undefined)

      const wrapper = await mountView()

      await wrapper.find('.leave-section button').trigger('click')
      await flushPromises()

      const footerButtons = document.body.querySelectorAll('.modal-footer button')
      expect(footerButtons).toHaveLength(2)
      ;(footerButtons[1] as HTMLElement).click()
      await flushPromises()

      expect(api.leaveGame).toHaveBeenCalledWith('g1')

      wrapper.unmount()
    })

    it('does not call leaveGame when the cancel button is clicked', async () => {
      vi.mocked(api.getGame).mockResolvedValue(
        makeGame({ myRole: GameRole.Player, status: GameStatus.Active, isParticipating: true }),
      )

      const wrapper = await mountView()

      await wrapper.find('.leave-section button').trigger('click')
      await flushPromises()

      const footerButtons = document.body.querySelectorAll('.modal-footer button')
      expect(footerButtons).toHaveLength(2)
      ;(footerButtons[0] as HTMLElement).click()
      await flushPromises()

      expect(api.leaveGame).not.toHaveBeenCalled()

      wrapper.unmount()
    })
  })
})
