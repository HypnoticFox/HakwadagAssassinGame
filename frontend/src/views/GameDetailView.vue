<script setup lang="ts">
import { onMounted, ref, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'

import Button from '@/components/Button.vue'
import Input from '@/components/Input.vue'
import { Copy, Crosshair, Link, Trophy } from '@lucide/vue'
import { useGameSignalR } from '@/composables/useSignalR'
import { useToast } from '@/composables/useToast'
import { useAuthStore, useGameStore, useLeaderboardStore } from '@/stores'
import {
  GameStatus,
  canEndGame,
  canStartGame,
  gameRoleLabel,
  gameStatusLabel,
  isGameAdmin,
} from '@/types'

const route = useRoute()
const router = useRouter()
const { t } = useI18n()
const gameStore = useGameStore()
const authStore = useAuthStore()
const leaderboardStore = useLeaderboardStore()

const gameId = computed(() => route.params.id as string)
const copiedCode = ref(false)
const copiedLink = ref(false)
const { toast } = useToast()
const newDurationHours = ref('24')

useGameSignalR(gameId.value)

onMounted(async () => {
  await Promise.all([
    gameStore.loadGame(gameId.value),
    leaderboardStore.loadLeaderboard(gameId.value).catch(() => undefined),
  ])
})

async function copyInviteCode() {
  if (!gameStore.currentGame) return
  try {
    await navigator.clipboard.writeText(gameStore.currentGame.inviteCode)
    copiedCode.value = true
    setTimeout(() => (copiedCode.value = false), 2000)
  } catch {
    // ignore
  }
}

async function copyInviteLink() {
  if (!gameStore.currentGame) return
  const inviteLink = `${window.location.origin}/invite/${gameStore.currentGame.inviteCode}`
  try {
    await navigator.clipboard.writeText(inviteLink)
    copiedLink.value = true
    setTimeout(() => (copiedLink.value = false), 2000)
  } catch {
    // ignore
  }
}

async function onStart() {
  try {
    await gameStore.startGame(gameId.value)
  } catch (err) {
    if (err instanceof Error) {
      toast(err.message, 'error')
    }
  }
}

async function onEnd() {
  try {
    await gameStore.endGame(gameId.value)
  } catch (err) {
    if (err instanceof Error) {
      toast(err.message, 'error')
    }
  }
}

async function onUpdateDuration() {
  if (!newDurationHours.value) return
  try {
    await gameStore.updateDuration(gameId.value, Number(newDurationHours.value))
  } catch (err) {
    if (err instanceof Error) {
      toast(err.message, 'error')
    }
  }
}

async function onExtend(minutes: number) {
  try {
    await gameStore.extendDuration(gameId.value, minutes)
  } catch (err) {
    if (err instanceof Error) {
      toast(err.message, 'error')
    }
  }
}

async function onLeave() {
  const confirmKey =
    gameStore.currentGame?.status === GameStatus.Active
      ? 'gameDetail.leaveConfirmActive'
      : 'gameDetail.leaveConfirm'
  if (!confirm(t(confirmKey))) return
  try {
    await gameStore.leaveGame(gameId.value)
    if (gameStore.currentGame?.status !== GameStatus.Active) {
      await router.push('/')
    }
  } catch (err) {
    if (err instanceof Error) {
      toast(err.message, 'error')
    }
  }
}

async function onRejoin() {
  try {
    await gameStore.rejoinGame(gameId.value)
  } catch (err) {
    if (err instanceof Error) {
      toast(err.message, 'error')
    }
  }
}

const myScore = computed(() => {
  const playerId = authStore.player?.id
  if (!playerId || !leaderboardStore.entries.length) return 0
  const entry = leaderboardStore.entries.find((e) => e.player.id === playerId)
  return entry?.score ?? 0
})

const myRank = computed(() => {
  const playerId = authStore.player?.id
  if (!playerId || !leaderboardStore.entries.length) return null
  const index = leaderboardStore.entries.findIndex((e) => e.player.id === playerId)
  return index >= 0 ? index + 1 : null
})

const formattedScheduledEndAt = computed(() => {
  if (!gameStore.currentGame?.scheduledEndAt) return t('gameDetail.noScheduledEnd')
  return new Date(gameStore.currentGame.scheduledEndAt).toLocaleString()
})
</script>

<template>
  <section class="page-section">
    <div v-if="gameStore.currentGame">
      <div class="game-header">
        <div>
          <p class="eyebrow">
            {{ gameStatusLabel(gameStore.currentGame.status) }}
          </p>
          <h1>{{ gameStore.currentGame.name }}</h1>
          <p class="game-meta">
            {{ gameRoleLabel(gameStore.currentGame.myRole) }} ·
            {{ gameStore.currentGame.participantCount }} {{ $t('common.players') }}
          </p>
        </div>
        <div class="game-actions">
          <Button
            v-if="canStartGame(gameStore.currentGame.myRole, gameStore.currentGame.status)"
            size="large"
            @click="onStart"
          >
            {{ $t('gameDetail.startGame') }}
          </Button>
          <Button
            v-if="canEndGame(gameStore.currentGame.myRole, gameStore.currentGame.status)"
            variant="danger"
            @click="onEnd"
          >
            {{ $t('gameDetail.endGame') }}
          </Button>
        </div>
      </div>

      <div
        v-if="
          gameStore.currentGame.status === GameStatus.Active &&
            !gameStore.currentGame.isParticipating
        "
        class="left-banner"
      >
        <p>{{ $t('gameDetail.youLeftGame') }}</p>
      </div>

      <div
        v-if="
          gameStore.currentGame.status === GameStatus.Active &&
            gameStore.currentGame.isParticipating
        "
        class="hero-section"
      >
        <div class="score-board">
          <div class="score-content">
            <div class="score-header">
              <span class="score-label">{{ $t('gameDetail.yourScore') }}</span>
            </div>
            <div class="score-value-row">
              <div class="score-value">
                {{ myScore }}
              </div>
              <div class="score-unit">
                {{ $t('common.pts') }}
              </div>
            </div>
          </div>
          <div v-if="myRank !== null" class="rank-content">
            <div class="rank-header">
              <span class="rank-label">{{ $t('gameDetail.yourRank') }}</span>
            </div>
            <div class="rank-value-row">
              <div class="rank-value">
                <span class="rank-hash">#</span>{{ myRank }}
              </div>
            </div>
          </div>
        </div>
        <Button
          size="large"
          full-width
          class="assignment-button"
          @click="router.push(`/games/${gameId}/assignment`)"
        >
          <Crosshair
            :size="24"
            aria-hidden="true"
          />
          {{ $t('gameDetail.myAssignment') }}
        </Button>
      </div>

      <div
        v-if="gameStore.currentGame.safeTimeBlocks.length > 0"
        class="safe-times"
      >
        <h2>{{ $t('gameDetail.safeTimes') }}</h2>
        <ul class="safe-time-list">
          <li
            v-for="block in gameStore.currentGame.safeTimeBlocks"
            :key="block.id"
            class="safe-time-item"
          >
            <span>{{ block.startTime }} – {{ block.endTime }}</span>
          </li>
        </ul>
      </div>

      <div class="invite-card">
        <div>
          <p class="invite-label">
            {{ $t('gameDetail.inviteCode') }}
          </p>
          <p class="invite-code">
            {{ gameStore.currentGame.inviteCode }}
          </p>
        </div>
        <div class="invite-actions">
          <Button
            variant="secondary"
            :title="$t('gameDetail.copyInviteCode')"
            @click="copyInviteCode"
          >
            <Copy :size="18" />
            {{ copiedCode ? $t('common.copied') : '' }}
          </Button>
          <Button
            variant="secondary"
            :title="$t('gameDetail.copyInviteLink')"
            @click="copyInviteLink"
          >
            <Link :size="18" />
            {{ copiedLink ? $t('common.copied') : '' }}
          </Button>
        </div>
      </div>

      <div class="detail-grid">
        <div class="detail-card">
          <p class="detail-label">
            {{ $t('gameDetail.pointsPerTag') }}
          </p>
          <p class="detail-value">
            {{ gameStore.currentGame.basePointsPerTag }}
          </p>
        </div>
        <div class="detail-card">
          <p class="detail-label">
            {{ $t('gameDetail.scheduledEnd') }}
          </p>
          <p class="detail-value">
            {{ formattedScheduledEndAt }}
          </p>
        </div>
      </div>

      <div class="action-grid">
        <Button
          variant="secondary"
          size="large"
          full-width
          @click="router.push(`/games/${gameId}/leaderboard`)"
        >
          {{ $t('gameDetail.leaderboard') }}
        </Button>
      </div>

      <div
        v-if="isGameAdmin(gameStore.currentGame.myRole)"
        class="admin-section"
      >
        <div
          v-if="gameStore.currentGame.status === GameStatus.NotStarted"
          class="participation-card"
        >
          <p class="participation-label">
            {{ $t('gameDetail.participationLabel') }}
          </p>
          <label class="toggle-row">
            <span class="toggle-text">{{
              gameStore.currentGame.isParticipating
                ? $t('gameDetail.participating')
                : $t('gameDetail.notParticipating')
            }}</span>
            <div class="toggle-switch">
              <input
                type="checkbox"
                class="toggle-input"
                :checked="gameStore.currentGame.isParticipating"
                @change="
                  gameStore.setParticipation(gameId, ($event.target as HTMLInputElement).checked)
                "
              >
              <span class="toggle-slider" />
            </div>
          </label>
          <p class="min-participants-note">
            {{ $t('gameDetail.minParticipants') }}
          </p>
        </div>

        <div
          v-if="gameStore.currentGame.status === GameStatus.NotStarted"
          class="duration-card"
        >
          <p class="duration-label">
            {{ $t('gameDetail.duration') }}
          </p>
          <div class="duration-edit">
            <Input
              v-model="newDurationHours"
              :label="$t('gameDetail.durationHours')"
              type="number"
              inputmode="numeric"
              min="1"
            />
            <Button
              variant="secondary"
              :loading="gameStore.isLoading"
              @click="onUpdateDuration"
            >
              {{ $t('gameDetail.updateDuration') }}
            </Button>
          </div>
        </div>

        <div
          v-if="gameStore.currentGame.status === GameStatus.Active"
          class="extend-card"
        >
          <p class="extend-label">
            {{ $t('gameDetail.extendGame') }}
          </p>
          <div class="extend-buttons">
            <Button
              variant="secondary"
              :loading="gameStore.isLoading"
              @click="onExtend(5)"
            >
              {{ $t('gameDetail.extend5min') }}
            </Button>
            <Button
              variant="secondary"
              :loading="gameStore.isLoading"
              @click="onExtend(60)"
            >
              {{ $t('gameDetail.extend1hour') }}
            </Button>
            <Button
              variant="secondary"
              :loading="gameStore.isLoading"
              @click="onExtend(1440)"
            >
              {{ $t('gameDetail.extend1day') }}
            </Button>
          </div>
        </div>

        <Button
          variant="secondary"
          full-width
          @click="router.push(`/games/${gameId}/admin`)"
        >
          {{ $t('gameDetail.adminPanel') }}
        </Button>
      </div>

      <!-- Leave/Rejoin button based on participation status -->
      <div v-if="gameStore.currentGame.status === GameStatus.Active">
        <Button
          v-if="gameStore.currentGame.isParticipating"
          variant="ghost"
          @click="onLeave"
        >
          {{ $t('gameDetail.leaveGame') }}
        </Button>
        <Button
          v-else
          variant="secondary"
          size="large"
          full-width
          @click="onRejoin"
        >
          {{ $t('gameDetail.rejoinGame') }}
        </Button>
      </div>
      <Button
        v-else
        variant="ghost"
        @click="onLeave"
      >
        {{ $t('gameDetail.leaveGame') }}
      </Button>
    </div>

    <div
      v-else-if="gameStore.isLoading"
      class="loading"
    >
      {{ $t('gameDetail.loading') }}
    </div>
    <div
      v-else
      class="empty"
    >
      <p>{{ $t('gameDetail.notFound') }}</p>
      <Button @click="router.push('/')">
        {{ $t('common.backHome') }}
      </Button>
    </div>
  </section>
</template>

<style scoped>
.game-header {
  display: grid;
  gap: 1rem;
  margin-bottom: 1.5rem;
}

.game-meta {
  color: var(--text-muted);
  margin: 0.5rem 0 0;
}

.left-banner {
  background: var(--warning-bg);
  border: 1px solid var(--warning);
  border-radius: 0.75rem;
  color: var(--warning-text);
  padding: 0.75rem 1rem;
  margin-bottom: 1.5rem;
  text-align: center;
}

.left-banner p {
  margin: 0;
  font-weight: 500;
}

.game-actions {
  display: flex;
  gap: 0.75rem;
}

.hero-section {
  background: var(--surface);
  border: 1px solid var(--border);
  border-radius: var(--radius-lg);
  display: grid;
  gap: 1.25rem;
  margin-bottom: 1.5rem;
  padding: 1.5rem;
}

.score-board {
  display: flex;
  align-items: flex-start;
  justify-content: center;
  gap: 2rem;
  padding: 0.75rem 0;
}

.score-content {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.5rem;
}

.score-header,
.rank-header {
  color: var(--primary);
  font-size: 0.9375rem;
  font-weight: 700;
  letter-spacing: 0.06em;
  text-transform: uppercase;
}

.score-value-row {
  display: flex;
  align-items: flex-end;
  gap: 0.375rem;
}

.score-value {
  color: var(--text);
  font-size: 3.5rem;
  font-weight: 800;
  line-height: 1;
}

.score-unit {
  color: var(--text-muted);
  font-size: 1rem;
  font-weight: 600;
  margin-bottom: 0.25rem;
}

.rank-content {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.5rem;
}

.rank-value-row {
  display: flex;
  align-items: flex-end;
  gap: 0.375rem;
}

.rank-value {
  color: var(--text);
  font-size: 3.5rem;
  font-weight: 700;
  line-height: 1;
  font-family: 'Roboto Slab', serif;
}

.rank-hash {
  color: var(--text-muted);
  font-size: 2rem;
  font-weight: 600;
  margin-right: 0.25rem;
  align-self: flex-end;
  margin-bottom: 0.375rem;
}

.hero-section .assignment-button {
  font-size: 1.25rem;
  gap: 0.75rem;
  min-height: 4rem;
}

.invite-card {
  align-items: center;
  background: var(--surface);
  border: 1px solid var(--border);
  border-radius: 1rem;
  display: flex;
  justify-content: space-between;
  gap: 1rem;
  padding: 1.25rem;
  margin-bottom: 1.5rem;
}

.invite-label {
  color: var(--text-muted);
  font-size: 0.875rem;
  margin: 0;
}

.invite-code {
  font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
  font-size: 1.5rem;
  font-weight: 700;
  margin: 0.25rem 0 0;
}

.invite-actions {
  align-items: center;
  display: flex;
  gap: 0.5rem;
}

.detail-grid {
  display: grid;
  gap: 0.75rem;
  grid-template-columns: repeat(2, 1fr);
  margin-bottom: 1.5rem;
}

.detail-card {
  background: var(--surface);
  border: 1px solid var(--border);
  border-radius: 1rem;
  padding: 1rem;
}

.detail-label {
  color: var(--text-muted);
  font-size: 0.75rem;
  font-weight: 700;
  margin: 0;
  text-transform: uppercase;
}

.detail-value {
  font-size: 1rem;
  font-weight: 600;
  margin: 0.25rem 0 0;
}

.safe-times {
  margin-bottom: 1.5rem;
}

.safe-times h2 {
  font-size: 1.125rem;
  margin: 0 0 0.75rem;
}

.safe-time-list {
  display: grid;
  gap: 0.5rem;
  list-style: none;
  margin: 0;
  padding: 0;
}

.safe-time-item {
  align-items: center;
  background: var(--surface);
  border: 1px solid var(--border);
  border-radius: 0.75rem;
  display: flex;
  gap: 0.75rem;
  justify-content: space-between;
  padding: 0.75rem 1rem;
}

.action-grid {
  display: grid;
  gap: 0.75rem;
  margin-bottom: 1.5rem;
}

.admin-section {
  margin-bottom: 1.5rem;
}

.participation-card {
  background: var(--surface);
  border: 1px solid var(--border);
  border-radius: 1rem;
  padding: 1rem 1.25rem;
  margin-bottom: 1.5rem;
}

.participation-label {
  color: var(--text-muted);
  font-size: 0.75rem;
  font-weight: 700;
  margin: 0 0 0.5rem;
  text-transform: uppercase;
}

.toggle-row {
  align-items: center;
  cursor: pointer;
  display: flex;
  justify-content: space-between;
  gap: 1rem;
}

.toggle-text {
  color: var(--text-secondary);
  font-size: 0.9375rem;
  font-weight: 500;
}

.toggle-switch {
  flex-shrink: 0;
  height: 1.5rem;
  position: relative;
  width: 2.75rem;
}

.toggle-input {
  height: 0;
  margin: 0;
  opacity: 0;
  position: absolute;
  width: 0;
}

.toggle-slider {
  background: var(--toggle-bg);
  border-radius: 1.5rem;
  bottom: 0;
  cursor: pointer;
  left: 0;
  position: absolute;
  right: 0;
  top: 0;
  transition: background-color 0.2s;
}

.toggle-slider::before {
  background: var(--text-inverse);
  border-radius: 50%;
  content: '';
  height: 1.125rem;
  left: 0.1875rem;
  position: absolute;
  top: 0.1875rem;
  transition: transform 0.2s;
  width: 1.125rem;
}

.toggle-input:checked + .toggle-slider {
  background: var(--toggle-active);
}

.toggle-input:checked + .toggle-slider::before {
  transform: translateX(1.25rem);
}

.toggle-input:focus-visible + .toggle-slider {
  outline: 2px solid var(--primary);
  outline-offset: 2px;
}

.min-participants-note {
  color: var(--text-faint);
  font-size: 0.8125rem;
  margin: 0.5rem 0 0;
}

.duration-card,
.extend-card {
  background: var(--surface);
  border: 1px solid var(--border);
  border-radius: 1rem;
  padding: 1rem 1.25rem;
  margin-bottom: 1.5rem;
}

.duration-label,
.extend-label {
  color: var(--text-muted);
  font-size: 0.75rem;
  font-weight: 700;
  margin: 0 0 0.75rem;
  text-transform: uppercase;
}

.duration-edit {
  display: flex;
  gap: 0.75rem;
  align-items: flex-end;
}

.extend-buttons {
  display: flex;
  gap: 0.75rem;
  flex-wrap: wrap;
}

.loading,
.empty {
  color: var(--text-muted);
  padding: 2rem 0;
  text-align: center;
}

.empty p {
  margin-bottom: 1rem;
}
</style>
