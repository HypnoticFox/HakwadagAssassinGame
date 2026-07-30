import { createApp } from 'vue'
import { createPinia } from 'pinia'
import { Workbox } from 'workbox-window'

import App from './App.vue'
import router from './router'
import { i18n } from './i18n'
import './assets/main.css'

const app = createApp(App)

const pinia = createPinia()
app.use(pinia)
app.use(router)
app.use(i18n)
app.mount('#app')

if ('serviceWorker' in navigator) {
  const workbox = new Workbox('/sw.js')
  void workbox.register()
}
