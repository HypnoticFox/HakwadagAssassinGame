import { ref } from 'vue'

export type ToastType = 'success' | 'error' | 'warning' | 'info'

export interface Toast {
  id: string
  message: string
  type: ToastType
  duration: number
}

const DEFAULT_DURATION = 4000

const toasts = ref<Toast[]>([])

function generateId(): string {
  return `${Date.now()}-${Math.random().toString(36).slice(2, 9)}`
}

export function useToast() {
  function toast(message: string, type: ToastType = 'info', duration: number = DEFAULT_DURATION) {
    const id = generateId()
    toasts.value.push({ id, message, type, duration })

    if (duration > 0) {
      setTimeout(() => {
        removeToast(id)
      }, duration)
    }
  }

  function removeToast(id: string) {
    const index = toasts.value.findIndex((t) => t.id === id)
    if (index !== -1) {
      toasts.value.splice(index, 1)
    }
  }

  return {
    toasts,
    toast,
    removeToast,
  }
}

/**
 * Clears all active toasts. Intended primarily for test cleanup.
 */
export function clearToasts() {
  toasts.value = []
}
