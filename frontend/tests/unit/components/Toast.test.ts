import { beforeEach, describe, expect, it } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'

import Toast from '@/components/Toast.vue'
import { clearToasts, useToast } from '@/composables/useToast'

beforeEach(() => {
  clearToasts()
  document.body.innerHTML = ''
})

describe('Toast.vue', () => {
  it('renders nothing when there are no toasts', () => {
    mount(Toast)
    expect(document.body.querySelectorAll('.toast')).toHaveLength(0)
  })

  it('renders toasts from the global queue with correct icons', async () => {
    const { toast } = useToast()
    toast('Game saved', 'success')
    toast('Something went wrong', 'error')

    mount(Toast)
    await flushPromises()

    const toasts = document.body.querySelectorAll('.toast')
    expect(toasts).toHaveLength(2)
    expect(toasts[0].textContent).toContain('Game saved')
    expect(toasts[0].querySelector('.toast__icon--success')).not.toBeNull()
    expect(toasts[1].textContent).toContain('Something went wrong')
    expect(toasts[1].querySelector('.toast__icon--error')).not.toBeNull()
  })

  it('uses role alert for errors and status for non-errors', async () => {
    const { toast } = useToast()
    toast('Warning', 'warning')
    toast('Error', 'error')
    toast('Info', 'info')

    mount(Toast)
    await flushPromises()

    const toasts = document.body.querySelectorAll('.toast')
    expect(toasts[0].getAttribute('role')).toBe('status')
    expect(toasts[1].getAttribute('role')).toBe('alert')
    expect(toasts[2].getAttribute('role')).toBe('status')
  })

  it('removes a toast when the close button is clicked', async () => {
    const { toast } = useToast()
    toast('Dismiss me')

    mount(Toast)
    await flushPromises()

    const closeButton = document.body.querySelector('.toast__close') as HTMLButtonElement | null
    expect(closeButton).not.toBeNull()

    closeButton!.click()
    await flushPromises()

    expect(document.body.querySelectorAll('.toast')).toHaveLength(0)
  })

  it('exposes a labelled, polite notifications region', async () => {
    mount(Toast)
    await flushPromises()

    const container = document.body.querySelector('.toast-container')
    expect(container).not.toBeNull()
    expect(container!.getAttribute('role')).toBe('region')
    expect(container!.getAttribute('aria-label')).toBe('Notifications')
    expect(container!.getAttribute('aria-live')).toBe('polite')
    expect(container!.getAttribute('aria-atomic')).toBe('true')
  })
})
