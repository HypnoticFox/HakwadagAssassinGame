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
            Rank
          </th>
          <th scope="col">
            Player
          </th>
          <th
            scope="col"
            class="numeric"
          >
            Tags
          </th>
          <th
            scope="col"
            class="numeric"
          >
            Score
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
  background: white;
  border: 1px solid #e2e8f0;
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
  background: #f8fafc;
  color: #475569;
  font-size: 0.75rem;
  font-weight: 700;
  text-transform: uppercase;
}

.leaderboard-table td {
  border-top: 1px solid #e2e8f0;
}

.leader-row {
  background: #eff6ff;
}

.numeric {
  text-align: right;
}

.rank {
  width: 3rem;
}

.rank-badge {
  align-items: center;
  background: #e2e8f0;
  border-radius: 50%;
  color: #475569;
  display: inline-flex;
  font-size: 0.75rem;
  font-weight: 700;
  height: 1.75rem;
  justify-content: center;
  width: 1.75rem;
}

.leader-row .rank-badge {
  background: #1d4ed8;
  color: white;
}

.player-cell {
  align-items: center;
  display: flex;
  gap: 0.75rem;
}

.player-avatar {
  align-items: center;
  background: #1d4ed8;
  border-radius: 50%;
  color: white;
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
  color: #1d4ed8;
  font-weight: 700;
}
</style>
