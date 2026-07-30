import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import PlayerList from '@/components/PlayerList.vue'
import type { PlayerDto } from '@/types'
import { withI18n } from '../helpers/i18n'

function makePlayer(overrides: Partial<PlayerDto> = {}): PlayerDto {
  return {
    id: 'p1',
    email: 'a@b.c',
    displayName: 'Alice',
    ...overrides,
  }
}

describe('PlayerList.vue', () => {
  describe('player rendering', () => {
    it('renders a list of players', () => {
      const players = [
        makePlayer({ id: 'p1', displayName: 'Alice', email: 'alice@test.com' }),
        makePlayer({ id: 'p2', displayName: 'Bob', email: 'bob@test.com' }),
      ]
      const wrapper = mount(PlayerList, {
        props: { players },
        ...withI18n(),
      })
      const items = wrapper.findAll('.player-item')
      expect(items).toHaveLength(2)
    })

    it('displays player display name', () => {
      const players = [makePlayer({ displayName: 'Charlie' })]
      const wrapper = mount(PlayerList, {
        props: { players },
        ...withI18n(),
      })
      expect(wrapper.text()).toContain('Charlie')
    })

    it('displays player email', () => {
      const players = [makePlayer({ email: 'charlie@example.com' })]
      const wrapper = mount(PlayerList, {
        props: { players },
        ...withI18n(),
      })
      expect(wrapper.text()).toContain('charlie@example.com')
    })

    it('shows initial letter when no avatarUrl', () => {
      const players = [makePlayer({ displayName: 'Dave' })]
      const wrapper = mount(PlayerList, {
        props: { players },
        ...withI18n(),
      })
      const avatar = wrapper.find('.player-avatar')
      expect(avatar.find('span').exists()).toBe(true)
      expect(avatar.find('span').text()).toBe('D')
    })

    it('renders img when avatarUrl is present', () => {
      const players = [
        makePlayer({
          displayName: 'Eve',
          avatarUrl: 'https://example.com/avatar.png',
        }),
      ]
      const wrapper = mount(PlayerList, {
        props: { players },
        ...withI18n(),
      })
      const img = wrapper.find('.player-avatar img')
      expect(img.exists()).toBe(true)
      expect(img.attributes('src')).toBe('https://example.com/avatar.png')
      expect(img.attributes('alt')).toBe('Eve')
    })
  })

  describe('empty state', () => {
    it('shows default empty text when no players', () => {
      const wrapper = mount(PlayerList, {
        props: { players: [] },
        ...withI18n(),
      })
      expect(wrapper.find('.player-list-empty').exists()).toBe(true)
      expect(wrapper.text()).toBe('No players yet.')
    })

    it('shows custom empty text when provided', () => {
      const wrapper = mount(PlayerList, {
        props: { players: [], emptyText: 'Nobody has joined yet.' },
        ...withI18n(),
      })
      expect(wrapper.text()).toBe('Nobody has joined yet.')
    })

    it('does not show player list when empty', () => {
      const wrapper = mount(PlayerList, {
        props: { players: [] },
        ...withI18n(),
      })
      expect(wrapper.find('.player-list').exists()).toBe(false)
    })

    it('does not show empty text when there are players', () => {
      const players = [makePlayer()]
      const wrapper = mount(PlayerList, {
        props: { players },
        ...withI18n(),
      })
      expect(wrapper.find('.player-list-empty').exists()).toBe(false)
    })
  })
})
