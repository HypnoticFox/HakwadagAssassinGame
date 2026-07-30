import { i18n } from '@/i18n'

/**
 * Mount options for components that use vue-i18n translations.
 * Sets locale to English so test assertions can use English strings.
 */
export function withI18n(locale: 'en' | 'nl' = 'en') {
  i18n.global.locale.value = locale
  return {
    global: {
      plugins: [i18n],
    },
  }
}
