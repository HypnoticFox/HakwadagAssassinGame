<script setup lang="ts">
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
              <svg
                xmlns="http://www.w3.org/2000/svg"
                width="24"
                height="24"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                stroke-width="2"
                stroke-linecap="round"
                stroke-linejoin="round"
                aria-hidden="true"
              >
                <line
                  x1="18"
                  y1="6"
                  x2="6"
                  y2="18"
                />
                <line
                  x1="6"
                  y1="6"
                  x2="18"
                  y2="18"
                />
              </svg>
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
  align-items: center;
  background: rgba(15, 23, 42, 0.6);
  backdrop-filter: blur(2px);
  display: flex;
  inset: 0;
  justify-content: center;
  padding: 1rem;
  position: fixed;
  z-index: 100;
}

.modal {
  animation: modal-in 0.25s ease-out;
  background: white;
  border-radius: 1rem;
  box-shadow: 0 20px 40px rgba(15, 23, 42, 0.2);
  max-width: 28rem;
  width: 100%;
}

.modal-header {
  align-items: center;
  border-bottom: 1px solid #e2e8f0;
  display: flex;
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
  color: #64748b;
  cursor: pointer;
  display: flex;
  height: 2.25rem;
  justify-content: center;
  padding: 0;
  width: 2.25rem;
}

.modal-close:hover {
  background: #f1f5f9;
  color: #0f172a;
}

.modal-close:focus-visible {
  outline: 3px solid #fbbf24;
  outline-offset: 2px;
}

.modal-body {
  padding: 1.25rem;
}

.modal-footer {
  border-top: 1px solid #e2e8f0;
  display: flex;
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
