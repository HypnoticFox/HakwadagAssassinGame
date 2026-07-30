import { describe, it, expect, beforeEach, vi } from 'vitest'
import { api } from '@/api/client'

vi.mock('@/api/client', () => ({
  api: {
    getVapidPublicKey: vi.fn(),
    subscribePush: vi.fn(),
  },
}))

beforeEach(() => {
  vi.clearAllMocks()
  vi.resetModules()
})

async function loadUsePushNotifications() {
  // Use dynamic import so the module-level import.meta.env is fresh
  const mod = await import('@/composables/usePushNotifications')
  return mod.usePushNotifications
}

function setupBrowserMocks() {
  vi.stubGlobal('navigator', {
    serviceWorker: {
      ready: Promise.resolve({
        pushManager: {
          subscribe: vi.fn().mockResolvedValue({
            endpoint: 'https://example.com/push',
            toJSON: () => ({
              endpoint: 'https://example.com/push',
              keys: { p256dh: 'key1', auth: 'auth1' },
            }),
          }),
        },
      }),
    },
  })

  vi.stubGlobal('Notification', {
    requestPermission: vi.fn().mockResolvedValue('granted'),
    permission: 'granted',
  })

  vi.stubGlobal('window', {
    ...window,
    atob: (str: string) => Buffer.from(str, 'base64').toString('binary'),
    PushManager: {},
  })
}

// Test urlBase64ToUint8Array indirectly via registerSubscription
describe('urlBase64ToUint8Array', () => {
  it('is used internally by registerSubscription', async () => {
    setupBrowserMocks()

    vi.stubGlobal('window', {
      ...window,
      atob: (str: string) => Buffer.from(str, 'base64').toString('binary'),
    })

    // Set VAPID key via env
    await vi.stubEnv('VITE_VAPID_PUBLIC_KEY', 'dGVzdC12YXBpZC1rZXk')

    vi.mocked(api.subscribePush).mockResolvedValue(undefined)

    const usePushNotifications = await loadUsePushNotifications()
    const { registerSubscription, isSubscribed, error } = usePushNotifications()
    const result = await registerSubscription()

    expect(result).toBe(true)
    expect(isSubscribed.value).toBe(true)
    expect(error.value).toBeNull()
  })
})

