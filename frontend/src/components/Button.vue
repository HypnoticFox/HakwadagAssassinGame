<script setup lang="ts">
withDefaults(
  defineProps<{
    variant?: 'primary' | 'secondary' | 'danger' | 'ghost'
    size?: 'default' | 'large'
    type?: 'button' | 'submit' | 'reset'
    disabled?: boolean
    loading?: boolean
    fullWidth?: boolean
  }>(),
  {
    variant: 'primary',
    size: 'default',
    type: 'button',
  },
)

const emit = defineEmits<{
  click: [event: MouseEvent]
}>()
</script>

<template>
  <button
    :type="type"
    class="button"
    :class="[
      {
        'button--primary': variant === 'primary',
        'button--secondary': variant === 'secondary',
        'button--danger': variant === 'danger',
        'button--ghost': variant === 'ghost',
        'button--large': size === 'large',
        'button--full-width': fullWidth,
        'button--loading': loading,
      },
    ]"
    :disabled="disabled || loading"
    @click="emit('click', $event)"
  >
    <span
      class="button__spinner"
      aria-hidden="true"
    />
    <span class="button__content">
      <slot />
    </span>
  </button>
</template>

<style scoped>
.button {
  align-items: center;
  border: 0;
  border-radius: 0.75rem;
  cursor: pointer;
  display: inline-flex;
  font-family: inherit;
  font-weight: 600;
  justify-content: center;
  line-height: 1.25;
  min-height: 2.875rem;
  padding: 0.75rem 1.25rem;
  position: relative;
  transition:
    transform 0.15s ease,
    background-color 0.15s ease,
    box-shadow 0.15s ease;
  -webkit-tap-highlight-color: transparent;
}

.button:active:not(:disabled) {
  transform: scale(0.98);
}

.button:focus-visible {
  outline: 3px solid #fbbf24;
  outline-offset: 2px;
}

.button:disabled {
  cursor: not-allowed;
  opacity: 0.6;
}

.button--primary {
  background: #1d4ed8;
  box-shadow: 0 4px 14px rgba(29, 78, 216, 0.25);
  color: white;
}

.button--primary:hover:not(:disabled) {
  background: #1e40af;
}

.button--secondary {
  background: #e2e8f0;
  color: #172554;
}

.button--secondary:hover:not(:disabled) {
  background: #cbd5e1;
}

.button--danger {
  background: #dc2626;
  color: white;
}

.button--danger:hover:not(:disabled) {
  background: #b91c1c;
}

.button--ghost {
  background: transparent;
  color: #1d4ed8;
  padding-left: 0.75rem;
  padding-right: 0.75rem;
}

.button--ghost:hover:not(:disabled) {
  background: #eff6ff;
}

.button--large {
  font-size: 1.125rem;
  min-height: 3.5rem;
  padding: 1rem 1.5rem;
}

.button--full-width {
  width: 100%;
}

.button__spinner {
  border: 2px solid rgba(255, 255, 255, 0.3);
  border-radius: 50%;
  border-top-color: currentColor;
  height: 1rem;
  opacity: 0;
  position: absolute;
  transition: opacity 0.15s ease;
  width: 1rem;
}

.button--loading .button__spinner {
  animation: spin 0.8s linear infinite;
  opacity: 1;
}

.button--loading .button__content {
  opacity: 0;
}

.button--secondary .button__spinner,
.button--ghost .button__spinner {
  border-color: rgba(23, 37, 84, 0.2);
  border-top-color: #172554;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}
</style>
