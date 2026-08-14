<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref, watch } from 'vue'
import { Timer } from '@lucide/vue'

const props = defineProps<{
  availableAt: string
}>()

const emit = defineEmits<{
  expired: []
}>()

const now = ref(Date.now())
let timerId: number | null = null

const remainingSeconds = computed(() => {
  const target = new Date(props.availableAt).getTime()
  return Math.max(0, Math.floor((target - now.value) / 1000))
})

const formattedTime = computed(() => {
  const minutes = Math.floor(remainingSeconds.value / 60)
  const seconds = remainingSeconds.value % 60
  return `${String(minutes).padStart(2, '0')}:${String(seconds).padStart(2, '0')}`
})

function tick() {
  now.value = Date.now()
  // Don't emit expired - just stay at 0 and wait for backend SignalR update
}

function startTimer() {
  stopTimer()
  now.value = Date.now()
  if (remainingSeconds.value === 0) {
    emit('expired')
    return
  }
  timerId = window.setInterval(tick, 1000)
}

function stopTimer() {
  if (timerId !== null) {
    window.clearInterval(timerId)
    timerId = null
  }
}

watch(
  () => props.availableAt,
  () => startTimer(),
)

onMounted(startTimer)
onUnmounted(stopTimer)
</script>

<template>
  <div class="pending-card">
    <Timer :size="32" class="pending-icon" aria-hidden="true" />
    <h2 class="pending-title">
      {{ $t('assignment.pendingTag.title') }}
    </h2>
    <p class="pending-message">
      {{ $t('assignment.pendingTag.message') }}
    </p>
    <p class="pending-time" role="timer" aria-live="polite">
      {{ formattedTime }}
    </p>
  </div>
</template>

<style scoped>
.pending-card {
  align-items: center;
  background: var(--surface);
  border: 1px solid var(--border);
  border-radius: var(--radius-lg);
  box-shadow: var(--shadow-sm);
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  margin: 0 auto;
  max-width: 24rem;
  padding: 2rem 1.5rem;
  text-align: center;
  width: 100%;
}

.pending-icon {
  color: var(--primary);
  margin-bottom: 0.5rem;
}

.pending-title {
  color: var(--text);
  font-size: 1.125rem;
  margin: 0;
}

.pending-message {
  color: var(--text-muted);
  margin: 0;
}

.pending-time {
  color: var(--text);
  font-size: 2.5rem;
  font-variant-numeric: tabular-nums;
  font-weight: 700;
  letter-spacing: 0.05em;
  margin: 0.75rem 0 0;
}
</style>
