import { describe, it, expect, beforeEach, vi } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { useTagStore } from '@/stores/tag'
import { api } from '@/api/client'
import { TagStatus, type TagSubmissionDto } from '@/types'

vi.mock('@/api/client', () => ({
  api: {
    getPendingTag: vi.fn(),
    getPendingOutgoingTag: vi.fn(),
    submitTag: vi.fn(),
    confirmTag: vi.fn(),
    denyTag: vi.fn(),
    voidTag: vi.fn(),
  },
}))

function makeTag(overrides: Partial<TagSubmissionDto> = {}): TagSubmissionDto {
  return {
    id: 'tag1',
    assignmentId: 'a1',
    hunterId: 'h1',
    targetId: 't1',
    conditionId: 'c1',
    status: TagStatus.Pending,
    submittedAt: '2024-01-01T00:00:00Z',
    ...overrides,
  }
}

beforeEach(() => {
  setActivePinia(createPinia())
  vi.clearAllMocks()
})

describe('tag store', () => {
  describe('initial state', () => {
    it('has default values', () => {
      const store = useTagStore()
      expect(store.pendingTag).toBeNull()
      expect(store.pendingOutgoingTag).toBeNull()
      expect(store.isLoading).toBe(false)
      expect(store.error).toBeNull()
    })
  })

  describe('loadPendingTag', () => {
    it('sets pendingTag on success', async () => {
      const tag = makeTag()
      vi.mocked(api.getPendingTag).mockResolvedValue(tag)
      const store = useTagStore()

      const result = await store.loadPendingTag('g1')

      expect(result).toBe(tag)
      expect(store.pendingTag).toStrictEqual(tag)
    })

    it('sets pendingTag to null when api returns null', async () => {
      vi.mocked(api.getPendingTag).mockResolvedValue(null)
      const store = useTagStore()

      const result = await store.loadPendingTag('g1')

      expect(result).toBeNull()
      expect(store.pendingTag).toBeNull()
    })

    it('sets error on failure', async () => {
      vi.mocked(api.getPendingTag).mockRejectedValue(new Error('Failed'))
      const store = useTagStore()

      await expect(store.loadPendingTag('g1')).rejects.toThrow('Failed')
      expect(store.error).toBe('Failed')
    })
  })

  describe('loadPendingOutgoingTag', () => {
    it('sets pendingOutgoingTag on success', async () => {
      const tag = makeTag()
      vi.mocked(api.getPendingOutgoingTag).mockResolvedValue(tag)
      const store = useTagStore()

      const result = await store.loadPendingOutgoingTag('g1')

      expect(api.getPendingOutgoingTag).toHaveBeenCalledWith('g1')
      expect(result).toBe(tag)
      expect(store.pendingOutgoingTag).toStrictEqual(tag)
    })

    it('sets pendingOutgoingTag to null when api returns null', async () => {
      vi.mocked(api.getPendingOutgoingTag).mockResolvedValue(null)
      const store = useTagStore()

      const result = await store.loadPendingOutgoingTag('g1')

      expect(result).toBeNull()
      expect(store.pendingOutgoingTag).toBeNull()
    })

    it('sets error on failure', async () => {
      vi.mocked(api.getPendingOutgoingTag).mockRejectedValue(new Error('Failed'))
      const store = useTagStore()

      await expect(store.loadPendingOutgoingTag('g1')).rejects.toThrow('Failed')
      expect(store.error).toBe('Failed')
    })
  })

  describe('submitTag', () => {
    it('calls api.submitTag and sets pendingTag and pendingOutgoingTag', async () => {
      const tag = makeTag()
      vi.mocked(api.submitTag).mockResolvedValue(tag)
      const store = useTagStore()

      const result = await store.submitTag('g1', 'a1', 'c1')

      expect(api.submitTag).toHaveBeenCalledWith('g1', {
        assignmentId: 'a1',
        conditionId: 'c1',
      })
      expect(result).toBe(tag)
      expect(store.pendingTag).toStrictEqual(tag)
      expect(store.pendingOutgoingTag).toStrictEqual(tag)
    })
  })

  describe('confirmTag', () => {
    it('calls api.confirmTag and updates pendingTag', async () => {
      const tag = makeTag({ status: TagStatus.Confirmed })
      vi.mocked(api.confirmTag).mockResolvedValue(tag)
      const store = useTagStore()

      const result = await store.confirmTag('g1', 'tag1')

      expect(api.confirmTag).toHaveBeenCalledWith('g1', 'tag1')
      expect(result).toBe(tag)
      expect(store.pendingTag).toStrictEqual(tag)
    })
  })

  describe('denyTag', () => {
    it('calls api.denyTag and updates pendingTag', async () => {
      const tag = makeTag({ status: TagStatus.Denied })
      vi.mocked(api.denyTag).mockResolvedValue(tag)
      const store = useTagStore()

      const result = await store.denyTag('g1', 'tag1')

      expect(api.denyTag).toHaveBeenCalledWith('g1', 'tag1')
      expect(result).toBe(tag)
      expect(store.pendingTag).toStrictEqual(tag)
    })
  })

  describe('voidTag', () => {
    it('calls api.voidTag and updates pendingTag', async () => {
      const tag = makeTag({ status: TagStatus.Voided })
      vi.mocked(api.voidTag).mockResolvedValue(tag)
      const store = useTagStore()

      const result = await store.voidTag('g1', 'tag1')

      expect(api.voidTag).toHaveBeenCalledWith('g1', 'tag1')
      expect(result).toBe(tag)
      expect(store.pendingTag).toStrictEqual(tag)
    })
  })

  describe('setPendingTag', () => {
    it('sets pendingTag directly', () => {
      const store = useTagStore()
      const tag = makeTag()
      store.setPendingTag(tag)
      expect(store.pendingTag).toStrictEqual(tag)
    })

    it('sets pendingTag to null', () => {
      const store = useTagStore()
      store.setPendingTag(null)
      expect(store.pendingTag).toBeNull()
    })
  })

  describe('pending tag queue', () => {
    it('queues a pending tag with its game id', () => {
      const store = useTagStore()
      const tag = makeTag()

      store.queuePendingTag('g1', tag)

      expect(store.pendingTagQueue).toStrictEqual([{ gameId: 'g1', tag }])
    })

    it('dequeues pending tags in FIFO order', () => {
      const store = useTagStore()
      const first = makeTag({ id: 'tag1' })
      const second = makeTag({ id: 'tag2' })
      store.queuePendingTag('g1', first)
      store.queuePendingTag('g2', second)

      expect(store.dequeuePendingTag()).toStrictEqual({ gameId: 'g1', tag: first })
      expect(store.dequeuePendingTag()).toStrictEqual({ gameId: 'g2', tag: second })
      expect(store.pendingTagQueue).toHaveLength(0)
    })

    it('returns null when the queue is empty', () => {
      const store = useTagStore()

      expect(store.dequeuePendingTag()).toBeNull()
    })

    it('clears all queued pending tags', () => {
      const store = useTagStore()
      store.queuePendingTag('g1', makeTag())

      store.clearPendingTagQueue()

      expect(store.pendingTagQueue).toHaveLength(0)
    })

    it('returns null after clearing the queue', () => {
      const store = useTagStore()
      store.queuePendingTag('g1', makeTag())
      store.clearPendingTagQueue()

      expect(store.dequeuePendingTag()).toBeNull()
    })
  })

  describe('clearPendingOutgoingTag', () => {
    it('clears the pending outgoing tag', () => {
      const store = useTagStore()
      store.pendingOutgoingTag = makeTag()
      store.clearPendingOutgoingTag()
      expect(store.pendingOutgoingTag).toBeNull()
    })

    it('is a no-op when no pending outgoing tag is set', () => {
      const store = useTagStore()
      store.clearPendingOutgoingTag()
      expect(store.pendingOutgoingTag).toBeNull()
    })
  })

  describe('isTagPending', () => {
    it('returns true for pending tag', () => {
      const store = useTagStore()
      const tag = makeTag({ status: TagStatus.Pending })
      expect(store.isTagPending(tag)).toBe(true)
    })

    it('returns false for confirmed tag', () => {
      const store = useTagStore()
      const tag = makeTag({ status: TagStatus.Confirmed })
      expect(store.isTagPending(tag)).toBe(false)
    })

    it('returns false for null', () => {
      const store = useTagStore()
      expect(store.isTagPending(null)).toBe(false)
    })
  })
})
