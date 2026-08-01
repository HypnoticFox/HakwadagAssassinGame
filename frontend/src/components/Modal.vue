<script setup lang="ts">
import { X } from '@lucide/vue'
import { useI18n } from 'vue-i18n'

const { t } = useI18n()

defineProps<{
  open: boolean
  title: string
}>()

const emit = defineEmits<{
  close: []
}>()

function onBackdropClick(event: MouseEvent) {
  if (event.target === event.currentTarget) {
    emit('close')
  }
}
</script>

<template>
  <Teleport to="body">
    <Transition name="modal">
      <div
        v-if="open"
        class="modal-backdrop"
        @click="onBackdropClick"
      >
        <div
          class="modal"
          role="dialog"
          aria-modal="true"
        >
          <header class="modal-header">
            <h2 class="modal-title">
              {{ title }}
            </h2>
            <button
              type="button"
              class="modal-close"
              :aria-label="t('common.close')"
              @click="emit('close')"
            >
              <X
                :size="24"
                aria-hidden="true"
              />
            </button>
          </header>
          <div class="modal-body">
            <slot />
          </div>
          <footer
            v-if="$slots.footer"
            class="modal-footer"
          >
            <slot name="footer" />
          </footer>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<style scoped>
.modal-backdrop {
  background: var(--backdrop);
  backdrop-filter: blur(2px);
  display: flex;
  inset: 0;
  overflow-y: auto;
  padding: 1rem;
  position: fixed;
  z-index: 100;
}

.modal-backdrop::before {
  content: '';
  display: block;
  min-height: 100%;
}

.modal {
  animation: modal-in 0.25s ease-out;
  background: var(--surface);
  border-radius: 1rem;
  box-shadow: var(--shadow-lg);
  display: flex;
  flex-direction: column;
  margin: auto;
  max-height: calc(100vh - 2rem);
  max-width: 28rem;
  overflow: hidden;
  width: 100%;
}

.modal-header {
  align-items: center;
  border-bottom: 1px solid var(--border);
  display: flex;
  flex-shrink: 0;
  justify-content: space-between;
  padding: 1rem 1.25rem;
}

.modal-title {
  font-size: 1.125rem;
  font-weight: 700;
  margin: 0;
}

.modal-close {
  align-items: center;
  background: transparent;
  border: 0;
  border-radius: 0.5rem;
  color: var(--text-muted);
  cursor: pointer;
  display: flex;
  height: 2.25rem;
  justify-content: center;
  padding: 0;
  width: 2.25rem;
}

.modal-close:hover {
  background: var(--surface-muted);
  color: var(--text);
}

.modal-close:focus-visible {
  outline: 3px solid var(--focus);
  outline-offset: 2px;
}

.modal-body {
  flex: 1;
  min-height: 0;
  overflow-y: auto;
  padding: 1.25rem;
}

.modal-footer {
  border-top: 1px solid var(--border);
  display: flex;
  flex-shrink: 0;
  gap: 0.75rem;
  justify-content: flex-end;
  padding: 1rem 1.25rem;
}

.modal-enter-active,
.modal-leave-active {
  transition: opacity 0.2s ease;
}

.modal-enter-from,
.modal-leave-to {
  opacity: 0;
}

@keyframes modal-in {
  from {
    opacity: 0;
    transform: translateY(0.5rem) scale(0.98);
  }
  to {
    opacity: 1;
    transform: translateY(0) scale(1);
  }
}
</style>
