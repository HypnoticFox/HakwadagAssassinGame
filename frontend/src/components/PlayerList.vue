<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import type { PlayerDto } from '@/types'

const { t } = useI18n()

defineProps<{
  players: PlayerDto[]
  emptyText?: string
}>()
</script>

<template>
  <ul
    v-if="players.length > 0"
    class="player-list"
  >
    <li
      v-for="player in players"
      :key="player.id"
      class="player-item"
    >
      <div class="player-avatar">
        <img
          v-if="player.avatarUrl"
          :src="player.avatarUrl"
          :alt="player.displayName"
        >
        <span v-else>{{ player.displayName.charAt(0).toUpperCase() }}</span>
      </div>
      <div class="player-info">
        <p class="player-name">
          {{ player.displayName }}
        </p>
        <p class="player-email">
          {{ player.email }}
        </p>
      </div>
    </li>
  </ul>
  <p
    v-else
    class="player-list-empty"
  >
    {{ emptyText || t('playerList.empty') }}
  </p>
</template>

<style scoped>
.player-list {
  display: grid;
  gap: 0.5rem;
  list-style: none;
  margin: 0;
  padding: 0;
}

.player-item {
  align-items: center;
  background: white;
  border: 1px solid #e2e8f0;
  border-radius: 0.75rem;
  display: flex;
  gap: 0.875rem;
  padding: 0.75rem;
}

.player-avatar {
  align-items: center;
  background: #1d4ed8;
  border-radius: 50%;
  color: white;
  display: flex;
  flex-shrink: 0;
  font-size: 0.875rem;
  font-weight: 700;
  height: 2.5rem;
  justify-content: center;
  overflow: hidden;
  width: 2.5rem;
}

.player-avatar img {
  height: 100%;
  object-fit: cover;
  width: 100%;
}

.player-info {
  min-width: 0;
}

.player-name {
  font-weight: 600;
  margin: 0;
}

.player-email {
  color: #64748b;
  font-size: 0.875rem;
  margin: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.player-list-empty {
  color: #64748b;
  margin: 0;
}
</style>
