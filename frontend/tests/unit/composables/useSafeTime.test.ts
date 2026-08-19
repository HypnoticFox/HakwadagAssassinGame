import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { defineComponent, h, ref, type Ref } from 'vue'
import { mount } from '@vue/test-utils'

import { formatTimeOfDay, localTimeToDateTimeOffset, useSafeTime } from '@/composables/useSafeTime'
import type { SafeTimeBlockDto } from '@/types'

function makeBlock(
  id: string,
  startIso: string,
  endIso: string,
): SafeTimeBlockDto {
  return { id, startTime: startIso, endTime: endIso }
}

function setUtcTime(time: string) {
  vi.setSystemTime(new Date(`2025-06-15T${time}Z`))
}

const mountedWrappers: Array<{ unmount: () => void }> = []

function setupUseSafeTime(blocks: Ref<SafeTimeBlockDto[]>) {
  let result!: ReturnType<typeof useSafeTime>
  const TestComponent = defineComponent({
    setup() {
      result = useSafeTime(blocks)
      return () => h('div')
    },
  })
  const wrapper = mount(TestComponent)
  mountedWrappers.push(wrapper)
  return { wrapper, result: () => result }
}

beforeEach(() => {
  vi.useFakeTimers()
})

afterEach(() => {
  for (const wrapper of mountedWrappers) {
    wrapper.unmount()
  }
  mountedWrappers.length = 0
  vi.useRealTimers()
})

describe('formatTimeOfDay', () => {
  it('formats a DateTimeOffset as HH:MM in local time', () => {
    const iso = '2025-06-15T06:00:00+00:00'
    const result = formatTimeOfDay(iso)
    expect(result).toMatch(/^\d{2}:\d{2}$/)
    const expected = new Date(iso)
    const expectedStr = `${String(expected.getHours()).padStart(2, '0')}:${String(expected.getMinutes()).padStart(2, '0')}`
    expect(result).toBe(expectedStr)
  })

  it('returns the raw value for invalid input', () => {
    expect(formatTimeOfDay('garbage')).toBe('garbage')
  })
})

describe('localTimeToDateTimeOffset', () => {
  it('converts HH:MM to an ISO 8601 DateTimeOffset string', () => {
    const result = localTimeToDateTimeOffset('22:00')
    expect(result).toMatch(/^\d{4}-\d{2}-\d{2}T22:00:00[+-]\d{2}:\d{2}$/)
    const date = new Date(result)
    expect(date.getHours()).toBe(22)
    expect(date.getMinutes()).toBe(0)
  })

  it('returns the raw value for invalid input', () => {
    expect(localTimeToDateTimeOffset('garbage')).toBe('garbage')
  })
})

