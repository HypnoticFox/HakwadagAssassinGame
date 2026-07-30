<script setup lang="ts">
import { RouterLink, RouterView, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'

import DevPlayerSwitcher from '@/components/DevPlayerSwitcher.vue'
import LanguageSwitcher from '@/components/LanguageSwitcher.vue'
import { useAuthStore } from '@/stores'

const { t } = useI18n()
const authStore = useAuthStore()
const router = useRouter()

const isDev = import.meta.env.DEV

async function logout() {
  authStore.logout()
  await router.push('/login')
}
</script>

<template>
  <div class="app-shell">
    <header class="app-header">
      <RouterLink class="app-title" to="/">
        <span class="app-title__icon">⚔</span>
        {{ $t('app.name') }}
      </RouterLink>
      <nav :aria-label="t('app.nav.mainNavigation')">
        <RouterLink v-if="authStore.isAuthenticated" to="/">
          {{ $t('app.nav.home') }}
        </RouterLink>
        <RouterLink v-if="!authStore.isAuthenticated" to="/login">
          {{ $t('app.nav.login') }}
        </RouterLink>
        <button
          v-if="authStore.isAuthenticated"
          type="button"
          class="logout-button"
          @click="logout"
        >
          {{ $t('app.nav.logout') }}
        </button>
        <LanguageSwitcher />
      </nav>
    </header>

    <main class="app-content">
      <RouterView v-slot="{ Component }">
        <Transition name="page" mode="out-in">
          <component :is="Component" />
        </Transition>
      </RouterView>
    </main>

    <DevPlayerSwitcher v-if="isDev" />
  </div>
</template>

<style>
.app-title__icon {
  display: inline-block;
  margin-right: 0.375rem;
}

.logout-button {
  align-items: center;
  background: transparent;
  border: 0;
  color: inherit;
  cursor: pointer;
  display: inline-flex;
  font: inherit;
  min-height: 2.75rem;
  padding: 0.75rem 0;
}

.logout-button:focus-visible {
  outline: 3px solid #fbbf24;
  outline-offset: 2px;
}

.page-enter-active,
.page-leave-active {
  transition:
    opacity 0.2s ease,
    transform 0.2s ease;
}

.page-enter-from {
  opacity: 0;
  transform: translateX(0.25rem);
}

.page-leave-to {
  opacity: 0;
  transform: translateX(-0.25rem);
}
</style>
