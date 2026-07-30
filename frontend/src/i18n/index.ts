import { createI18n } from 'vue-i18n'

import nl from './nl.json'
import en from './en.json'

export type SupportedLocale = 'nl' | 'en'

export const SUPPORTED_LOCALES: SupportedLocale[] = ['nl', 'en']

const LOCALE_STORAGE_KEY = 'hakwadag_locale'

function getInitialLocale(): SupportedLocale {
  const stored = localStorage.getItem(LOCALE_STORAGE_KEY)
  if (stored && SUPPORTED_LOCALES.includes(stored as SupportedLocale)) {
    return stored as SupportedLocale
  }
  return 'nl'
}

export const i18n = createI18n({
  legacy: false,
  locale: getInitialLocale(),
  fallbackLocale: 'nl',
  messages: {
    nl,
    en,
  },
})

export function setLocale(locale: SupportedLocale): void {
  i18n.global.locale.value = locale
  localStorage.setItem(LOCALE_STORAGE_KEY, locale)
  document.documentElement.lang = locale
}

export function getLocale(): SupportedLocale {
  return i18n.global.locale.value as SupportedLocale
}

// Set initial html lang attribute
document.documentElement.lang = getInitialLocale()
