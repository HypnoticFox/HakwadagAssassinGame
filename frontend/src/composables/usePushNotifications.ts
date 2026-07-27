import { ref } from 'vue'

import { api } from '@/api/client'

const VAPID_PUBLIC_KEY = import.meta.env.VITE_VAPID_PUBLIC_KEY as string | undefined

function urlBase64ToUint8Array(base64String: string): ArrayBuffer {
  const padding = '='.repeat((4 - (base64String.length % 4)) % 4)
  const base64 = (base64String + padding).replace(/-/g, '+').replace(/_/g, '/')
  const rawData = window.atob(base64)
  const outputArray = new Uint8Array(rawData.length)
  for (let i = 0; i < rawData.length; ++i) {
    outputArray[i] = rawData.charCodeAt(i)
  }
  return outputArray.buffer
}

export function usePushNotifications() {
  const isSupported = ref('serviceWorker' in navigator && 'PushManager' in window)
  const permission = ref<NotificationPermission>('default')
  const isSubscribed = ref(false)
  const error = ref<string | null>(null)

  async function requestPermission() {
    if (!isSupported.value) {
      error.value = 'Push notifications are not supported on this device.'
      return false
    }

    try {
      const result = await Notification.requestPermission()
      permission.value = result
      return result === 'granted'
    } catch (err) {
      if (err instanceof Error) {
        error.value = err.message
      }
      return false
    }
  }

  async function registerSubscription() {
    if (!isSupported.value) {
      error.value = 'Push notifications are not supported on this device.'
      return false
    }

    const granted = await requestPermission()
    if (!granted) {
      error.value = 'Permission to send notifications was denied.'
      return false
    }

    try {
      const registration = await navigator.serviceWorker.ready
      let publicKey = VAPID_PUBLIC_KEY
      if (!publicKey) {
        try {
          publicKey = await api.getVapidPublicKey()
        } catch {
          error.value = 'Could not retrieve push configuration from the server.'
          return false
        }
      }

      if (!publicKey) {
        error.value = 'Push configuration is missing.'
        return false
      }

      const subscription = await registration.pushManager.subscribe({
        userVisibleOnly: true,
        applicationServerKey: urlBase64ToUint8Array(publicKey),
      })

      await api.subscribePush(subscription)
      isSubscribed.value = true
      return true
    } catch (err) {
      if (err instanceof Error) {
        error.value = err.message
      }
      return false
    }
  }

  return {
    isSupported,
    permission,
    isSubscribed,
    error,
    requestPermission,
    registerSubscription,
  }
}
