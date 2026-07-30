<script setup lang="ts">
import { useI18n } from 'vue-i18n'

import { setLocale, SUPPORTED_LOCALES, type SupportedLocale } from '@/i18n'

const { t, locale } = useI18n()

function onLocaleChange(event: Event) {
  const newLocale = (event.target as HTMLSelectElement).value as SupportedLocale
  setLocale(newLocale)
}
</script>

<template>
  <label class="language-switcher">
    <span class="language-switcher__label">{{ t('language.label') }}</span>
    <select
      :value="locale"
      class="language-switcher__select"
      @change="onLocaleChange"
    >
      <option
        v-for="loc in SUPPORTED_LOCALES"
        :key="loc"
        :value="loc"
      >
        {{ loc === 'nl' ? t('language.dutch') : t('language.english') }}
      </option>
    </select>
  </label>
</template>

<style scoped>
.language-switcher {
  align-items: center;
  display: inline-flex;
  gap: 0.375rem;
}

.language-switcher__label {
  font-size: 0.75rem;
  font-weight: 600;
  white-space: nowrap;
}

.language-switcher__select {
  appearance: none;
  background: rgba(255, 255, 255, 0.15);
  border: 1px solid rgba(255, 255, 255, 0.3);
  border-radius: 0.5rem;
  color: inherit;
  cursor: pointer;
  font: inherit;
  font-size: 0.75rem;
  font-weight: 600;
  padding: 0.25rem 1.5rem 0.25rem 0.5rem;
  background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='12' height='12' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'%3E%3Cpath d='m6 9 6 6 6-6'/%3E%3C/svg%3E");
  background-repeat: no-repeat;
  background-position: right 0.25rem center;
}

.language-switcher__select:focus-visible {
  outline: 3px solid #fbbf24;
  outline-offset: 2px;
}
</style>
