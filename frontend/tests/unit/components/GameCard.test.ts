import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import GameCard from '@/components/GameCard.vue'
import { GameStatus, type GameRole, type GameDto } from '@/types'
import { withI18n } from '../helpers/i18n'

function makeGame(overrides: Partial<GameDto> = {}): GameDto {
  return {
    id: 'g1',
    name: 'Test Game',
    inviteCode: 'ABC',
    status: GameStatus.NotStarted,
    createdAt: '2024-01-01',
    maxPlayers: 10,
    basePointsPerTag: 100,
    confirmationTimeout: '01:00:00',
    playerCount: 3,
    myRole: 0 as GameRole,
    safeTimeBlocks: [],
    ...overrides,
  }
}

describe('GameCard.vue', () => {
  it('renders game name', () => {
    const wrapper = mount(GameCard, {
      props: { game: makeGame({ name: 'Epic Battle' }) },
      ...withI18n(),
    })
    expect(wrapper.text()).toContain('Epic Battle')
  })

  it('renders player count and max players', () => {
    const wrapper = mount(GameCard, {
      props: { game: makeGame({ playerCount: 3, maxPlayers: 10 }) },
      ...withI18n(),
    })
    expect(wrapper.text()).toContain('3 / 10 players')
  })

  it('renders base points per tag', () => {
    const wrapper = mount(GameCard, {
      props: { game: makeGame({ basePointsPerTag: 150 }) },
      ...withI18n(),
    })
    expect(wrapper.text()).toContain('150 pts per tag')
  })

  describe('status display', () => {
    it('shows "Not started" for NotStarted status', () => {
      const wrapper = mount(GameCard, {
        props: { game: makeGame({ status: GameStatus.NotStarted }) },
        ...withI18n(),
      })
      expect(wrapper.text()).toContain('Not started')
    })

    it('shows "Active" for Active status', () => {
      const wrapper = mount(GameCard, {
        props: { game: makeGame({ status: GameStatus.Active }) },
        ...withI18n(),
      })
      expect(wrapper.text()).toContain('Active')
    })

    it('shows "Ended" for Ended status', () => {
      const wrapper = mount(GameCard, {
        props: { game: makeGame({ status: GameStatus.Ended }) },
        ...withI18n(),
      })
      expect(wrapper.text()).toContain('Ended')
    })
  })

  describe('status colors', () => {
    it('has status--not-started class for NotStarted', () => {
      const wrapper = mount(GameCard, {
        props: { game: makeGame({ status: GameStatus.NotStarted }) },
        ...withI18n(),
      })
      expect(wrapper.find('.status--not-started').exists()).toBe(true)
    })

    it('has status--active class for Active', () => {
      const wrapper = mount(GameCard, {
        props: { game: makeGame({ status: GameStatus.Active }) },
        ...withI18n(),
      })
      expect(wrapper.find('.status--active').exists()).toBe(true)
    })

    it('has status--ended class for Ended', () => {
      const wrapper = mount(GameCard, {
        props: { game: makeGame({ status: GameStatus.Ended }) },
        ...withI18n(),
      })
      expect(wrapper.find('.status--ended').exists()).toBe(true)
    })
  })

  describe('click event', () => {
    it('emits click with game id when clicked', async () => {
      const wrapper = mount(GameCard, {
        props: { game: makeGame({ id: 'game-42' }) },
        ...withI18n(),
      })
      await wrapper.trigger('click')
      expect(wrapper.emitted('click')).toHaveLength(1)
      expect(wrapper.emitted('click')![0]).toEqual(['game-42'])
    })
  })

  it('is a button element', () => {
    const wrapper = mount(GameCard, {
      props: { game: makeGame() },
      ...withI18n(),
    })
    expect(wrapper.element.tagName).toBe('BUTTON')
  })
})
