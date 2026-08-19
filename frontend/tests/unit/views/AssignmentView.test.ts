import { describe, it, expect, beforeEach, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { ref } from 'vue'

import { clearToasts } from '@/composables/useToast'
import { useSafeTime } from '@/composables/useSafeTime'
import AssignmentView from '@/views/AssignmentView.vue'
import { useAssignmentStore, useTagStore } from '@/stores'
import {
  GameStatus,
  TagStatus,
  type AssignmentDto,
  type GameDto,
  type TagSubmissionDto,
} from '@/types'
import { api } from '@/api/client'
import { withI18n } from '../helpers/i18n'

vi.mock('@/api/client', () => ({
  api: {
    getGame: vi.fn(),
    getMyAssignment: vi.fn(),
    getNextAssignmentAvailability: vi.fn(),
    getPendingOutgoingTag: vi.fn(),
    submitTag: vi.fn(),
  },
}))

vi.mock('@/composables/useSignalR', () => ({
  useGameSignalR: vi.fn(),
}))

vi.mock('@/composables/useSafeTime', () => ({
  useSafeTime: vi.fn(),
}))

vi.mock('vue-router', () => ({
  useRoute: () => ({ params: { id: 'g1' } }),
  useRouter: () => ({ push: vi.fn() }),
}))

function makeGame(overrides: Partial<GameDto> = {}): GameDto {
  return {
    id: 'g1',
    name: 'Hunt',
    inviteCode: 'ABC',
    status: GameStatus.Active,
    createdAt: '2024-01-01T00:00:00Z',
    maxPlayers: 10,
    basePointsPerTag: 10,
    confirmationTimeout: '00:10:00',
    assignmentCooldownMinutes: 30,
    playerCount: 2,
    participantCount: 2,
    isParticipating: true,
    myRole: 0,
    safeTimeBlocks: [],
    ...overrides,
  }
}

function makeAssignment(): AssignmentDto {
  return {
    id: 'a1',
    target: { id: 't1', displayName: 'Target' },
    conditions: [{ id: 'c1', type: 0, description: 'With someone' }],
    assignedAt: '2024-01-01T00:00:00Z',
  }
}

function makePendingTag(): TagSubmissionDto {
  return {
    id: 'tag1',
    assignmentId: 'a1',
    hunterId: 'p1',
    targetId: 't1',
    conditionId: 'c1',
    status: TagStatus.Pending,
    submittedAt: new Date(Date.now() - 60_000).toISOString(),
  }
}

function futureAvailability() {
  return { availableAt: new Date(Date.now() + 30 * 60 * 1000).toISOString() }
}

beforeEach(() => {
  setActivePinia(createPinia())
  vi.clearAllMocks()
  clearToasts()

  vi.mocked(api.getGame).mockResolvedValue(makeGame())
  vi.mocked(api.getPendingOutgoingTag).mockResolvedValue(null)
  vi.mocked(api.getNextAssignmentAvailability).mockResolvedValue(futureAvailability())
  vi.mocked(useSafeTime).mockReturnValue({
    isInSafeTime: ref(false),
    currentBlock: ref(null),
  })
})

async function mountView() {
  const i18nOptions = withI18n()
  const wrapper = mount(AssignmentView, {
    ...i18nOptions,
    global: {
      ...i18nOptions.global,
      stubs: {
        Button: true,
        AssignmentCooldownTimer: true,
        ConditionCard: true,
        Modal: true,
        PendingTagCountdown: true,
      },
    },
  })
  await flushPromises()
  await flushPromises()
  return wrapper
}

describe('AssignmentView.vue', () => {
  it('loads the cooldown when the assignment disappears after a SignalR update', async () => {
    // Fresh object per request, like a real API response
    vi.mocked(api.getMyAssignment).mockImplementation(() => Promise.resolve(makeAssignment()))

    const wrapper = await mountView()
    const assignmentStore = useAssignmentStore()
    expect(assignmentStore.currentAssignment).not.toBeNull()

    // SignalR TagResolved arrives: the backend reports no active assignment
    vi.mocked(api.getMyAssignment).mockResolvedValue(null)
    await assignmentStore.loadAssignment('g1')

    await flushPromises()
    await flushPromises()

    expect(api.getNextAssignmentAvailability).toHaveBeenCalledWith('g1')
    expect(assignmentStore.currentAssignment).toBeNull()
    expect(assignmentStore.nextAvailability).not.toBeNull()

    wrapper.unmount()
  })

  it('does not loop when the same assignment is re-fetched', async () => {
    vi.mocked(api.getMyAssignment).mockImplementation(() => Promise.resolve(makeAssignment()))

    const wrapper = await mountView()
    await flushPromises()
    await flushPromises()

    // The initial mount refresh triggers one extra refresh when the assignment
    // first appears; re-fetching the identical assignment must not loop.
    expect(api.getMyAssignment).toHaveBeenCalledTimes(2)

    wrapper.unmount()
  })

  it('refreshes the assignment when the pending outgoing tag is cleared', async () => {
    vi.mocked(api.getMyAssignment).mockImplementation(() => Promise.resolve(makeAssignment()))
    vi.mocked(api.getPendingOutgoingTag).mockResolvedValue(makePendingTag())

    const wrapper = await mountView()
    const tagStore = useTagStore()
    expect(tagStore.pendingOutgoingTag).not.toBeNull()

    const callsBefore = vi.mocked(api.getMyAssignment).mock.calls.length

    // SignalR TagResolved clears the hunter's pending outgoing tag
    tagStore.clearPendingOutgoingTag()
    await flushPromises()
    await flushPromises()

    expect(vi.mocked(api.getMyAssignment).mock.calls.length).toBeGreaterThan(callsBefore)

    wrapper.unmount()
  })

  it('shows the safe time message when safe time is active', async () => {
    vi.mocked(useSafeTime).mockReturnValue({
      isInSafeTime: ref(true),
      currentBlock: ref(null),
    })
    vi.mocked(api.getGame).mockResolvedValue(
      makeGame({
        safeTimeBlocks: [{ id: 'safe-1', startTime: '2025-06-15T22:00:00+00:00', endTime: '2025-06-15T06:00:00+00:00' }],
      }),
    )
    vi.mocked(api.getMyAssignment).mockResolvedValue(makeAssignment())

    const wrapper = await mountView()
    const message = wrapper.find('.safe-time-message')

    expect(message.exists()).toBe(true)
    expect(message.text()).toContain('Safe time is active')
    expect(message.text()).toContain('Tags cannot be submitted during safe time')
    expect(wrapper.find('.target-card').exists()).toBe(false)

    wrapper.unmount()
  })

  it('shows the assignment when safe time is not active', async () => {
    vi.mocked(useSafeTime).mockReturnValue({
      isInSafeTime: ref(false),
      currentBlock: ref(null),
    })
    vi.mocked(api.getMyAssignment).mockResolvedValue(makeAssignment())

    const wrapper = await mountView()

    expect(wrapper.find('.target-card').exists()).toBe(true)
    expect(wrapper.find('.safe-time-message').exists()).toBe(false)

    wrapper.unmount()
  })

  it('does not show the safe time message when the game is not active', async () => {
    vi.mocked(useSafeTime).mockReturnValue({
      isInSafeTime: ref(true),
      currentBlock: ref(null),
    })
    vi.mocked(api.getGame).mockResolvedValue(makeGame({ status: GameStatus.NotStarted }))

    const wrapper = await mountView()

    expect(wrapper.find('.safe-time-message').exists()).toBe(false)

    wrapper.unmount()
  })
})
