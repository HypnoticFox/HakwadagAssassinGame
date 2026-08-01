<script setup lang="ts">
import { Moon, Sun } from '@lucide/vue'
import { useTheme } from '@/composables/useTheme'

const { theme, toggleTheme } = useTheme()
</script>

<template>
  <button
    type="button"
    class="theme-toggle"
    :aria-label="theme === 'light' ? 'Switch to dark mode' : 'Switch to light mode'"
    @click="toggleTheme"
  >
    <Moon
      v-if="theme === 'light'"
      :size="20"
      aria-hidden="true"
    />
    <Sun
      v-else
      :size="20"
      aria-hidden="true"
    />
  </button>
</template>

<style scoped>
.theme-toggle {
  align-items: center;
  background: transparent;
  border: 0;
  border-radius: var(--radius);
  color: inherit;
  cursor: pointer;
  display: inline-flex;
  justify-content: center;
  min-height: 2.75rem;
  min-width: 2.75rem;
  padding: 0.5rem;
}

.theme-toggle:hover {
  background: rgba(255, 255, 255, 0.1);
}

.theme-toggle:focus-visible {
  outline: 3px solid var(--focus);
  outline-offset: 2px;
}
</style>
