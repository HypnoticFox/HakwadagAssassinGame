<script setup lang="ts">
import { onMounted, onUnmounted, ref, useTemplateRef } from 'vue'

import { setLocale, SUPPORTED_LOCALES, type SupportedLocale } from '@/i18n'

const localeConfig: Record<SupportedLocale, { name: string }> = {
  nl: { name: 'Nederlands' },
  en: { name: 'English' },
}

const currentLocale = ref<SupportedLocale>(
  (document.documentElement.lang as SupportedLocale) || 'nl',
)
const isOpen = ref(false)
const dropdownRef = useTemplateRef<HTMLElement>('dropdown')

function toggle() {
  isOpen.value = !isOpen.value
}

function selectLocale(loc: SupportedLocale) {
  currentLocale.value = loc
  setLocale(loc)
  isOpen.value = false
}

function onClickOutside(event: MouseEvent) {
  if (dropdownRef.value && !dropdownRef.value.contains(event.target as Node)) {
    isOpen.value = false
  }
}

onMounted(() => {
  document.addEventListener('click', onClickOutside)
})

onUnmounted(() => {
  document.removeEventListener('click', onClickOutside)
})
</script>

<template>
  <div
    ref="dropdown"
    class="language-switcher"
  >
    <button
      type="button"
      class="language-switcher__trigger"
      :aria-expanded="isOpen"
      aria-haspopup="listbox"
      @click="toggle"
    >
      {{ currentLocale.toUpperCase() }}
    </button>
    <ul
      v-if="isOpen"
      class="language-switcher__menu"
      role="listbox"
    >
      <li
        v-for="loc in SUPPORTED_LOCALES"
        :key="loc"
        role="option"
        :aria-selected="loc === currentLocale"
        class="language-switcher__option"
        :class="{ 'language-switcher__option--selected': loc === currentLocale }"
        @click="selectLocale(loc)"
      >
        {{ localeConfig[loc].name }}
      </li>
    </ul>
  </div>
</template>

<style scoped>
.language-switcher {
  display: inline-block;
  position: relative;
}

.language-switcher__trigger {
  align-items: center;
  background: var(--surface);
  border: 1px solid var(--border);
  border-radius: 0.5rem;
  color: var(--text);
  cursor: pointer;
  display: inline-flex;
  font-size: 0.75rem;
  font-weight: 600;
  justify-content: center;
  line-height: 1;
  padding: 0.25rem 0.5rem;
}

.language-switcher__trigger:focus-visible {
  outline: 3px solid var(--focus);
  outline-offset: 2px;
}

.language-switcher__menu {
  background: var(--surface);
  border: 1px solid var(--border);
  border-radius: 0.5rem;
  box-shadow: var(--shadow);
  color: var(--text);
  list-style: none;
  margin: 0.25rem 0 0;
  min-width: 10rem;
  padding: 0.25rem;
  position: absolute;
  right: 0;
  z-index: 100;
}

.language-switcher__option {
  border-radius: 0.375rem;
  color: var(--text);
  cursor: pointer;
  font-size: 0.875rem;
  font-weight: 500;
  padding: 0.5rem 0.75rem;
}

.language-switcher__option:hover {
  background: var(--surface-muted);
}

.language-switcher__option--selected {
  background: var(--surface-muted);
}
</style>
