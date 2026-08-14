import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import AssignmentCooldownTimer from '@/components/AssignmentCooldownTimer.vue'
import { withI18n } from '../helpers/i18n'

describe('AssignmentCooldownTimer.vue', () => {
  beforeEach(() => {
    vi.useFakeTimers()
    vi.setSystemTime(new Date('2024-01-01T00:00:00Z'))
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  describe('display', () => {
    it('shows the countdown in minutes:seconds format', () => {
      const wrapper = mount(AssignmentCooldownTimer, {
        props: { availableAt: '2024-01-01T00:24:35Z' },
        ...withI18n(),
      })
      expect(wrapper.find('[role="timer"]').text()).toBe('24:35')
    })

    it('pads minutes and seconds with leading zeros', () => {
      const wrapper = mount(AssignmentCooldownTimer, {
        props: { availableAt: '2024-01-01T00:00:05Z' },
        ...withI18n(),
      })
      expect(wrapper.find('[role="timer"]').text()).toBe('00:05')
    })

    it('renders cooldown title and message translations', () => {
      const wrapper = mount(AssignmentCooldownTimer, {
        props: { availableAt: '2024-01-01T00:24:35Z' },
        ...withI18n(),
      })
      expect(wrapper.text()).toContain('Waiting for next assignment')
      expect(wrapper.text()).toContain("You'll get a new assignment in")
    })
  })

  describe('countdown', () => {
    it('counts down each second', async () => {
      const wrapper = mount(AssignmentCooldownTimer, {
        props: { availableAt: '2024-01-01T00:00:30Z' },
        ...withI18n(),
      })
      expect(wrapper.find('[role="timer"]').text()).toBe('00:30')

      await vi.advanceTimersByTimeAsync(1000)
      expect(wrapper.find('[role="timer"]').text()).toBe('00:29')

      await vi.advanceTimersByTimeAsync(1000)
      expect(wrapper.find('[role="timer"]').text()).toBe('00:28')
    })

    it('restarts the countdown when availableAt changes', async () => {
      const wrapper = mount(AssignmentCooldownTimer, {
        props: { availableAt: '2024-01-01T00:00:30Z' },
        ...withI18n(),
      })

      await wrapper.setProps({ availableAt: '2024-01-01T00:10:00Z' })

      expect(wrapper.find('[role="timer"]').text()).toBe('10:00')
    })
  })

  describe('countdown completion', () => {
    it('stays at 00:00 when the countdown reaches zero', async () => {
      const wrapper = mount(AssignmentCooldownTimer, {
        props: { availableAt: '2024-01-01T00:00:02Z' },
        ...withI18n(),
      })
      expect(wrapper.find('[role="timer"]').text()).toBe('00:02')

      await vi.advanceTimersByTimeAsync(2000)

      expect(wrapper.find('[role="timer"]').text()).toBe('00:00')
    })

    it('shows 00:00 when availableAt is in the past', () => {
      const wrapper = mount(AssignmentCooldownTimer, {
        props: { availableAt: '2023-12-31T23:59:00Z' },
        ...withI18n(),
      })
      expect(wrapper.find('[role="timer"]').text()).toBe('00:00')
    })
  })

  describe('lifecycle', () => {
    it('clears the interval on unmount', () => {
      const wrapper = mount(AssignmentCooldownTimer, {
        props: { availableAt: '2024-01-01T00:00:30Z' },
        ...withI18n(),
      })
      expect(vi.getTimerCount()).toBe(1)

      wrapper.unmount()

      expect(vi.getTimerCount()).toBe(0)
    })
  })
})
