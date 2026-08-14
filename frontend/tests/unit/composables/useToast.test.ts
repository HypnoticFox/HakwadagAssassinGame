import { beforeEach, describe, expect, it, vi } from 'vitest'

import { clearToasts, useToast } from '@/composables/useToast'

beforeEach(() => {
  clearToasts()
})

describe('useToast', () => {
  it('starts with an empty toast list', () => {
    const { toasts } = useToast()
    expect(toasts.value).toHaveLength(0)
  })

  it('adds a toast with default type and duration', () => {
    const { toasts, toast } = useToast()

    toast('Hello')

    expect(toasts.value).toHaveLength(1)
    expect(toasts.value[0].message).toBe('Hello')
    expect(toasts.value[0].type).toBe('info')
    expect(toasts.value[0].duration).toBe(4000)
  })

  it('supports custom type and duration', () => {
    const { toasts, toast } = useToast()

    toast('Saved', 'success', 2000)

    expect(toasts.value[0].type).toBe('success')
    expect(toasts.value[0].duration).toBe(2000)
  })

  it('auto-removes a toast after its duration', () => {
    vi.useFakeTimers()
    const { toasts, toast } = useToast()

    toast('Short lived', 'info', 1000)
    expect(toasts.value).toHaveLength(1)

    vi.advanceTimersByTime(1000)
    expect(toasts.value).toHaveLength(0)

    vi.useRealTimers()
  })

  it('does not auto-remove when duration is zero', () => {
    vi.useFakeTimers()
    const { toasts, toast } = useToast()

    toast('Persistent', 'info', 0)
    vi.advanceTimersByTime(10_000)
    expect(toasts.value).toHaveLength(1)

    vi.useRealTimers()
  })

  it('stacks multiple toasts', () => {
    const { toasts, toast } = useToast()

    toast('One', 'info')
    toast('Two', 'success')
    toast('Three', 'error')

    expect(toasts.value).toHaveLength(3)
    expect(toasts.value.map((t) => t.message)).toEqual(['One', 'Two', 'Three'])
  })

  it('removes a specific toast immediately', () => {
    const { toasts, toast, removeToast } = useToast()

    toast('Keep')
    toast('Remove')
    const idToRemove = toasts.value[1].id

    removeToast(idToRemove)

    expect(toasts.value).toHaveLength(1)
    expect(toasts.value[0].message).toBe('Keep')
  })

  it('ignores removing an unknown toast id', () => {
    const { toasts, toast, removeToast } = useToast()

    toast('Only one')
    removeToast('unknown-id')

    expect(toasts.value).toHaveLength(1)
  })

  it('shares state across composable calls', () => {
    const { toast } = useToast()
    const { toasts: toastsFromAnotherCall } = useToast()

    toast('Shared')

    expect(toastsFromAnotherCall.value).toHaveLength(1)
  })
})
