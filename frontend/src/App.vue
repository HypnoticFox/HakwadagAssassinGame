<script setup lang="ts">
import { Menu, Swords } from '@lucide/vue'
import { onMounted, onUnmounted, ref, useTemplateRef } from 'vue'
import { RouterLink, RouterView, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'

import DevPlayerSwitcher from '@/components/DevPlayerSwitcher.vue'
import LanguageSwitcher from '@/components/LanguageSwitcher.vue'
import ThemeToggle from '@/components/ThemeToggle.vue'
import { useAuthStore } from '@/stores'

const { t } = useI18n()
const authStore = useAuthStore()
const router = useRouter()

const isDev = import.meta.env.DEV
const isMobileMenuOpen = ref(false)
const mobileNavRef = useTemplateRef<HTMLElement>('mobileNav')

function toggleMobileMenu() {
  isMobileMenuOpen.value = !isMobileMenuOpen.value
}

function closeMobileMenu() {
  isMobileMenuOpen.value = false
}

function onClickOutside(event: MouseEvent) {
  if (mobileNavRef.value && !mobileNavRef.value.contains(event.target as Node)) {
    isMobileMenuOpen.value = false
  }
}

function onEscape(event: KeyboardEvent) {
  if (event.key === 'Escape') {
    isMobileMenuOpen.value = false
  }
}

onMounted(() => {
  document.addEventListener('click', onClickOutside)
  document.addEventListener('keydown', onEscape)
})

onUnmounted(() => {
  document.removeEventListener('click', onClickOutside)
  document.removeEventListener('keydown', onEscape)
})

async function logout() {
  authStore.logout()
  await router.push('/login')
  closeMobileMenu()
}
</script>

<template>
  <div class="app-shell">
    <header class="app-header">
      <div
        ref="mobileNav"
        class="mobile-nav"
      >
        <button
          id="mobile-nav-toggle"
          type="button"
          class="mobile-nav__trigger"
          aria-controls="mobile-nav-menu"
          :aria-expanded="isMobileMenuOpen"
          :aria-label="isMobileMenuOpen ? t('app.nav.closeMenu') : t('app.nav.openMenu')"
          @click="toggleMobileMenu"
        >
          <Menu :size="24" />
        </button>
        <div
          v-if="isMobileMenuOpen"
          id="mobile-nav-menu"
          class="mobile-nav__menu"
          role="navigation"
          :aria-label="t('app.nav.mobileNavigation')"
        >
          <RouterLink
            v-if="authStore.isAuthenticated"
            class="mobile-nav__item"
            to="/"
            @click="closeMobileMenu"
          >
            {{ $t('app.nav.home') }}
          </RouterLink>
          <RouterLink
            v-if="!authStore.isAuthenticated"
            class="mobile-nav__item"
            to="/login"
            @click="closeMobileMenu"
          >
            {{ $t('app.nav.login') }}
          </RouterLink>
          <button
            v-if="authStore.isAuthenticated"
            type="button"
            class="mobile-nav__item"
            @click="logout"
          >
            {{ $t('app.nav.logout') }}
          </button>
        </div>
      </div>
      <RouterLink class="app-title" to="/">
        <Swords
          class="app-title__icon"
          :size="20"
        />
        {{ $t('app.name') }}
      </RouterLink>
      <div class="header-actions">
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
          <ThemeToggle />
          <LanguageSwitcher />
        </nav>
      </div>
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
  color: currentColor;
  flex-shrink: 0;
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
  outline: 3px solid var(--focus);
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

<style scoped>
.header-actions {
  align-items: center;
  display: flex;
  gap: 0.75rem;
}

.mobile-nav {
  display: none;
  position: relative;
}

.mobile-nav__trigger {
  align-items: center;
  background: transparent;
  border: 0;
  border-radius: var(--radius-sm);
  color: var(--text-inverse);
  cursor: pointer;
  display: inline-flex;
  justify-content: center;
  min-height: 2.75rem;
  min-width: 2.75rem;
  padding: 0.25rem;
  transition: background-color 0.15s ease;
}

.mobile-nav__trigger:hover {
  background: color-mix(in srgb, var(--text-inverse) 15%, transparent);
}

.mobile-nav__trigger:focus-visible {
  outline: 3px solid var(--focus);
  outline-offset: 2px;
}

.mobile-nav__menu {
  background: var(--surface);
  border: 2px solid var(--border-input);
  border-radius: 0 0 0.5rem 0;
  box-shadow: var(--shadow-lg);
  color: var(--text);
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
  min-width: 12rem;
  padding: 0.5rem;
  position: fixed;
  left: 0;
  top: 4.25rem;
  z-index: 100;
}

.mobile-nav__item {
  align-items: center;
  background: transparent;
  border: 0;
  border-radius: var(--radius-sm);
  color: var(--text);
  cursor: pointer;
  display: flex;
  font: inherit;
  font-size: 0.9375rem;
  font-weight: 500;
  justify-content: flex-start;
  min-height: 2.75rem;
  padding: 0.75rem 1rem;
  text-align: left;
  text-decoration: none;
  transition: background-color 0.15s ease;
}

.mobile-nav__item:hover {
  background: var(--surface-muted);
}

.mobile-nav__item:focus-visible {
  outline: 3px solid var(--focus);
  outline-offset: 2px;
}

.mobile-nav__item.router-link-active {
  color: var(--primary);
}

@media (max-width: 480px) {
  .mobile-nav {
    display: block;
  }
}
</style>
