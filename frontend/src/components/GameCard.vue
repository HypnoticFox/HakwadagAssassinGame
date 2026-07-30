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
  background: var(--surface);
  border: 1px solid var(--border);
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
  box-shadow: var(--shadow);
  transform: translateY(-2px);
}

.game-card:focus-visible {
  outline: 3px solid var(--focus);
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
  background: var(--warning-bg);
  color: var(--warning-text);
}

.status--active {
  background: var(--success-bg);
  color: var(--success-text);
}

.status--ended {
  background: var(--surface-muted);
  color: var(--text-muted);
}

.game-card__details {
  color: var(--text-muted);
  display: flex;
  font-size: 0.875rem;
  gap: 1rem;
}

.game-card__players,
.game-card__points {
  margin: 0;
}
</style>
