<script setup lang="ts">
import { ArrowLeft } from '@lucide/vue'

defineProps<{
  label: string
}>()

const emit = defineEmits<{
  click: [event: MouseEvent]
}>()
</script>

<template>
  <button
    type="button"
    class="back-button"
    @click="emit('click', $event)"
  >
    <ArrowLeft
      :size="16"
      aria-hidden="true"
    />
    <span>{{ label }}</span>
  </button>
</template>

<style scoped>
.back-button {
  align-items: center;
  background: transparent;
  border: 0;
  border-radius: var(--radius);
  color: var(--text-secondary);
  cursor: pointer;
  display: inline-flex;
  font-family: inherit;
  font-size: 0.875rem;
  font-weight: 500;
  gap: 0.375rem;
  line-height: 1.25;
  padding: 0.5rem 0.75rem;
  transition:
    background-color 0.15s ease,
    color 0.15s ease;
  -webkit-tap-highlight-color: transparent;
}

.back-button:hover {
  background: var(--surface-muted);
  color: var(--primary);
}

.back-button:focus-visible {
  outline: 3px solid var(--focus);
  outline-offset: 2px;
}
</style>
