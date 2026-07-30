import { describe, it, expect, beforeEach, vi } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { useAssignmentStore } from '@/stores/assignment'
import { api } from '@/api/client'
import type { AssignmentDto } from '@/types'

vi.mock('@/api/client', () => ({
  api: {
    getMyAssignment: vi.fn(),
  },
}))

function makeAssignment(overrides: Partial<AssignmentDto> = {}): AssignmentDto {
  return {
    id: 'a1',
    target: { id: 't1', displayName: 'Target' },
    conditions: [
      { id: 'c1', type: 0, description: 'With someone' },
    ],
    assignedAt: '2024-01-01T00:00:00Z',
    ...overrides,
  }
}

beforeEach(() => {
  setActivePinia(createPinia())
  vi.clearAllMocks()
})

describe('assignment store', () => {
  describe('initial state', () => {
    it('has default values', () => {
      const store = useAssignmentStore()
      expect(store.currentAssignment).toBeNull()
      expect(store.isLoading).toBe(false)
      expect(store.error).toBeNull()
    })
  })

  describe('loadAssignment', () => {
    it('sets assignment on success', async () => {
      const assignment = makeAssignment()
      vi.mocked(api.getMyAssignment).mockResolvedValue(assignment)
      const store = useAssignmentStore()

      const result = await store.loadAssignment('g1')

      expect(result).toStrictEqual(assignment)
      expect(store.currentAssignment).toStrictEqual(assignment)
      expect(store.isLoading).toBe(false)
    })

    it('sets currentAssignment to null when api returns null', async () => {
      vi.mocked(api.getMyAssignment).mockResolvedValue(null)
      const store = useAssignmentStore()

      const result = await store.loadAssignment('g1')

      expect(result).toBeNull()
      expect(store.currentAssignment).toBeNull()
    })

    it('sets error and rethrows on failure', async () => {
      vi.mocked(api.getMyAssignment).mockRejectedValue(new Error('Not found'))
      const store = useAssignmentStore()

      await expect(store.loadAssignment('g1')).rejects.toThrow('Not found')
      expect(store.error).toBe('Not found')
      expect(store.isLoading).toBe(false)
    })

    it('manages loading state', async () => {
      vi.mocked(api.getMyAssignment).mockResolvedValue(makeAssignment())
      const store = useAssignmentStore()

      const promise = store.loadAssignment('g1')
      expect(store.isLoading).toBe(true)
      await promise
      expect(store.isLoading).toBe(false)
    })
  })

  describe('setAssignment', () => {
    it('sets the assignment', () => {
      const store = useAssignmentStore()
      const assignment = makeAssignment()
      store.setAssignment(assignment)
      expect(store.currentAssignment).toStrictEqual(assignment)
    })

    it('sets assignment to null', () => {
      const store = useAssignmentStore()
      store.currentAssignment = makeAssignment()
      store.setAssignment(null)
      expect(store.currentAssignment).toBeNull()
    })
  })

  describe('clearAssignment', () => {
    it('clears the assignment', () => {
      const store = useAssignmentStore()
      store.currentAssignment = makeAssignment()
      store.clearAssignment()
      expect(store.currentAssignment).toBeNull()
    })
  })
})
