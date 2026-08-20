import { describe, it, expect, beforeEach, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'

import TagConfirmView from '@/views/TagConfirmView.vue'
import { api } from '@/api/client'
import { TagStatus, type TagSubmissionDto } from '@/types'
import { withI18n } from '../helpers/i18n'

const { mockGameStore, mockTagStore, mockPush, mockGetPendingTag, mockRoute } = vi.hoisted(() => ({
  mockGameStore: {
    loadGame: vi.fn(),
    loadGamePlayers: vi.fn(),
    gamePlayers: [] as Array<{ playerId: string; displayName: string; email: string; role: number }>,
  },
  mockTagStore: {
    pendingTag: null as TagSubmissionDto | null,
    isLoading: false,
    loadPendingTag: vi.fn(),
    confirmTag: vi.fn(),
    denyTag: vi.fn(),
    voidTag: vi.fn(),
    dequeuePendingTag: vi.fn(),
  },
  mockPush: vi.fn(),
  mockGetPendingTag: vi.fn(),
  // require('vue') resolves synchronously — the ESM import binding isn't
  // initialized yet inside vi.hoisted, so we can't use the imported reactive.
  mockRoute: require('vue').reactive({ params: { id: 'g1', tagId: 'tag1' } }),
}))

vi.mock('@/api/client', () => ({
  api: {
    getPendingTag: mockGetPendingTag,
    confirmTag: vi.fn(),
    denyTag: vi.fn(),
    voidTag: vi.fn(),
  },
}))

vi.mock('@/composables/useSignalR', () => ({
  useGameSignalR: vi.fn(),
}))

vi.mock('vue-router', () => ({
  useRoute: () => mockRoute,
  useRouter: () => ({ push: mockPush }),
}))

vi.mock('@/stores', () => ({
  useGameStore: () => mockGameStore,
  useTagStore: () => mockTagStore,
}))

function makeTag(overrides: Partial<TagSubmissionDto> = {}): TagSubmissionDto {
  return {
    id: 'tag1',
    assignmentId: 'a1',
    hunterId: 'hunter1',
    targetId: 'target1',
    conditionId: 'c1',
    status: TagStatus.Pending,
    submittedAt: '2024-01-01T00:00:00Z',
    ...overrides,
  }
}

beforeEach(() => {
  vi.clearAllMocks()
  mockTagStore.pendingTag = makeTag()
  mockTagStore.isLoading = false
  mockRoute.params = { id: 'g1', tagId: 'tag1' }
  mockGameStore.loadGame.mockResolvedValue(undefined)
  mockGameStore.loadGamePlayers.mockResolvedValue(undefined)
  mockGameStore.gamePlayers = [
    { playerId: 'hunter1', displayName: 'Alice', email: 'alice@test.com', role: 0 },
    { playerId: 'target1', displayName: 'Bob', email: 'bob@test.com', role: 0 },
  ]
  mockGetPendingTag.mockResolvedValue(mockTagStore.pendingTag)
  mockTagStore.loadPendingTag.mockImplementation(async (gameId: string) => {
    const tag = await api.getPendingTag(gameId)
    mockTagStore.pendingTag = tag
    return tag
  })
  mockTagStore.confirmTag.mockResolvedValue(makeTag({ status: TagStatus.Confirmed }))
  mockTagStore.denyTag.mockResolvedValue(makeTag({ status: TagStatus.Denied }))
  mockTagStore.voidTag.mockResolvedValue(makeTag({ status: TagStatus.Voided }))
  mockTagStore.dequeuePendingTag.mockReturnValue(null)
  mockPush.mockResolvedValue(undefined)
})

async function mountView() {
  const wrapper = mount(TagConfirmView, withI18n())
  await flushPromises()
  return wrapper
}

function findAction(wrapper: ReturnType<typeof mount>, className: string) {
  return wrapper.find(`button.${className}`)
}

describe('TagConfirmView.vue', () => {
  it('renders the tag confirmation when the pending tag matches the route', async () => {
    const wrapper = await mountView()

    expect(wrapper.find('.tag-card').exists()).toBe(true)
    expect(wrapper.text()).toContain('Pending tag')
  })

  it('shows hunter and target display names instead of IDs', async () => {
    const wrapper = await mountView()

    expect(wrapper.text()).toContain('Alice')
    expect(wrapper.text()).toContain('Bob')
    expect(wrapper.text()).not.toContain('hunter1')
    expect(wrapper.text()).not.toContain('target1')
  })

  it('falls back to player ID when display name is not found', async () => {
    mockGameStore.gamePlayers = []
    const wrapper = await mountView()

    expect(wrapper.text()).toContain('hunter1')
    expect(wrapper.text()).toContain('target1')
  })

  it('shows a loading screen instead of content after confirm', async () => {
    const wrapper = await mountView()
    mockGetPendingTag.mockResolvedValue(null)

    // Make confirmTag resolve but delay navigation by holding push
    let resolvePush!: () => void
    mockPush.mockReturnValue(new Promise<void>((resolve) => (resolvePush = resolve)))

    await findAction(wrapper, 'button--primary').trigger('click')
    await flushPromises()

    // While navigation is pending, the loading screen should show
    // and all tag content should be hidden
    expect(wrapper.find('.loading-screen').exists()).toBe(true)
    expect(wrapper.find('.tag-card').exists()).toBe(false)
    expect(wrapper.text()).not.toContain('Void tag')
    expect(wrapper.text()).not.toContain('Pending tag')

    // Complete navigation
    resolvePush()
    await flushPromises()
  })

  it('confirms a tag and navigates to the leaderboard when the queue is empty', async () => {
    const wrapper = await mountView()
    mockGetPendingTag.mockResolvedValue(null)

    await findAction(wrapper, 'button--primary').trigger('click')
    await flushPromises()

    expect(mockTagStore.confirmTag).toHaveBeenCalledWith('g1', 'tag1')
    expect(mockPush).toHaveBeenCalledWith('/games/g1/leaderboard')
  })

  it('confirms a tag and navigates to the next queued tag', async () => {
    mockTagStore.dequeuePendingTag.mockReturnValue({
      gameId: 'g2',
      tag: makeTag({ id: 'tag2' }),
    })
    const wrapper = await mountView()

    await findAction(wrapper, 'button--primary').trigger('click')
    await flushPromises()

    expect(mockTagStore.confirmTag).toHaveBeenCalledWith('g1', 'tag1')
    expect(mockPush).toHaveBeenCalledWith('/games/g2/tag/tag2')
  })

  it('confirms a tag and navigates to the next API pending tag when the queue is empty', async () => {
    const nextTag = makeTag({ id: 'tag2' })
    const wrapper = await mountView()
    mockGetPendingTag.mockResolvedValue(nextTag)

    await findAction(wrapper, 'button--primary').trigger('click')
    await flushPromises()

    expect(mockPush).toHaveBeenCalledWith('/games/g1/tag/tag2')
  })

  it('denies a tag and navigates to the leaderboard when the queue is empty', async () => {
    const wrapper = await mountView()
    mockGetPendingTag.mockResolvedValue(null)

    await findAction(wrapper, 'button--secondary').trigger('click')
    await flushPromises()

    expect(mockTagStore.denyTag).toHaveBeenCalledWith('g1', 'tag1')
    expect(mockPush).toHaveBeenCalledWith('/games/g1/leaderboard')
  })

  it('denies a tag and navigates to the next queued tag', async () => {
    mockTagStore.dequeuePendingTag.mockReturnValue({
      gameId: 'g2',
      tag: makeTag({ id: 'tag2' }),
    })
    const wrapper = await mountView()

    await findAction(wrapper, 'button--secondary').trigger('click')
    await flushPromises()

    expect(mockTagStore.denyTag).toHaveBeenCalledWith('g1', 'tag1')
    expect(mockPush).toHaveBeenCalledWith('/games/g2/tag/tag2')
  })

  it('resets the loading screen and reloads data when navigating to a queued tag', async () => {
    mockTagStore.dequeuePendingTag.mockReturnValue({
      gameId: 'g2',
      tag: makeTag({ id: 'tag2' }),
    })
    const wrapper = await mountView()

    // Confirm the tag — this sets isResolving=true and navigates to tag2
    await findAction(wrapper, 'button--primary').trigger('click')
    await flushPromises()

    // Simulate the route change to the new tag (same component, different params)
    mockRoute.params = { id: 'g2', tagId: 'tag2' }
    mockTagStore.pendingTag = makeTag({ id: 'tag2' })
    mockGetPendingTag.mockResolvedValue(makeTag({ id: 'tag2' }))
    await flushPromises()

    // The loading screen should be gone and the new tag content should show
    expect(wrapper.find('.loading-screen').exists()).toBe(false)
    expect(wrapper.find('.tag-card').exists()).toBe(true)
    // loadPendingTag should have been called again for the new tag
    expect(mockTagStore.loadPendingTag).toHaveBeenCalledWith('g2')
  })
})
