<script setup lang="ts">
import { GameStatus, gameStatusLabel, type GameDto } from '@/types'

defineProps<{
  game: GameDto
}>()
const emit = defineEmits<{
  click: [gameId: string]
}>()
</script>

<template>
  <button
    type="button"
    class="game-card"
    @click="emit('click', game.id)"
  >
    <span class="game-card__header">
      <h3 class="game-card__name">
        {{ game.name }}
      </h3>
      <span
        class="game-card__status"
        :class="{
          'status--not-started': game.status === GameStatus.NotStarted,
          'status--active': game.status === GameStatus.Active,
          'status--ended': game.status === GameStatus.Ended,
        }"
      >
        {{ gameStatusLabel(game.status) }}
      </span>
    </span>
    <span class="game-card__details">
      <span class="game-card__players">
        {{ game.playerCount }} / {{ game.maxPlayers }} {{ $t('common.players') }}
      </span>
      <span class="game-card__points">
        {{ game.basePointsPerTag }} {{ $t('common.ptsPerTag') }}
      </span>
    </span>
  </button>
</template>

<style scoped>
.game-card {
  background: white;
  border: 1px solid #e2e8f0;
  border-radius: 1rem;
  cursor: pointer;
  display: grid;
  gap: 0.75rem;
  padding: 1.25rem;
  text-align: left;
  transition:
    transform 0.15s ease,
    box-shadow 0.15s ease;
  width: 100%;
}

.game-card:hover {
  box-shadow: 0 8px 24px rgba(15, 23, 42, 0.08);
  transform: translateY(-2px);
}

.game-card:focus-visible {
  outline: 3px solid #fbbf24;
  outline-offset: 2px;
}

.game-card__header {
  align-items: flex-start;
  display: flex;
  justify-content: space-between;
  gap: 0.75rem;
}

.game-card__name {
  font-size: 1.125rem;
  font-weight: 700;
  margin: 0;
}

.game-card__status {
  border-radius: 9999px;
  font-size: 0.75rem;
  font-weight: 700;
  padding: 0.25rem 0.75rem;
  text-transform: uppercase;
  white-space: nowrap;
}

.status--not-started {
  background: #fef3c7;
  color: #92400e;
}

.status--active {
  background: #dcfce7;
  color: #166534;
}

.status--ended {
  background: #f1f5f9;
  color: #475569;
}

.game-card__details {
  color: #64748b;
  display: flex;
  font-size: 0.875rem;
  gap: 1rem;
}

.game-card__players,
.game-card__points {
  margin: 0;
}
</style>
