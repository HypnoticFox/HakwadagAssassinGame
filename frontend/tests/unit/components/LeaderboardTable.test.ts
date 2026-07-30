import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import LeaderboardTable from '@/components/LeaderboardTable.vue'
import type { LeaderboardEntryDto } from '@/types'
import { withI18n } from '../helpers/i18n'

function makeEntry(overrides: Partial<LeaderboardEntryDto> = {}): LeaderboardEntryDto {
  return {
    player: { id: 'p1', email: 'a@b.c', displayName: 'Alice', avatarUrl: undefined },
    score: 100,
    tags: 2,
    ...overrides,
  }
}

describe('LeaderboardTable.vue', () => {
  it('renders table headers', () => {
    const wrapper = mount(LeaderboardTable, {
      props: { entries: [] },
      ...withI18n(),
    })
    const headers = wrapper.findAll('th')
    expect(headers).toHaveLength(4)
    expect(headers[0].text()).toBe('Rank')
    expect(headers[1].text()).toBe('Player')
    expect(headers[2].text()).toBe('Tags')
    expect(headers[3].text()).toBe('Score')
  })

  it('renders player entries with correct rank', () => {
    const entries = [
      makeEntry({ player: { id: 'p1', displayName: 'Alice', email: 'a@b.c' }, score: 100, tags: 5 }),
      makeEntry({ player: { id: 'p2', displayName: 'Bob', email: 'b@c.d' }, score: 50, tags: 3 }),
    ]
    const wrapper = mount(LeaderboardTable, {
      props: { entries },
      ...withI18n(),
    })
    const rows = wrapper.findAll('tbody tr')
    expect(rows).toHaveLength(2)
    expect(rows[0].text()).toContain('1')
    expect(rows[0].text()).toContain('Alice')
    expect(rows[0].text()).toContain('5')
    expect(rows[0].text()).toContain('100')
    expect(rows[1].text()).toContain('2')
    expect(rows[1].text()).toContain('Bob')
    expect(rows[1].text()).toContain('3')
    expect(rows[1].text()).toContain('50')
  })

  it('displays player avatar initial when no avatarUrl', () => {
    const entries = [
      makeEntry({ player: { id: 'p1', displayName: 'Charlie', email: 'c@d.e' } }),
    ]
    const wrapper = mount(LeaderboardTable, {
      props: { entries },
      ...withI18n(),
    })
    // The avatar should show the first letter of the display name
    expect(wrapper.find('.player-avatar span').exists()).toBe(true)
    expect(wrapper.find('.player-avatar span').text()).toBe('C')
  })

  it('renders img when avatarUrl is present', () => {
    const entries = [
      makeEntry({
        player: {
          id: 'p1',
          displayName: 'Dave',
          email: 'd@e.f',
          avatarUrl: 'https://example.com/avatar.png',
        },
      }),
    ]
    const wrapper = mount(LeaderboardTable, {
      props: { entries },
      ...withI18n(),
    })
    const img = wrapper.find('.player-avatar img')
    expect(img.exists()).toBe(true)
    expect(img.attributes('src')).toBe('https://example.com/avatar.png')
    expect(img.attributes('alt')).toBe('Dave')
  })

  describe('first place styling', () => {
    it('adds leader-row class to first place row', () => {
      const entries = [
        makeEntry({ player: { id: 'p1', displayName: 'Alice', email: 'a@b.c' }, score: 100, tags: 5 }),
        makeEntry({ player: { id: 'p2', displayName: 'Bob', email: 'b@c.d' }, score: 50, tags: 3 }),
      ]
      const wrapper = mount(LeaderboardTable, {
        props: { entries },
        ...withI18n(),
      })
      const rows = wrapper.findAll('tbody tr')
      expect(rows[0].classes()).toContain('leader-row')
      expect(rows[1].classes()).not.toContain('leader-row')
    })

    it('does not add leader-row when entries is empty', () => {
      const wrapper = mount(LeaderboardTable, {
        props: { entries: [] },
        ...withI18n(),
      })
      expect(wrapper.find('tbody').exists()).toBe(true)
    })

    it('only first row has leader-row class', () => {
      const entry = makeEntry({ player: { id: 'p1', displayName: 'Alice', email: 'a@b.c' } })
      const wrapper = mount(LeaderboardTable, {
        props: { entries: [entry] },
        ...withI18n(),
      })
      const row = wrapper.find('tbody tr')
      expect(row.classes()).toContain('leader-row')
    })
  })

  it('renders empty table body when no entries', () => {
    const wrapper = mount(LeaderboardTable, {
      props: { entries: [] },
      ...withI18n(),
    })
    expect(wrapper.findAll('tbody tr')).toHaveLength(0)
  })
})
