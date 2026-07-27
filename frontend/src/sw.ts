/// <reference lib="webworker" />

import { clientsClaim } from 'workbox-core'
import { precacheAndRoute } from 'workbox-precaching'
import { registerRoute } from 'workbox-routing'
import { StaleWhileRevalidate } from 'workbox-strategies'

declare const self: ServiceWorkerGlobalScope

precacheAndRoute(self.__WB_MANIFEST)
clientsClaim()

registerRoute(
  ({ request }) => request.destination === 'document',
  new StaleWhileRevalidate({ cacheName: 'hakwadag-pages' }),
)

self.addEventListener('push', (event) => {
  const data = event.data?.json() as { title?: string; body?: string; url?: string } | undefined

  event.waitUntil(
    self.registration.showNotification(data?.title ?? 'Hakwadag update', {
      body: data?.body ?? 'You have a new game notification.',
      icon: '/icons/icon.svg',
      badge: '/icons/icon.svg',
      data: { url: data?.url ?? '/' },
    }),
  )
})

self.addEventListener('notificationclick', (event) => {
  event.notification.close()
  const url = (event.notification.data as { url?: string } | undefined)?.url ?? '/'

  event.waitUntil(self.clients.openWindow(url))
})