describe('usePushNotifications', () => {
  describe('isSupported', () => {
    it('returns true when serviceWorker and PushManager are available', async () => {
      vi.stubGlobal('navigator', { serviceWorker: {} })
      vi.stubGlobal('window', { ...window, PushManager: {} })
      const usePushNotifications = await loadUsePushNotifications()
      const { isSupported } = usePushNotifications()
      expect(isSupported.value).toBe(true)
    })

    it('returns false when serviceWorker is missing', async () => {
      vi.stubGlobal('navigator', {})
      vi.stubGlobal('window', { ...window, PushManager: {} })
      const usePushNotifications = await loadUsePushNotifications()
      const { isSupported } = usePushNotifications()
      expect(isSupported.value).toBe(false)
    })

    it('returns false when PushManager is missing', async () => {
      vi.stubGlobal('navigator', { serviceWorker: {} })
      const win = { ...window }
      delete (win as any).PushManager
      vi.stubGlobal('window', win)
      const usePushNotifications = await loadUsePushNotifications()
      const { isSupported } = usePushNotifications()
      expect(isSupported.value).toBe(false)
    })
  })

  describe('requestPermission', () => {
    it('returns true when permission is granted', async () => {
      setupBrowserMocks()
      const usePushNotifications = await loadUsePushNotifications()
      const { requestPermission, permission } = usePushNotifications()

      const result = await requestPermission()

      expect(result).toBe(true)
      expect(permission.value).toBe('granted')
    })

    it('returns false when permission is denied', async () => {
      vi.stubGlobal('navigator', { serviceWorker: {} })
      vi.stubGlobal('Notification', {
        requestPermission: vi.fn().mockResolvedValue('denied'),
        permission: 'default',
      })
      const usePushNotifications = await loadUsePushNotifications()
      const { requestPermission, permission } = usePushNotifications()

      const result = await requestPermission()

      expect(result).toBe(false)
      expect(permission.value).toBe('denied')
    })

    it('returns false when not supported', async () => {
      vi.stubGlobal('navigator', {})
      const usePushNotifications = await loadUsePushNotifications()
      const { requestPermission, error } = usePushNotifications()

      const result = await requestPermission()

      expect(result).toBe(false)
      expect(error.value).toBe('Push notifications are not supported on this device.')
    })

    it('catches errors from Notification.requestPermission', async () => {
      vi.stubGlobal('navigator', { serviceWorker: {} })
      vi.stubGlobal('Notification', {
        requestPermission: vi.fn().mockRejectedValue(new Error('Permission error')),
        permission: 'default',
      })
      const usePushNotifications = await loadUsePushNotifications()
      const { requestPermission, error } = usePushNotifications()

      const result = await requestPermission()

      expect(result).toBe(false)
      expect(error.value).toBe('Permission error')
    })
  })

  describe('registerSubscription', () => {
    it('registers subscription successfully', async () => {
      setupBrowserMocks()

      await vi.stubEnv('VITE_VAPID_PUBLIC_KEY', 'dGVzdC12YXBpZC1rZXk')

      vi.mocked(api.subscribePush).mockResolvedValue(undefined)

      const usePushNotifications = await loadUsePushNotifications()
      const { registerSubscription, isSubscribed, error } = usePushNotifications()
      const result = await registerSubscription()

      expect(result).toBe(true)
      expect(isSubscribed.value).toBe(true)
      expect(error.value).toBeNull()
    })

    it('fetches VAPID key from server when not in env', async () => {
      setupBrowserMocks()

      // Empty env key so it fetches from server
      await vi.stubEnv('VITE_VAPID_PUBLIC_KEY', '')

      vi.mocked(api.getVapidPublicKey).mockResolvedValue('server-vapid-key')
      vi.mocked(api.subscribePush).mockResolvedValue(undefined)

      const usePushNotifications = await loadUsePushNotifications()
      const { registerSubscription } = usePushNotifications()
      const result = await registerSubscription()

      expect(result).toBe(true)
      expect(api.getVapidPublicKey).toHaveBeenCalled()
    })

    it('sets error when server VAPID key fetch fails', async () => {
      setupBrowserMocks()

      await vi.stubEnv('VITE_VAPID_PUBLIC_KEY', '')

      vi.mocked(api.getVapidPublicKey).mockRejectedValue(new Error('Server error'))

      const usePushNotifications = await loadUsePushNotifications()
      const { registerSubscription, isSubscribed, error } = usePushNotifications()
      const result = await registerSubscription()

      expect(result).toBe(false)
      expect(error.value).toBe('Could not retrieve push configuration from the server.')
      expect(isSubscribed.value).toBe(false)
    })

    it('sets error when not supported', async () => {
      vi.stubGlobal('navigator', {})
      const usePushNotifications = await loadUsePushNotifications()
      const { registerSubscription, error } = usePushNotifications()

      const result = await registerSubscription()

      expect(result).toBe(false)
      expect(error.value).toBe('Push notifications are not supported on this device.')
    })

    it('returns false when permission is denied', async () => {
      vi.stubGlobal('navigator', { serviceWorker: {} })
      vi.stubGlobal('Notification', {
        requestPermission: vi.fn().mockResolvedValue('denied'),
        permission: 'default',
      })
      const usePushNotifications = await loadUsePushNotifications()
      const { registerSubscription, error } = usePushNotifications()

      const result = await registerSubscription()

      expect(result).toBe(false)
      expect(error.value).toBe('Permission to send notifications was denied.')
    })
  })
})
