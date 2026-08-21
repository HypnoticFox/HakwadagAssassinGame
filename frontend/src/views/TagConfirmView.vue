<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'

import Button from '@/components/Button.vue'
import BackButton from '@/components/BackButton.vue'
import LoadingSpinner from '@/components/LoadingSpinner.vue'
import { useGameSignalR } from '@/composables/useSignalR'
import { useToast } from '@/composables/useToast'
import { useGameStore, useTagStore } from '@/stores'
import { TagStatus, tagStatusLabel } from '@/types'

const route = useRoute()
const router = useRouter()
const gameStore = useGameStore()
const tagStore = useTagStore()

const gameId = computed(() => route.params.id as string)
const tagId = computed(() => route.params.tagId as string)
const { toast } = useToast()
const isResolving = ref(false)

useGameSignalR(gameId.value)

onMounted(async () => {
  await Promise.all([
    gameStore.loadGame(gameId.value),
    tagStore.loadPendingTag(gameId.value),
    gameStore.loadGamePlayers(gameId.value).catch(() => undefined),
  ])
})

// When navigating to a different tag within the same component instance
// (Vue Router reuses the route component when only params change),
// reset the resolving state and reload data for the new tag.
watch(tagId, async () => {
  isResolving.value = false
  await Promise.all([
    gameStore.loadGame(gameId.value),
    tagStore.loadPendingTag(gameId.value),
    gameStore.loadGamePlayers(gameId.value).catch(() => undefined),
  ])
})

const currentTag = computed(() => {
  if (tagStore.pendingTag && tagStore.pendingTag.id === tagId.value) {
    return tagStore.pendingTag
  }
  return null
})

const hunterName = computed(() => {
  if (!currentTag.value) return ''
  return (
    gameStore.gamePlayers.find((p) => p.playerId === currentTag.value!.hunterId)
      ?.displayName ?? currentTag.value.hunterId
  )
})

const targetName = computed(() => {
  if (!currentTag.value) return ''
  return (
    gameStore.gamePlayers.find((p) => p.playerId === currentTag.value!.targetId)
      ?.displayName ?? currentTag.value.targetId
  )
})

async function onConfirm() {
  isResolving.value = true
  try {
    await tagStore.confirmTag(gameId.value, tagId.value)
    await navigateAfterResolution()
  } catch (err) {
    isResolving.value = false
    if (err instanceof Error) {
      toast(err.message, 'error')
    }
  }
}

async function onDeny() {
  isResolving.value = true
  try {
    await tagStore.denyTag(gameId.value, tagId.value)
    await navigateAfterResolution()
  } catch (err) {
    isResolving.value = false
    if (err instanceof Error) {
      toast(err.message, 'error')
    }
  }
}

async function onVoid() {
  isResolving.value = true
  try {
    await tagStore.voidTag(gameId.value, tagId.value)
    await navigateAfterResolution()
  } catch (err) {
    isResolving.value = false
    if (err instanceof Error) {
      toast(err.message, 'error')
    }
  }
}

async function navigateAfterResolution() {
  const next = tagStore.dequeuePendingTag()
  if (next) {
    await router.push(`/games/${next.gameId}/tag/${next.tag.id}`)
    return
  }

  // Queue is empty — check the API for more pending tags
  // (handles page-reload case where in-memory queue was lost)
  try {
    const pendingTag = await tagStore.loadPendingTag(gameId.value)
    if (pendingTag && pendingTag.status === TagStatus.Pending) {
      await router.push(`/games/${gameId.value}/tag/${pendingTag.id}`)
      return
    }
  } catch {
    // Ignore — fall through to leaderboard
  }

  await router.push(`/games/${gameId.value}/leaderboard`)
}

const isResolved = computed(() => {
  if (!currentTag.value) return false
  return currentTag.value.status !== TagStatus.Pending
})
</script>

<template>
  <section class="page-section">
    <LoadingSpinner v-if="isResolving" />

    <template v-else>
      <BackButton
        :label="$t('common.back')"
        @click="router.push(`/games/${gameId}/leaderboard`)"
      />
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
          <p><strong>{{ $t('tagConfirm.hunter') }}</strong> {{ hunterName }}</p>
          <p><strong>{{ $t('tagConfirm.target') }}</strong> {{ targetName }}</p>
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

      </div>

      <LoadingSpinner v-else-if="tagStore.isLoading" />
      <div
        v-else
        class="empty"
      >
        <p>{{ $t('tagConfirm.notFound') }}</p>
        <Button @click="router.push(`/games/${gameId}/leaderboard`)">
          {{ $t('tagConfirm.backToLeaderboard') }}
        </Button>
      </div>
    </template>
  </section>
</template>

<style scoped>
.tag-status {
  color: var(--text-muted);
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
  background: var(--warning-bg);
  color: var(--warning-text);
}

.status--1 {
  background: var(--success-bg);
  color: var(--success-text);
}

.status--2 {
  background: var(--danger-bg);
  color: var(--danger-text);
}

.status--3 {
  background: var(--surface-muted);
  color: var(--text-muted);
}

.tag-card {
  background: var(--surface);
  border: 1px solid var(--border);
  border-radius: 1rem;
  display: grid;
  gap: 0.75rem;
  margin-bottom: 1.5rem;
  padding: 1.25rem;
}

.tag-card p {
  color: var(--text-secondary);
  margin: 0;
}

.tag-actions {
  display: grid;
  gap: 0.75rem;
  margin-bottom: 1rem;
}

.empty {
  color: var(--text-muted);
  padding: 2rem 0;
  text-align: center;
}
</style>
