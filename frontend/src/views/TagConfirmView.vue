<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'

import Button from '@/components/Button.vue'
import { useGameSignalR } from '@/composables/useSignalR'
import { useGameStore, useTagStore } from '@/stores'
import { TagStatus, tagStatusLabel } from '@/types'

const route = useRoute()
const router = useRouter()
const gameStore = useGameStore()
const tagStore = useTagStore()

const gameId = computed(() => route.params.id as string)
const tagId = computed(() => route.params.tagId as string)
const localError = ref<string | null>(null)

useGameSignalR(gameId.value)

onMounted(async () => {
  await gameStore.loadGame(gameId.value)
  await tagStore.loadPendingTag(gameId.value)
})

const currentTag = computed(() => {
  if (tagStore.pendingTag && tagStore.pendingTag.id === tagId.value) {
    return tagStore.pendingTag
  }
  return null
})

async function onConfirm() {
  localError.value = null
  try {
    await tagStore.confirmTag(gameId.value, tagId.value)
    await router.push(`/games/${gameId.value}/leaderboard`)
  } catch (err) {
    if (err instanceof Error) {
      localError.value = err.message
    }
  }
}

async function onDeny() {
  localError.value = null
  try {
    await tagStore.denyTag(gameId.value, tagId.value)
    await router.push(`/games/${gameId.value}/leaderboard`)
  } catch (err) {
    if (err instanceof Error) {
      localError.value = err.message
    }
  }
}

async function onVoid() {
  localError.value = null
  try {
    await tagStore.voidTag(gameId.value, tagId.value)
    await router.push(`/games/${gameId.value}/leaderboard`)
  } catch (err) {
    if (err instanceof Error) {
      localError.value = err.message
    }
  }
}

const isResolved = computed(() => {
  if (!currentTag.value) return false
  return currentTag.value.status !== TagStatus.Pending
})
</script>

<template>
  <section class="page-section">
    <div v-if="currentTag">
      <p class="eyebrow">
        {{ $t('tagConfirm.eyebrow') }}
      </p>
      <h1>{{ $t('tagConfirm.title') }}</h1>
      <p class="tag-status">
        {{ $t('tagConfirm.status') }}
        <span
          class="status"
          :class="{
            'status--0': currentTag.status === TagStatus.Pending,
            'status--1': currentTag.status === TagStatus.Confirmed,
            'status--2': currentTag.status === TagStatus.Denied,
            'status--3': currentTag.status === TagStatus.Voided,
          }"
        >{{ tagStatusLabel(currentTag.status) }}</span>
      </p>

      <div class="tag-card">
        <p><strong>{{ $t('tagConfirm.hunter') }}</strong> {{ currentTag.hunterId }}</p>
        <p><strong>{{ $t('tagConfirm.target') }}</strong> {{ currentTag.targetId }}</p>
        <p><strong>{{ $t('tagConfirm.submitted') }}</strong> {{ new Date(currentTag.submittedAt).toLocaleString() }}</p>
        <p v-if="currentTag.resolvedAt">
          <strong>{{ $t('tagConfirm.resolved') }}</strong> {{ new Date(currentTag.resolvedAt).toLocaleString() }}
        </p>
      </div>

      <div
        v-if="!isResolved"
        class="tag-actions"
      >
        <Button
          variant="secondary"
          full-width
          @click="onDeny"
        >
          {{ $t('tagConfirm.deny') }}
        </Button>
        <Button
          full-width
          @click="onConfirm"
        >
          {{ $t('tagConfirm.confirm') }}
        </Button>
      </div>
      <div
        v-else
        class="tag-actions"
      >
        <Button
          v-if="
            currentTag.status === TagStatus.Pending || currentTag.status === TagStatus.Confirmed
          "
          variant="danger"
          full-width
          @click="onVoid"
        >
          {{ $t('tagConfirm.voidTag') }}
        </Button>
      </div>

      <Button
        variant="ghost"
        full-width
        @click="router.push(`/games/${gameId}/leaderboard`)"
      >
        {{ $t('tagConfirm.backToLeaderboard') }}
      </Button>

      <p
        v-if="localError"
        class="form-error"
        role="alert"
      >
        {{ localError }}
      </p>
    </div>

    <div
      v-else-if="tagStore.isLoading"
      class="loading"
    >
      {{ $t('tagConfirm.loading') }}
    </div>
    <div
      v-else
      class="empty"
    >
      <p>{{ $t('tagConfirm.notFound') }}</p>
      <Button @click="router.push(`/games/${gameId}/leaderboard`)">
        {{ $t('tagConfirm.backToLeaderboard') }}
      </Button>
    </div>
  </section>
</template>

<style scoped>
.tag-status {
  color: #64748b;
  margin-bottom: 1.5rem;
}

.status {
  border-radius: 9999px;
  font-size: 0.75rem;
  font-weight: 700;
  padding: 0.25rem 0.75rem;
  text-transform: uppercase;
}

.status--0 {
  background: #fef3c7;
  color: #92400e;
}

.status--1 {
  background: #dcfce7;
  color: #166534;
}

.status--2 {
  background: #fee2e2;
  color: #991b1b;
}

.status--3 {
  background: #f1f5f9;
  color: #475569;
}

.tag-card {
  background: white;
  border: 1px solid #e2e8f0;
  border-radius: 1rem;
  display: grid;
  gap: 0.75rem;
  margin-bottom: 1.5rem;
  padding: 1.25rem;
}

.tag-card p {
  color: #334155;
  margin: 0;
}

.tag-actions {
  display: grid;
  gap: 0.75rem;
  margin-bottom: 1rem;
}

.form-error {
  background: #fef2f2;
  border-radius: 0.5rem;
  color: #991b1b;
  font-size: 0.875rem;
  margin-top: 1rem;
  padding: 0.75rem;
}

.loading,
.empty {
  color: #64748b;
  padding: 2rem 0;
  text-align: center;
}
</style>
