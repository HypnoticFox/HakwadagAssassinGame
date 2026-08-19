import { beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'

import { api } from '@/api/client'
import { clearToasts } from '@/composables/useToast'
import Modal from '@/components/Modal.vue'
import GameAdminView from '@/views/GameAdminView.vue'
import { GameRole, GameStatus, type GameDto, type GamePlayerDto } from '@/types'
import { withI18n } from '../helpers/i18n'

vi.mock('@/api/client', () => ({
  api: {
    getToken: vi.fn(() => null),
    getGame: vi.fn(),
    getGamePlayers: vi.fn(),
    removeAdmin: vi.fn(),
    addAdmin: vi.fn(),
  },
}))

vi.mock('@/composables/useSignalR', () => ({
  useGameSignalR: vi.fn(),
}))

vi.mock('@/composables/useSafeTime', () => ({
  formatTimeOfDay: vi.fn((iso: string) => iso),
  localTimeToDateTimeOffset: vi.fn((value: string) => value),
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
    myRole: GameRole.Creator,
    safeTimeBlocks: [],
    ...overrides,
  }
}

function makePlayer(overrides: Partial<GamePlayerDto> = {}): GamePlayerDto {
  return {
    playerId: 'p1',
    displayName: 'Player One',
    email: 'player1@example.com',
    role: GameRole.Player,
    ...overrides,
  }
}

beforeEach(() => {
  setActivePinia(createPinia())
  vi.clearAllMocks()
  clearToasts()

  vi.mocked(api.getGame).mockResolvedValue(makeGame())
  vi.mocked(api.getGamePlayers).mockResolvedValue([])
  vi.mocked(api.removeAdmin).mockResolvedValue(undefined)
  vi.mocked(api.addAdmin).mockResolvedValue(undefined)
})

async function mountView() {
  const i18nOptions = withI18n()
  const wrapper = mount(GameAdminView, {
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

describe('GameAdminView.vue', () => {
  describe('Remove moderator confirmation', () => {
    function mockAdminGame() {
      vi.mocked(api.getGame).mockResolvedValue(
        makeGame({ myRole: GameRole.Creator, status: GameStatus.Active }),
      )
      vi.mocked(api.getGamePlayers).mockResolvedValue([
        makePlayer({ playerId: 'p1', displayName: 'Creator', role: GameRole.Creator }),
        makePlayer({ playerId: 'p2', displayName: 'Moderator', role: GameRole.CoAdmin }),
      ])
    }

    async function openRemoveModal(wrapper: Awaited<ReturnType<typeof mountView>>) {
      const removeButton = wrapper
        .findAll('.player-management-action')
        .find((button) => button.text() === 'Remove')
      expect(removeButton).toBeDefined()
      await removeButton!.trigger('click')
      await flushPromises()
    }

    it('opens a confirmation modal instead of removing the moderator immediately', async () => {
      mockAdminGame()

      const wrapper = await mountView()

      await openRemoveModal(wrapper)

      const modal = wrapper.findComponent(Modal)
      expect(modal.props('open')).toBe(true)
      expect(api.removeAdmin).not.toHaveBeenCalled()

      wrapper.unmount()
    })

    it('calls removeAdmin when the confirm button is clicked', async () => {
      mockAdminGame()

      const wrapper = await mountView()

      await openRemoveModal(wrapper)

      const footerButtons = document.body.querySelectorAll('.modal-footer button')
      expect(footerButtons).toHaveLength(2)
      ;(footerButtons[1] as HTMLElement).click()
      await flushPromises()

      expect(api.removeAdmin).toHaveBeenCalledWith('g1', 'p2')

      wrapper.unmount()
    })

    it('does not call removeAdmin when the cancel button is clicked', async () => {
      mockAdminGame()

      const wrapper = await mountView()

      await openRemoveModal(wrapper)

      const footerButtons = document.body.querySelectorAll('.modal-footer button')
      expect(footerButtons).toHaveLength(2)
      ;(footerButtons[0] as HTMLElement).click()
      await flushPromises()

      expect(api.removeAdmin).not.toHaveBeenCalled()

      wrapper.unmount()
    })
  })
})