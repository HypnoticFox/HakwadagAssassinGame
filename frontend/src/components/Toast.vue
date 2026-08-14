<script setup lang="ts">
import { AlertTriangle, CheckCircle, Info, X, XCircle } from '@lucide/vue'
import type { Component } from 'vue'

import { useToast, type ToastType } from '@/composables/useToast'

const { toasts, removeToast } = useToast()

const icons: Record<ToastType, Component> = {
  success: CheckCircle,
  error: XCircle,
  warning: AlertTriangle,
  info: Info,
}

function getRole(type: ToastType): 'alert' | 'status' {
  return type === 'error' ? 'alert' : 'status'
}

function dismissOnEscape(event: KeyboardEvent, id: string) {
  if (event.key === 'Escape') {
    removeToast(id)
  }
}
</script>

<template>
  <Teleport to="body">
    <div
      class="toast-container"
      role="region"
      aria-label="Notifications"
      aria-live="polite"
      aria-atomic="true"
    >
      <TransitionGroup
        name="toast"
        tag="div"
        class="toast-list"
      >
        <div
          v-for="toast in toasts"
          :key="toast.id"
          class="toast"
          :class="`toast--${toast.type}`"
          :role="getRole(toast.type)"
          aria-atomic="true"
          tabindex="-1"
          @keydown="dismissOnEscape($event, toast.id)"
        >
          <component
            :is="icons[toast.type]"
            class="toast__icon"
            :class="`toast__icon--${toast.type}`"
            :size="20"
            aria-hidden="true"
          />

          <p class="toast__message">
            {{ toast.message }}
          </p>
          <button
            type="button"
            class="toast__close"
            aria-label="Dismiss notification"
            @click="removeToast(toast.id)"
          >
            <X
              :size="16"
              aria-hidden="true"
            />
          </button>
        </div>
      </TransitionGroup>
    </div>
  </Teleport>
</template>

<style scoped>
.toast-container {
  bottom: 1rem;
  left: 1rem;
  pointer-events: none;
  position: fixed;
  right: 1rem;
  z-index: 200;
}

.toast-list {
  align-items: center;
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.toast {
  align-items: flex-start;
  background: var(--surface);
  border: 1px solid var(--border);
  border-left: 4px solid var(--primary);
  border-radius: var(--radius);
  box-shadow: var(--shadow-lg);
  color: var(--text);
  display: flex;
  gap: 0.75rem;
  max-width: 100%;
  min-width: 16rem;
  padding: 0.875rem 1rem;
  pointer-events: auto;
  width: fit-content;
}

.toast--success {
  border-left-color: var(--success);
}

.toast--error {
  border-left-color: var(--danger);
}

.toast--warning {
  border-left-color: var(--warning);
}

.toast--info {
  border-left-color: var(--primary);
}

.toast__icon {
  flex-shrink: 0;
  margin-top: 0.125rem;
}

.toast__icon--success {
  color: var(--success);
}

.toast__icon--error {
  color: var(--danger);
}

.toast__icon--warning {
  color: var(--warning);
}

.toast__icon--info {
  color: var(--primary);
}

.toast__message {
  flex: 1;
  font-size: 0.9375rem;
  line-height: 1.4;
  margin: 0;
  word-break: break-word;
}

.toast__close {
  align-items: center;
  background: transparent;
  border: 0;
  border-radius: var(--radius-sm);
  color: var(--text-muted);
  cursor: pointer;
  display: inline-flex;
  flex-shrink: 0;
  justify-content: center;
  margin: -0.25rem -0.25rem -0.25rem 0;
  min-height: 1.75rem;
  min-width: 1.75rem;
  padding: 0.25rem;
  transition:
    background-color 0.15s ease,
    color 0.15s ease;
}

.toast__close:hover {
  background: var(--surface-muted);
  color: var(--text);
}

.toast__close:focus-visible {
  outline: 3px solid var(--focus);
  outline-offset: 2px;
}

.toast-enter-active,
.toast-leave-active,
.toast-move {
  transition:
    opacity 0.25s ease,
    transform 0.25s ease;
}

.toast-enter-from {
  opacity: 0;
  transform: translateY(1rem) scale(0.96);
}

.toast-leave-to {
  opacity: 0;
  transform: translateY(-0.5rem) scale(0.96);
}

@media (min-width: 640px) {
  .toast-container {
    bottom: auto;
    left: auto;
    max-width: 24rem;
    right: 1.5rem;
    top: 1.5rem;
  }

  .toast-list {
    align-items: flex-end;
  }
}
</style>
