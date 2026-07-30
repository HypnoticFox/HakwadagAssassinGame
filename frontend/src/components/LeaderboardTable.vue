<script setup lang="ts">
import type { LeaderboardEntryDto } from '@/types'

defineProps<{
  entries: LeaderboardEntryDto[]
}>()
</script>

<template>
  <div class="leaderboard-table-wrapper">
    <table class="leaderboard-table">
      <thead>
        <tr>
          <th scope="col">
            {{ $t('leaderboard.table.rank') }}
          </th>
          <th scope="col">
            {{ $t('leaderboard.table.player') }}
          </th>
          <th
            scope="col"
            class="numeric"
          >
            {{ $t('leaderboard.table.tags') }}
          </th>
          <th
            scope="col"
            class="numeric"
          >
            {{ $t('leaderboard.table.score') }}
          </th>
        </tr>
      </thead>
      <tbody>
        <tr
          v-for="(entry, index) in entries"
          :key="entry.player.id"
          :class="{ 'leader-row': index === 0 }"
        >
          <td class="rank">
            <span class="rank-badge">{{ index + 1 }}</span>
          </td>
          <td class="player-cell">
            <div class="player-avatar">
              <img
                v-if="entry.player.avatarUrl"
                :src="entry.player.avatarUrl"
                :alt="entry.player.displayName"
              >
              <span v-else>{{ entry.player.displayName.charAt(0).toUpperCase() }}</span>
            </div>
            <span class="player-name">{{ entry.player.displayName }}</span>
          </td>
          <td class="numeric">
            {{ entry.tags }}
          </td>
          <td class="numeric score">
            {{ entry.score }}
          </td>
        </tr>
      </tbody>
    </table>
  </div>
</template>

<style scoped>
.leaderboard-table-wrapper {
  background: var(--surface);
  border: 1px solid var(--border);
  border-radius: 1rem;
  overflow: hidden;
}

.leaderboard-table {
  border-collapse: collapse;
  width: 100%;
}

.leaderboard-table th,
.leaderboard-table td {
  padding: 0.875rem 1rem;
  text-align: left;
}

.leaderboard-table th {
  background: var(--background);
  color: var(--text-muted);
  font-size: 0.75rem;
  font-weight: 700;
  text-transform: uppercase;
}

.leaderboard-table td {
  border-top: 1px solid var(--border);
}

.leader-row {
  background: var(--primary-light);
}

.numeric {
  text-align: right;
}

.rank {
  width: 3rem;
}

.rank-badge {
  align-items: center;
  background: var(--border);
  border-radius: 50%;
  color: var(--text-muted);
  display: inline-flex;
  font-size: 0.75rem;
  font-weight: 700;
  height: 1.75rem;
  justify-content: center;
  width: 1.75rem;
}

.leader-row .rank-badge {
  background: var(--primary);
  color: var(--text-inverse);
}

.player-cell {
  align-items: center;
  display: flex;
  gap: 0.75rem;
}

.player-avatar {
  align-items: center;
  background: var(--primary);
  border-radius: 50%;
  color: var(--text-inverse);
  display: flex;
  flex-shrink: 0;
  font-size: 0.75rem;
  font-weight: 700;
  height: 2rem;
  justify-content: center;
  overflow: hidden;
  width: 2rem;
}

.player-avatar img {
  height: 100%;
  object-fit: cover;
  width: 100%;
}

.player-name {
  font-weight: 600;
}

.score {
  color: var(--primary);
  font-weight: 700;
}
</style>
