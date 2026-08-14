<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'

import Button from '@/components/Button.vue'
import BackButton from '@/components/BackButton.vue'
import LeaderboardTable from '@/components/LeaderboardTable.vue'
import { useGameSignalR } from '@/composables/useSignalR'
import { useGameStore, useLeaderboardStore } from '@/stores'

const route = useRoute()
const router = useRouter()
const gameStore = useGameStore()
const leaderboardStore = useLeaderboardStore()

const gameId = computed(() => route.params.id as string)

useGameSignalR(gameId.value)

onMounted(async () => {
  await gameStore.loadGame(gameId.value)
  await leaderboardStore.loadLeaderboard(gameId.value)
})
</script>

<template>
  <section class="page-section">
    <BackButton
      :label="$t('common.back')"
      @click="router.push(`/games/${gameId}`)"
    />
    <p class="eyebrow">
      {{ $t('leaderboard.eyebrow') }}
    </p>
    <h1>{{ $t('leaderboard.title') }}</h1>
    <p>{{ $t('leaderboard.subtitle') }}</p>

    <div class="leaderboard-wrapper">
      <LeaderboardTable
        v-if="leaderboardStore.entries.length > 0"
        :entries="leaderboardStore.entries"
      />
      <div
        v-else-if="leaderboardStore.isLoading"
        class="loading"
      >
        {{ $t('leaderboard.loading') }}
      </div>
      <div
        v-else
        class="empty"
      >
        <p>{{ $t('leaderboard.noScores') }}</p>
      </div>
    </div>

    <div class="leaderboard-actions">
      <Button
        v-if="gameStore.currentGame?.status === 1"
        variant="secondary"
        full-width
        @click="router.push(`/games/${gameId}/assignment`)"
      >
        {{ $t('leaderboard.myAssignment') }}
      </Button>
      <Button
        variant="secondary"
        full-width
        @click="router.push(`/games/${gameId}`)"
      >
        {{ $t('leaderboard.backToGame') }}
      </Button>
    </div>
  </section>
</template>

<style scoped>
.leaderboard-wrapper {
  margin-top: 1.5rem;
  margin-bottom: 1.5rem;
}

.leaderboard-actions {
  display: grid;
  gap: 0.75rem;
}

.loading,
.empty {
  color: var(--text-muted);
  padding: 2rem 0;
  text-align: center;
}
</style>
