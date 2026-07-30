<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref } from 'vue'

import { api } from '@/api/client'
import { GameStatus, TagStatus, gameRoleLabel, gameStatusLabel, tagStatusLabel } from '@/types'
import type { DevAssignment, DevGame, DevPlayer, DevTag, GameRole } from '@/types'

interface GameDetails {
  players: DevPlayer[]
  assignments: DevAssignment[]
  tags: DevTag[]
}

const games = ref<DevGame[]>([])
const loading = ref(false)
const error = ref<string | null>(null)
const expandedGameId = ref<string | null>(null)
const details = ref<Record<string, GameDetails>>({})
const detailsLoading = ref<Record<string, boolean>>({})
const detailsError = ref<Record<string, string | null>>({})
const autoRefresh = ref(false)
const autoRefreshInterval = ref<number | null>(null)

const sortedGames = computed(() => {
  return [...games.value].sort(
    (a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime(),
  )
})

function statusClass(status: GameStatus) {
  switch (status) {
    case GameStatus.NotStarted:
      return 'status--not-started'
    case GameStatus.Active:
      return 'status--active'
    case GameStatus.Ended:
      return 'status--ended'
    default:
      return ''
  }
}

function tagStatusClass(status: TagStatus) {
  switch (status) {
    case TagStatus.Pending:
      return 'tag-status--pending'
    case TagStatus.Confirmed:
      return 'tag-status--confirmed'
    case TagStatus.Denied:
      return 'tag-status--denied'
    case TagStatus.Voided:
      return 'tag-status--voided'
    default:
      return ''
  }
}

function formatDate(iso: string) {
  return new Date(iso).toLocaleString()
}

function formatShortDate(iso: string) {
  return new Date(iso).toLocaleDateString(undefined, {
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}

async function loadGames() {
  loading.value = true
  error.value = null
  try {
    games.value = await api.devGetGames()
    if (expandedGameId.value && !games.value.find((g) => g.id === expandedGameId.value)) {
      expandedGameId.value = null
    }
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Failed to load games'
  } finally {
    loading.value = false
  }
}

async function loadGameDetails(gameId: string) {
  if (detailsLoading.value[gameId]) return
  detailsLoading.value[gameId] = true
  detailsError.value[gameId] = null
  try {
    const [players, assignments, tags] = await Promise.all([
      api.devGetGamePlayers(gameId),
      api.devGetGameAssignments(gameId),
      api.devGetGameTags(gameId),
    ])
    details.value[gameId] = { players, assignments, tags }
  } catch (err) {
    detailsError.value[gameId] = err instanceof Error ? err.message : 'Failed to load game details'
  } finally {
    detailsLoading.value[gameId] = false
  }
}

function toggleGame(gameId: string) {
  if (expandedGameId.value === gameId) {
    expandedGameId.value = null
  } else {
    expandedGameId.value = gameId
    void loadGameDetails(gameId)
  }
}

function toggleAutoRefresh() {
  autoRefresh.value = !autoRefresh.value
  if (autoRefresh.value && autoRefreshInterval.value === null) {
    autoRefreshInterval.value = window.setInterval(() => {
      void loadGames()
      if (expandedGameId.value) {
        void loadGameDetails(expandedGameId.value)
      }
    }, 5000)
  } else if (!autoRefresh.value && autoRefreshInterval.value !== null) {
    window.clearInterval(autoRefreshInterval.value)
    autoRefreshInterval.value = null
  }
}

onMounted(() => {
  void loadGames()
})

onUnmounted(() => {
  if (autoRefreshInterval.value !== null) {
    window.clearInterval(autoRefreshInterval.value)
    autoRefreshInterval.value = null
  }
})
</script>

<template>
  <section class="page-section dev-dashboard">
    <header class="dev-dashboard__header">
      <div>
        <p class="eyebrow">
          Dev Tools
        </p>
        <h1>Games Dashboard</h1>
      </div>
      <div class="dev-dashboard__actions">
        <button
          type="button"
          class="dev-dashboard__refresh"
          :disabled="loading"
          @click="loadGames"
        >
          {{ loading ? 'Loading...' : 'Refresh' }}
        </button>
        <button
          type="button"
          class="dev-dashboard__refresh"
          :class="{ 'dev-dashboard__refresh--active': autoRefresh }"
          @click="toggleAutoRefresh"
        >
          {{ autoRefresh ? 'Auto-refresh: On' : 'Auto-refresh: Off' }}
        </button>
      </div>
    </header>

    <p
      v-if="error"
      class="dev-dashboard__error"
      role="alert"
    >
      {{ error }}
    </p>

    <div class="dev-dashboard__list">
      <div
        v-if="!games.length && !loading"
        class="dev-dashboard__empty"
      >
        No games found.
      </div>

      <div
        v-for="game in sortedGames"
        :key="game.id"
        class="dev-dashboard__game"
        :class="{ 'dev-dashboard__game--expanded': expandedGameId === game.id }"
      >
        <button
          type="button"
          class="dev-dashboard__game-header"
          @click="toggleGame(game.id)"
        >
          <div class="dev-dashboard__game-main">
            <span class="dev-dashboard__game-name">{{ game.name }}</span>
            <span
              class="dev-dashboard__game-status"
              :class="statusClass(game.status)"
            >
              {{ gameStatusLabel(game.status) }}
            </span>
          </div>
          <div class="dev-dashboard__game-meta">
            <span>{{ game.playerCount }} players</span>
            <span class="dev-dashboard__game-date">{{ formatShortDate(game.createdAt) }}</span>
            <span class="dev-dashboard__chevron">{{ expandedGameId === game.id ? '▾' : '▸' }}</span>
          </div>
        </button>

        <div
          v-if="expandedGameId === game.id"
          class="dev-dashboard__game-body"
        >
          <p
            v-if="detailsLoading[game.id]"
            class="dev-dashboard__loading"
          >
            Loading details...
          </p>
          <p
            v-else-if="detailsError[game.id]"
            class="dev-dashboard__error"
            role="alert"
          >
            {{ detailsError[game.id] }}
          </p>

          <template v-if="details[game.id] && !detailsLoading[game.id]">
            <div class="dev-dashboard__detail">
              <h3 class="dev-dashboard__detail-title">
                Players
              </h3>
              <div class="dev-dashboard__table-wrap">
                <table class="dev-dashboard__table">
                  <thead>
                    <tr>
                      <th>Name</th>
                      <th>Email</th>
                      <th>Role</th>
                      <th>Score</th>
                      <th>Active</th>
                      <th>Participating</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr
                      v-for="player in details[game.id].players"
                      :key="player.playerId"
                    >
                      <td>{{ player.displayName }}</td>
                      <td>{{ player.email }}</td>
                      <td>{{ gameRoleLabel(player.role as GameRole) }}</td>
                      <td>{{ player.score }}</td>
                      <td>{{ player.isActive ? 'Yes' : 'No' }}</td>
                      <td>{{ player.isParticipating ? 'Yes' : 'No' }}</td>
                    </tr>
                    <tr v-if="!details[game.id].players.length">
                      <td
                        colspan="6"
                        class="dev-dashboard__cell-empty"
                      >
                        No players
                      </td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </div>

            <div class="dev-dashboard__detail">
              <h3 class="dev-dashboard__detail-title">
                Assignments
              </h3>
              <div class="dev-dashboard__table-wrap">
                <table class="dev-dashboard__table">
                  <thead>
                    <tr>
                      <th>Hunter</th>
                      <th>Target</th>
                      <th>Status</th>
                      <th>Assigned</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr
                      v-for="assignment in details[game.id].assignments"
                      :key="assignment.id"
                    >
                      <td>{{ assignment.hunterName }}</td>
                      <td>{{ assignment.targetName }}</td>
                      <td>{{ assignment.status }}</td>
                      <td>{{ formatDate(assignment.assignedAt) }}</td>
                    </tr>
                    <tr v-if="!details[game.id].assignments.length">
                      <td
                        colspan="4"
                        class="dev-dashboard__cell-empty"
                      >
                        No assignments
                      </td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </div>

            <div class="dev-dashboard__detail">
              <h3 class="dev-dashboard__detail-title">
                Tags
              </h3>
              <div class="dev-dashboard__table-wrap">
                <table class="dev-dashboard__table">
                  <thead>
                    <tr>
                      <th>Hunter</th>
                      <th>Target</th>
                      <th>Status</th>
                      <th>Submitted</th>
                      <th>Resolved</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr
                      v-for="tag in details[game.id].tags"
                      :key="tag.id"
                    >
                      <td>{{ tag.hunterName }}</td>
                      <td>{{ tag.targetName }}</td>
                      <td>
                        <span
                          class="dev-dashboard__tag-status"
                          :class="tagStatusClass(tag.status)"
                        >
                          {{ tagStatusLabel(tag.status) }}
                        </span>
                      </td>
                      <td>{{ formatDate(tag.submittedAt) }}</td>
                      <td>{{ tag.resolvedAt ? formatDate(tag.resolvedAt) : '—' }}</td>
                    </tr>
                    <tr v-if="!details[game.id].tags.length">
                      <td
                        colspan="5"
                        class="dev-dashboard__cell-empty"
                      >
                        No tags
                      </td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </div>
          </template>
        </div>
      </div>
    </div>
  </section>
</template>

<style scoped>
.dev-dashboard__header {
  align-items: flex-start;
  display: flex;
  flex-direction: column;
  gap: 1rem;
  margin-bottom: 0.5rem;
}

.dev-dashboard__header h1 {
  margin: 0;
}

.dev-dashboard__actions {
  display: flex;
  gap: 0.75rem;
  flex-wrap: wrap;
}

.dev-dashboard__refresh {
  background: white;
  border: 1px solid var(--border);
  border-radius: 0.75rem;
  color: var(--text);
  cursor: pointer;
  font: inherit;
  font-size: 0.875rem;
  font-weight: 600;
  min-height: 2.5rem;
  padding: 0.5rem 1rem;
  transition: background-color 0.15s ease;
}

.dev-dashboard__refresh:hover:not(:disabled) {
  background: var(--primary-light);
}

.dev-dashboard__refresh:disabled {
  cursor: not-allowed;
  opacity: 0.6;
}

.dev-dashboard__refresh--active {
  background: var(--primary);
  border-color: var(--primary);
  color: white;
}

.dev-dashboard__error {
  background: #fef2f2;
  border-radius: 0.75rem;
  color: #991b1b;
  font-size: 0.875rem;
  margin: 0;
  padding: 0.75rem 1rem;
}

.dev-dashboard__loading {
  color: var(--text-muted);
  font-size: 0.875rem;
  margin: 0.5rem 0;
}

.dev-dashboard__list {
  display: grid;
  gap: 0.75rem;
}

.dev-dashboard__empty {
  background: white;
  border: 1px solid var(--border);
  border-radius: 1rem;
  color: var(--text-muted);
  padding: 1.5rem;
  text-align: center;
}

.dev-dashboard__game {
  background: white;
  border: 1px solid var(--border);
  border-radius: 1rem;
  overflow: hidden;
}

.dev-dashboard__game-header {
  align-items: center;
  background: transparent;
  border: 0;
  color: inherit;
  cursor: pointer;
  display: flex;
  font: inherit;
  gap: 1rem;
  justify-content: space-between;
  padding: 1rem;
  text-align: left;
  width: 100%;
}

.dev-dashboard__game-header:hover {
  background: var(--background);
}

.dev-dashboard__game-main {
  align-items: center;
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
  min-width: 0;
}

.dev-dashboard__game-name {
  font-family: 'Space Grotesk', ui-sans-serif, sans-serif;
  font-size: 1.125rem;
  font-weight: 700;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.dev-dashboard__game-status {
  border-radius: 9999px;
  font-size: 0.75rem;
  font-weight: 700;
  padding: 0.25rem 0.625rem;
  text-transform: uppercase;
}

.status--not-started {
  background: #e2e8f0;
  color: #475569;
}

.status--active {
  background: #dcfce7;
  color: #166534;
}

.status--ended {
  background: #fee2e2;
  color: #991b1b;
}

.dev-dashboard__game-meta {
  align-items: center;
  color: var(--text-muted);
  display: flex;
  flex-shrink: 0;
  font-size: 0.875rem;
  gap: 0.75rem;
}

.dev-dashboard__game-date {
  display: none;
}

.dev-dashboard__chevron {
  color: var(--text-muted);
  font-size: 1rem;
  line-height: 1;
  width: 1rem;
}

.dev-dashboard__game-body {
  border-top: 1px solid var(--border);
  padding: 1rem;
}

.dev-dashboard__detail + .dev-dashboard__detail {
  margin-top: 1.5rem;
}

.dev-dashboard__detail-title {
  font-size: 1rem;
  margin: 0 0 0.75rem;
}

.dev-dashboard__table-wrap {
  overflow-x: auto;
  -webkit-overflow-scrolling: touch;
}

.dev-dashboard__table {
  border-collapse: collapse;
  font-size: 0.875rem;
  min-width: 100%;
  width: 100%;
}

.dev-dashboard__table th,
.dev-dashboard__table td {
  border-bottom: 1px solid var(--border);
  padding: 0.625rem 0.75rem;
  text-align: left;
  white-space: nowrap;
}

.dev-dashboard__table th {
  background: var(--background);
  color: var(--text-muted);
  font-size: 0.75rem;
  font-weight: 700;
  letter-spacing: 0.03em;
  text-transform: uppercase;
}

.dev-dashboard__table tr:last-child td {
  border-bottom: 0;
}

.dev-dashboard__cell-empty {
  color: var(--text-muted);
  font-style: italic;
  text-align: center;
}

.dev-dashboard__tag-status {
  border-radius: 0.375rem;
  font-size: 0.75rem;
  font-weight: 700;
  padding: 0.25rem 0.5rem;
  text-transform: uppercase;
}

.tag-status--pending {
  background: #fef3c7;
  color: #92400e;
}

.tag-status--confirmed {
  background: #dcfce7;
  color: #166534;
}

.tag-status--denied {
  background: #fee2e2;
  color: #991b1b;
}

.tag-status--voided {
  background: #e2e8f0;
  color: #475569;
}

@media (min-width: 640px) {
  .dev-dashboard__header {
    align-items: center;
    flex-direction: row;
    justify-content: space-between;
  }

  .dev-dashboard__game-date {
    display: inline;
  }
}
</style>