describe('useSafeTime', () => {
  it('reports no active block for an empty block list', () => {
    setUtcTime('12:00:00')
    const blocks = ref<SafeTimeBlockDto[]>([])
    const { result } = setupUseSafeTime(blocks)

    expect(result().isInSafeTime.value).toBe(false)
    expect(result().currentBlock.value).toBeNull()
  })

  describe('same-day blocks (UTC offset)', () => {
    const block = makeBlock('day', '2025-06-15T09:00:00+00:00', '2025-06-15T17:00:00+00:00')

    it('is active when the time is inside the block', () => {
      setUtcTime('12:00:00')
      const { result } = setupUseSafeTime(ref([block]))

      expect(result().isInSafeTime.value).toBe(true)
      expect(result().currentBlock.value?.id).toBe('day')
    })

    it('is inactive before the block starts', () => {
      setUtcTime('08:00:00')
      const { result } = setupUseSafeTime(ref([block]))

      expect(result().isInSafeTime.value).toBe(false)
      expect(result().currentBlock.value).toBeNull()
    })

    it('includes the start boundary', () => {
      setUtcTime('09:00:00')
      const { result } = setupUseSafeTime(ref([block]))

      expect(result().isInSafeTime.value).toBe(true)
    })

    it('excludes the end boundary', () => {
      setUtcTime('17:00:00')
      const { result } = setupUseSafeTime(ref([block]))

      expect(result().isInSafeTime.value).toBe(false)
    })

    it('is inactive after the block ends', () => {
      setUtcTime('18:00:00')
      const { result } = setupUseSafeTime(ref([block]))

      expect(result().isInSafeTime.value).toBe(false)
      expect(result().currentBlock.value).toBeNull()
    })
  })

  describe('cross-midnight blocks (UTC offset)', () => {
    const block = makeBlock('night', '2025-06-15T22:00:00+00:00', '2025-06-15T06:00:00+00:00')

    it('is active after the start time', () => {
      setUtcTime('23:00:00')
      const { result } = setupUseSafeTime(ref([block]))

      expect(result().isInSafeTime.value).toBe(true)
      expect(result().currentBlock.value?.id).toBe('night')
    })

    it('is active before the end time on the next day', () => {
      setUtcTime('03:00:00')
      const { result } = setupUseSafeTime(ref([block]))

      expect(result().isInSafeTime.value).toBe(true)
    })

    it('includes the start boundary', () => {
      setUtcTime('22:00:00')
      const { result } = setupUseSafeTime(ref([block]))

      expect(result().isInSafeTime.value).toBe(true)
    })

    it('excludes the end boundary', () => {
      setUtcTime('06:00:00')
      const { result } = setupUseSafeTime(ref([block]))

      expect(result().isInSafeTime.value).toBe(false)
    })

    it('is inactive during the daytime gap', () => {
      setUtcTime('14:00:00')
      const { result } = setupUseSafeTime(ref([block]))

      expect(result().isInSafeTime.value).toBe(false)
      expect(result().currentBlock.value).toBeNull()
    })
  })

  describe('non-zero offset blocks', () => {
    const block = makeBlock('cest', '2025-06-15T22:00:00+02:00', '2025-06-15T06:00:00+02:00')

    it('is active when UTC time converts to within the block', () => {
      setUtcTime('21:00:00')
      const { result } = setupUseSafeTime(ref([block]))
      expect(result().isInSafeTime.value).toBe(true)
    })

    it('is inactive when UTC time is outside the block', () => {
      setUtcTime('10:00:00')
      const { result } = setupUseSafeTime(ref([block]))
      expect(result().isInSafeTime.value).toBe(false)
    })

    it('is active at the UTC-converted start boundary', () => {
      setUtcTime('20:00:00')
      const { result } = setupUseSafeTime(ref([block]))
      expect(result().isInSafeTime.value).toBe(true)
    })

    it('excludes the UTC-converted end boundary', () => {
      setUtcTime('04:00:00')
      const { result } = setupUseSafeTime(ref([block]))
      expect(result().isInSafeTime.value).toBe(false)
    })
  })

  describe('multiple blocks', () => {
    it('selects the first block when only it is active', () => {
      setUtcTime('10:00:00')
      const first = makeBlock('first', '2025-06-15T09:00:00+00:00', '2025-06-15T11:00:00+00:00')
      const second = makeBlock('second', '2025-06-15T13:00:00+00:00', '2025-06-15T17:00:00+00:00')
      const { result } = setupUseSafeTime(ref([first, second]))

      expect(result().currentBlock.value?.id).toBe('first')
    })

    it('selects the block ending latest when both are active', () => {
      setUtcTime('12:00:00')
      const first = makeBlock('first', '2025-06-15T09:00:00+00:00', '2025-06-15T14:00:00+00:00')
      const second = makeBlock('second', '2025-06-15T10:00:00+00:00', '2025-06-15T17:00:00+00:00')
      const { result } = setupUseSafeTime(ref([first, second]))

      expect(result().isInSafeTime.value).toBe(true)
      expect(result().currentBlock.value?.id).toBe('second')
    })

    it('reports no active block when neither block contains the time', () => {
      setUtcTime('12:00:00')
      const first = makeBlock('first', '2025-06-15T06:00:00+00:00', '2025-06-15T08:00:00+00:00')
      const second = makeBlock('second', '2025-06-15T18:00:00+00:00', '2025-06-15T20:00:00+00:00')
      const { result } = setupUseSafeTime(ref([first, second]))

      expect(result().isInSafeTime.value).toBe(false)
      expect(result().currentBlock.value).toBeNull()
    })
  })

  describe('boundary timers', () => {
    it('enters safe time when the start boundary timer fires', () => {
      setUtcTime('08:59:55')
      const block = makeBlock('day', '2025-06-15T09:00:00+00:00', '2025-06-15T17:00:00+00:00')
      const { result } = setupUseSafeTime(ref([block]))

      expect(result().isInSafeTime.value).toBe(false)

      vi.advanceTimersByTime(6000)

      expect(result().isInSafeTime.value).toBe(true)
      expect(result().currentBlock.value?.id).toBe('day')
    })

    it('leaves safe time when the end boundary timer fires', () => {
      setUtcTime('16:59:55')
      const block = makeBlock('day', '2025-06-15T09:00:00+00:00', '2025-06-15T17:00:00+00:00')
      const { result } = setupUseSafeTime(ref([block]))

      expect(result().isInSafeTime.value).toBe(true)

      vi.advanceTimersByTime(6000)

      expect(result().isInSafeTime.value).toBe(false)
      expect(result().currentBlock.value).toBeNull()
    })

    it('clears the boundary timer when unmounted', () => {
      setUtcTime('08:59:55')
      const block = makeBlock('day', '2025-06-15T09:00:00+00:00', '2025-06-15T17:00:00+00:00')
      const { wrapper, result } = setupUseSafeTime(ref([block]))

      wrapper.unmount()

      expect(() => vi.advanceTimersByTime(6000)).not.toThrow()
      expect(result().isInSafeTime.value).toBe(false)
    })
  })
})
