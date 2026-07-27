<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'

import Button from '@/components/Button.vue'
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
    <p class="eyebrow">
      Live standings
    </p>
    <h1>Leaderboard</h1>
    <p>Scores update in real time as tags are confirmed.</p>

    <div class="leaderboard-wrapper">
      <LeaderboardTable
        v-if="leaderboardStore.entries.length > 0"
        :entries="leaderboardStore.entries"
      />
      <div
        v-else-if="leaderboardStore.isLoading"
        class="loading"
      >
        Loading leaderboard...
      </div>
      <div
        v-else
        class="empty"
      >
        <p>No scores yet.</p>
      </div>
    </div>

    <div class="leaderboard-actions">
      <Button
        v-if="gameStore.currentGame?.status === 1"
        variant="secondary"
        full-width
        @click="router.push(`/games/${gameId}/assignment`)"
      >
        My assignment
      </Button>
      <Button
        variant="secondary"
        full-width
        @click="router.push(`/games/${gameId}`)"
      >
        Back to game
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
  color: #64748b;
  padding: 2rem 0;
  text-align: center;
}
</style>
