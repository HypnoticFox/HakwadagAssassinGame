import { ref, watch } from 'vue'

type Theme = 'light' | 'dark'

const STORAGE_KEY = 'hakwadag_theme'

function getSystemTheme(): Theme {
  if (typeof window === 'undefined') return 'light'
  return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light'
}

function getInitialTheme(): Theme {
  const stored = localStorage.getItem(STORAGE_KEY)
  if (stored === 'light' || stored === 'dark') return stored
  return getSystemTheme()
}

const currentTheme = ref<Theme>(getInitialTheme())

function applyTheme(theme: Theme) {
  document.documentElement.setAttribute('data-theme', theme)
  localStorage.setItem(STORAGE_KEY, theme)
}

// Apply initial theme
applyTheme(currentTheme.value)

// Listen for system theme changes
if (typeof window !== 'undefined') {
  window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', (e) => {
    if (!localStorage.getItem(STORAGE_KEY)) {
      currentTheme.value = e.matches ? 'dark' : 'light'
    }
  })
}

// Watch for theme changes
watch(currentTheme, (theme) => {
  applyTheme(theme)
})

export function useTheme() {
  function toggleTheme() {
    currentTheme.value = currentTheme.value === 'light' ? 'dark' : 'light'
  }

  return {
    theme: currentTheme,
    toggleTheme,
  }
}
