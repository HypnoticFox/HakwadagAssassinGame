import { describe, it, expect, beforeEach, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'
import { defineComponent, h, nextTick, reactive } from 'vue'

import { api } from '@/api/client'
import { usePendingTagRedirect } from '@/composables/usePendingTagRedirect'
import { GameStatus, TagStatus, type TagSubmissionDto } from '@/types'
import { withI18n } from '../helpers/i18n'

const { mockGameStore, mockTagStore, mockRouter } = vi.hoisted(() => ({
  mockGameStore: {
    recentGames: [] as Array<{ id: string; status: GameStatus }>,
  },
  mockTagStore: {
    pendingTagQueue: [] as Array<{ gameId: string; tag: TagSubmissionDto }>,
    setPendingTag: vi.fn(),
    dequeuePendingTag: vi.fn(),
  },
  mockRouter: {
    currentRoute: { value: { name: 'home' as string } },
    push: vi.fn(),
  },
}))

vi.mock('vue-router', () => ({
  useRouter: () => mockRouter,
}))

vi.mock('@/stores', () => ({
  useGameStore: () => mockGameStore,
  useTagStore: () => mockTagStore,
}))

vi.mock('@/api/client', () => ({
  api: {
    getPendingTag: vi.fn(),
  },
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

function withSetup() {
  let composable: ReturnType<typeof usePendingTagRedirect>
  const component = defineComponent({
    setup() {
      composable = usePendingTagRedirect()
      return () => h('div')
    },
  })
  const wrapper = mount(component, withI18n())
  return { wrapper, composable: composable! }
}

beforeEach(() => {
  vi.clearAllMocks()
  mockGameStore.recentGames = []
  mockTagStore.pendingTagQueue = reactive([])
  mockTagStore.dequeuePendingTag.mockImplementation(
    () => mockTagStore.pendingTagQueue.shift() ?? null,
  )
  mockRouter.currentRoute.value.name = 'home'
})

describe('usePendingTagRedirect', () => {
  it('does not check games when already on the tag confirmation route', async () => {
    mockRouter.currentRoute.value.name = 'tag-confirm'
    mockGameStore.recentGames = [{ id: 'g1', status: GameStatus.Active }]
    const { wrapper, composable } = withSetup()

    await composable.checkPendingTags()

    expect(api.getPendingTag).not.toHaveBeenCalled()
    expect(mockRouter.push).not.toHaveBeenCalled()
    wrapper.unmount()
  })

  it('redirects to the first pending tag found in an active game', async () => {
    const tag = makeTag()
    mockGameStore.recentGames = [
      { id: 'g1', status: GameStatus.Active },
      { id: 'g2', status: GameStatus.Active },
    ]
    vi.mocked(api.getPendingTag).mockResolvedValueOnce(tag)
    const { wrapper, composable } = withSetup()

    await composable.checkPendingTags()

    expect(mockTagStore.setPendingTag).toHaveBeenCalledWith(tag)
    expect(mockRouter.push).toHaveBeenCalledWith('/games/g1/tag/tag1')
    expect(api.getPendingTag).toHaveBeenCalledTimes(1)
    wrapper.unmount()
  })

  it('does not redirect when no pending tags are found', async () => {
    mockGameStore.recentGames = [{ id: 'g1', status: GameStatus.Active }]
    vi.mocked(api.getPendingTag).mockResolvedValue(null)
    const { wrapper, composable } = withSetup()

    await composable.checkPendingTags()

    expect(mockRouter.push).not.toHaveBeenCalled()
    wrapper.unmount()
  })

  it('continues checking games when one pending-tag request fails', async () => {
    const tag = makeTag()
    mockGameStore.recentGames = [
      { id: 'g1', status: GameStatus.Active },
      { id: 'g2', status: GameStatus.Active },
    ]
    vi.mocked(api.getPendingTag)
      .mockRejectedValueOnce(new Error('Unavailable'))
      .mockResolvedValueOnce(tag)
    const { wrapper, composable } = withSetup()

    await composable.checkPendingTags()

    expect(api.getPendingTag).toHaveBeenNthCalledWith(1, 'g1')
    expect(api.getPendingTag).toHaveBeenNthCalledWith(2, 'g2')
    expect(mockRouter.push).toHaveBeenCalledWith('/games/g2/tag/tag1')
    wrapper.unmount()
  })

  it('only checks active games', async () => {
    mockGameStore.recentGames = [
      { id: 'ended', status: GameStatus.Ended },
      { id: 'active', status: GameStatus.Active },
    ]
    vi.mocked(api.getPendingTag).mockResolvedValue(null)
    const { wrapper, composable } = withSetup()

    await composable.checkPendingTags()

    expect(api.getPendingTag).toHaveBeenCalledTimes(1)
    expect(api.getPendingTag).toHaveBeenCalledWith('active')
    wrapper.unmount()
  })

  it('redirects to a queued tag when the queue changes outside tag confirmation', async () => {
    const queuedTag = { gameId: 'g2', tag: makeTag({ id: 'tag2' }) }
    const { wrapper } = withSetup()

    mockTagStore.pendingTagQueue.push(queuedTag)
    await nextTick()
    await flushPromises()

    expect(mockTagStore.dequeuePendingTag).toHaveBeenCalled()
    expect(mockRouter.push).toHaveBeenCalledWith('/games/g2/tag/tag2')
    wrapper.unmount()
  })

  it('does not consume a queued tag while on tag confirmation', async () => {
    mockRouter.currentRoute.value.name = 'tag-confirm'
    const { wrapper } = withSetup()

    mockTagStore.pendingTagQueue.push({ gameId: 'g2', tag: makeTag({ id: 'tag2' }) })
    await nextTick()
    await flushPromises()

    expect(mockTagStore.dequeuePendingTag).not.toHaveBeenCalled()
    expect(mockRouter.push).not.toHaveBeenCalled()
    wrapper.unmount()
  })
})
