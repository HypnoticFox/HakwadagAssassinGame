<script setup lang="ts">
import { onMounted, ref, computed, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'

import Button from '@/components/Button.vue'
import Input from '@/components/Input.vue'
import Modal from '@/components/Modal.vue'
import { Copy, Link } from '@lucide/vue'
import { useGameSignalR } from '@/composables/useSignalR'
import { useGameStore } from '@/stores'
import {
  GameRole,
  GameStatus,
  canEndGame,
  canStartGame,
  gameRoleLabel,
  gameStatusLabel,
  isGameAdmin,
} from '@/types'
import type { GamePlayerDto } from '@/types'

const route = useRoute()
const router = useRouter()
const { t } = useI18n()
const gameStore = useGameStore()

const gameId = computed(() => route.params.id as string)
const copiedCode = ref(false)
const copiedLink = ref(false)
const adminPanelOpen = ref(false)
const newCondition = ref('')
const safeTimeStart = ref('')
const safeTimeEnd = ref('')
const localError = ref<string | null>(null)
const playersLoading = ref(false)
const newDurationHours = ref('24')

useGameSignalR(gameId.value)

onMounted(async () => {
  await gameStore.loadGame(gameId.value)
  if (gameStore.isCreator) {
    await loadPlayers()
  }
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
  localError.value = null
  try {
    await gameStore.startGame(gameId.value)
  } catch (err) {
    if (err instanceof Error) {
      localError.value = err.message
    }
  }
}

async function onEnd() {
  localError.value = null
  try {
    await gameStore.endGame(gameId.value)
  } catch (err) {
    if (err instanceof Error) {
      localError.value = err.message
    }
  }
}

async function onUpdateDuration() {
  if (!newDurationHours.value) return
  localError.value = null
  try {
    await gameStore.updateDuration(gameId.value, Number(newDurationHours.value))
  } catch (err) {
    if (err instanceof Error) {
      localError.value = err.message
    }
  }
}

async function onExtend(minutes: number) {
  localError.value = null
  try {
    await gameStore.extendDuration(gameId.value, minutes)
  } catch (err) {
    if (err instanceof Error) {
      localError.value = err.message
    }
  }
}

async function onLeave() {
  const confirmKey =
    gameStore.currentGame?.status === GameStatus.Active
      ? 'gameDetail.leaveConfirmActive'
      : 'gameDetail.leaveConfirm'
  if (!confirm(t(confirmKey))) return
  localError.value = null
  try {
    await gameStore.leaveGame(gameId.value)
    if (gameStore.currentGame?.status !== GameStatus.Active) {
      await router.push('/')
    }
  } catch (err) {
    if (err instanceof Error) {
      localError.value = err.message
    }
  }
}

async function onRejoin() {
  localError.value = null
  try {
    await gameStore.rejoinGame(gameId.value)
  } catch (err) {
    if (err instanceof Error) {
      localError.value = err.message
    }
  }
}

async function onAddCondition() {
  if (!newCondition.value) return
  localError.value = null
  try {
    await gameStore.addCondition(gameId.value, newCondition.value)
    newCondition.value = ''
  } catch (err) {
    if (err instanceof Error) {
      localError.value = err.message
    }
  }
}

async function onAddSafeTime() {
  if (!safeTimeStart.value || !safeTimeEnd.value) return
  localError.value = null
  try {
    await gameStore.addSafeTime(gameId.value, {
      startTime: safeTimeStart.value,
      endTime: safeTimeEnd.value,
    })
    safeTimeStart.value = ''
    safeTimeEnd.value = ''
    await gameStore.loadGame(gameId.value)
  } catch (err) {
    if (err instanceof Error) {
      localError.value = err.message
    }
  }
}

async function onRemoveSafeTime(blockId: string) {
  try {
    await gameStore.removeSafeTime(gameId.value, blockId)
    await gameStore.loadGame(gameId.value)
  } catch (err) {
    if (err instanceof Error) {
      localError.value = err.message
    }
  }
}

async function loadPlayers() {
  if (!gameStore.isCreator) return
  playersLoading.value = true
  try {
    await gameStore.loadGamePlayers(gameId.value)
  } catch (err) {
    if (err instanceof Error) {
      localError.value = err.message
    }
  } finally {
    playersLoading.value = false
  }
}

async function onPromote(player: GamePlayerDto) {
  localError.value = null
  try {
    await gameStore.addAdmin(gameId.value, player.playerId)
    await Promise.all([gameStore.loadGame(gameId.value), loadPlayers()])
  } catch (err) {
    if (err instanceof Error) {
      localError.value = err.message
    }
  }
}

async function onRemove(player: GamePlayerDto) {
  if (!confirm(t('gameDetail.admin.confirmRemoveModerator'))) return
  localError.value = null
  try {
    await gameStore.removeAdmin(gameId.value, player.playerId)
    await Promise.all([gameStore.loadGame(gameId.value), loadPlayers()])
  } catch (err) {
    if (err instanceof Error) {
      localError.value = err.message
    }
  }
}

watch(adminPanelOpen, (open) => {
  if (open) {
    loadPlayers()
  }
})

const formattedCreatedAt = computed(() => {
  if (!gameStore.currentGame) return ''
  return new Date(gameStore.currentGame.createdAt).toLocaleString()
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
          isGameAdmin(gameStore.currentGame.myRole) &&
          gameStore.currentGame.status === GameStatus.NotStarted
        "
        class="participation-card"
      >
        <p class="participation-label">{{ $t('gameDetail.participationLabel') }}</p>
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
            />
            <span class="toggle-slider"></span>
          </div>
        </label>
        <p class="min-participants-note">{{ $t('gameDetail.minParticipants') }}</p>
      </div>

      <div
        v-if="
          isGameAdmin(gameStore.currentGame.myRole) &&
          gameStore.currentGame.status === GameStatus.NotStarted
        "
        class="duration-card"
      >
        <p class="duration-label">{{ $t('gameDetail.duration') }}</p>
        <div class="duration-edit">
          <Input
            v-model="newDurationHours"
            :label="$t('gameDetail.durationHours')"
            type="number"
            inputmode="numeric"
            min="1"
          />
          <Button variant="secondary" :loading="gameStore.isLoading" @click="onUpdateDuration">
            {{ $t('gameDetail.updateDuration') }}
          </Button>
        </div>
      </div>

      <div
        v-if="
          isGameAdmin(gameStore.currentGame.myRole) &&
          gameStore.currentGame.status === GameStatus.Active
        "
        class="extend-card"
      >
        <p class="extend-label">{{ $t('gameDetail.extendGame') }}</p>
        <div class="extend-buttons">
          <Button variant="secondary" :loading="gameStore.isLoading" @click="onExtend(5)">
            {{ $t('gameDetail.extend5min') }}
          </Button>
          <Button variant="secondary" :loading="gameStore.isLoading" @click="onExtend(60)">
            {{ $t('gameDetail.extend1hour') }}
          </Button>
          <Button variant="secondary" :loading="gameStore.isLoading" @click="onExtend(1440)">
            {{ $t('gameDetail.extend1day') }}
          </Button>
        </div>
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
            {{ $t('gameDetail.maxPlayers') }}
          </p>
          <p class="detail-value">
            {{ gameStore.currentGame.maxPlayers }}
          </p>
        </div>
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
            {{ $t('gameDetail.timeout') }}
          </p>
          <p class="detail-value">
            {{ gameStore.currentGame.confirmationTimeout }}
          </p>
        </div>
        <div class="detail-card">
          <p class="detail-label">
            {{ $t('gameDetail.created') }}
          </p>
          <p class="detail-value">
            {{ formattedCreatedAt }}
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

      <div v-if="gameStore.currentGame.safeTimeBlocks.length > 0" class="safe-times">
        <h2>{{ $t('gameDetail.safeTimes') }}</h2>
        <ul class="safe-time-list">
          <li
            v-for="block in gameStore.currentGame.safeTimeBlocks"
            :key="block.id"
            class="safe-time-item"
          >
            <span>{{ block.startTime }} – {{ block.endTime }}</span>
            <Button
              v-if="isGameAdmin(gameStore.currentGame.myRole)"
              variant="ghost"
              @click="onRemoveSafeTime(block.id)"
            >
              {{ $t('common.remove') }}
            </Button>
          </li>
        </ul>
      </div>

      <div class="action-grid">
        <Button
          v-if="
            gameStore.currentGame.status === GameStatus.Active &&
            gameStore.currentGame.isParticipating
          "
          size="large"
          full-width
          @click="router.push(`/games/${gameId}/assignment`)"
        >
          {{ $t('gameDetail.myAssignment') }}
        </Button>
        <Button
          variant="secondary"
          size="large"
          full-width
          @click="router.push(`/games/${gameId}/leaderboard`)"
        >
          {{ $t('gameDetail.leaderboard') }}
        </Button>
      </div>

      <div v-if="isGameAdmin(gameStore.currentGame.myRole)" class="admin-section">
        <Button variant="secondary" full-width @click="adminPanelOpen = true">
          {{ $t('gameDetail.adminPanel') }}
        </Button>
      </div>

      <!-- Leave/Rejoin button based on participation status -->
      <div v-if="gameStore.currentGame.status === GameStatus.Active">
        <Button v-if="gameStore.currentGame.isParticipating" variant="ghost" @click="onLeave">
          {{ $t('gameDetail.leaveGame') }}
        </Button>
        <Button v-else variant="secondary" size="large" full-width @click="onRejoin">
          {{ $t('gameDetail.rejoinGame') }}
        </Button>
      </div>
      <Button v-else variant="ghost" @click="onLeave">
        {{ $t('gameDetail.leaveGame') }}
      </Button>

      <p v-if="localError" class="form-error" role="alert">
        {{ localError }}
      </p>
    </div>

    <div v-else-if="gameStore.isLoading" class="loading">
      {{ $t('gameDetail.loading') }}
    </div>
    <div v-else class="empty">
      <p>{{ $t('gameDetail.notFound') }}</p>
      <Button @click="router.push('/')">
        {{ $t('common.backHome') }}
      </Button>
    </div>

    <Modal
      :open="adminPanelOpen"
      :title="$t('gameDetail.admin.title')"
      @close="adminPanelOpen = false"
    >
      <div class="admin-form">
        <h3>{{ $t('gameDetail.admin.conditions') }}</h3>
        <Input
          v-model="newCondition"
          :label="$t('gameDetail.admin.newCondition')"
          :placeholder="$t('gameDetail.admin.newConditionPlaceholder')"
        />
        <Button full-width @click="onAddCondition">
          {{ $t('gameDetail.admin.addCondition') }}
        </Button>

        <h3>{{ $t('gameDetail.admin.safeTimeBlock') }}</h3>
        <Input
          v-model="safeTimeStart"
          :label="$t('gameDetail.admin.startTime')"
          type="time"
          required
        />
        <Input v-model="safeTimeEnd" :label="$t('gameDetail.admin.endTime')" type="time" required />
        <Button full-width @click="onAddSafeTime">
          {{ $t('gameDetail.admin.addSafeTime') }}
        </Button>

        <template v-if="gameStore.isCreator">
          <h3>{{ $t('gameDetail.admin.moderators') }}</h3>
          <p v-if="playersLoading" class="players-loading">
            {{ $t('common.loading') }}
          </p>
          <p v-else-if="gameStore.gamePlayers.length === 0" class="players-empty">
            {{ $t('gameDetail.admin.noPlayers') }}
          </p>
          <ul v-else class="player-management-list">
            <li
              v-for="player in gameStore.gamePlayers"
              :key="player.playerId"
              class="player-management-item"
            >
              <div class="player-management-avatar">
                <img v-if="player.avatarUrl" :src="player.avatarUrl" :alt="player.displayName" />
                <span v-else>{{ player.displayName.charAt(0).toUpperCase() }}</span>
              </div>
              <div class="player-management-info">
                <p class="player-management-name">
                  {{ player.displayName }}
                </p>
                <span class="player-management-role">{{ gameRoleLabel(player.role) }}</span>
              </div>
              <Button
                v-if="player.role === GameRole.CoAdmin"
                variant="ghost"
                class="player-management-action"
                @click="onRemove(player)"
              >
                {{ $t('gameDetail.admin.removeModerator') }}
              </Button>
              <Button
                v-else-if="player.role === GameRole.Player"
                variant="secondary"
                class="player-management-action"
                @click="onPromote(player)"
              >
                {{ $t('gameDetail.admin.promoteToModer') }}
              </Button>
            </li>
          </ul>
        </template>
      </div>
    </Modal>
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

.admin-form {
  display: grid;
  gap: 1rem;
}

.admin-form h3 {
  font-size: 1rem;
  margin: 0.5rem 0 0;
}

.players-loading,
.players-empty {
  color: var(--text-muted);
  font-size: 0.875rem;
  margin: 0;
}

.player-management-list {
  display: grid;
  gap: 0.5rem;
  list-style: none;
  margin: 0;
  max-height: 18rem;
  overflow-y: auto;
  padding: 0;
}

.player-management-item {
  align-items: center;
  background: var(--surface);
  border: 1px solid var(--border);
  border-radius: 0.75rem;
  display: flex;
  gap: 0.75rem;
  padding: 0.625rem 0.75rem;
}

.player-management-avatar {
  align-items: center;
  background: var(--primary);
  border-radius: 50%;
  color: var(--text-inverse);
  display: flex;
  flex-shrink: 0;
  font-size: 0.875rem;
  font-weight: 700;
  height: 2.25rem;
  justify-content: center;
  overflow: hidden;
  width: 2.25rem;
}

.player-management-avatar img {
  height: 100%;
  object-fit: cover;
  width: 100%;
}

.player-management-info {
  flex: 1;
  min-width: 0;
}

.player-management-name {
  font-weight: 600;
  margin: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.player-management-role {
  background: var(--surface-muted);
  border-radius: var(--radius-full);
  color: var(--text-secondary);
  font-size: 0.6875rem;
  font-weight: 700;
  padding: 0.125rem 0.5rem;
  text-transform: uppercase;
}

.player-management-action {
  flex-shrink: 0;
  font-size: 0.8125rem;
  min-height: 2rem;
  padding: 0.375rem 0.625rem;
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

.form-error {
  background: var(--danger-bg);
  border-radius: 0.5rem;
  color: var(--danger-text);
  font-size: 0.875rem;
  margin: 0;
  padding: 0.75rem;
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
