<script setup lang="ts">
import { onMounted, ref, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'

import Button from '@/components/Button.vue'
import Input from '@/components/Input.vue'
import Modal from '@/components/Modal.vue'
import { useGameSignalR } from '@/composables/useSignalR'
import { useGameStore } from '@/stores'
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
const gameStore = useGameStore()

const gameId = computed(() => route.params.id as string)
const copied = ref(false)
const adminPanelOpen = ref(false)
const newCondition = ref('')
const safeTimeStart = ref('')
const safeTimeEnd = ref('')
const safeTimeDay = ref('0')
const localError = ref<string | null>(null)

useGameSignalR(gameId.value)

onMounted(async () => {
  await gameStore.loadGame(gameId.value)
})

async function copyInviteCode() {
  if (!gameStore.currentGame) return
  try {
    await navigator.clipboard.writeText(gameStore.currentGame.inviteCode)
    copied.value = true
    setTimeout(() => (copied.value = false), 2000)
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

async function onLeave() {
  if (!confirm('Are you sure you want to leave this game?')) return
  localError.value = null
  try {
    await gameStore.leaveGame(gameId.value)
    await router.push('/')
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
      day: Number(safeTimeDay.value),
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

const formattedCreatedAt = computed(() => {
  if (!gameStore.currentGame) return ''
  return new Date(gameStore.currentGame.createdAt).toLocaleString()
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
            {{ gameStore.currentGame.playerCount }} players
          </p>
        </div>
        <div class="game-actions">
          <Button
            v-if="canStartGame(gameStore.currentGame.myRole, gameStore.currentGame.status)"
            size="large"
            @click="onStart"
          >
            Start game
          </Button>
          <Button
            v-if="canEndGame(gameStore.currentGame.myRole, gameStore.currentGame.status)"
            variant="danger"
            @click="onEnd"
          >
            End game
          </Button>
        </div>
      </div>

      <div class="invite-card">
        <div>
          <p class="invite-label">
            Invite code
          </p>
          <p class="invite-code">
            {{ gameStore.currentGame.inviteCode }}
          </p>
        </div>
        <Button
          variant="secondary"
          @click="copyInviteCode"
        >
          {{ copied ? 'Copied!' : 'Copy' }}
        </Button>
      </div>

      <div class="detail-grid">
        <div class="detail-card">
          <p class="detail-label">
            Max players
          </p>
          <p class="detail-value">
            {{ gameStore.currentGame.maxPlayers }}
          </p>
        </div>
        <div class="detail-card">
          <p class="detail-label">
            Points per tag
          </p>
          <p class="detail-value">
            {{ gameStore.currentGame.basePointsPerTag }}
          </p>
        </div>
        <div class="detail-card">
          <p class="detail-label">
            Timeout
          </p>
          <p class="detail-value">
            {{ gameStore.currentGame.confirmationTimeout }}
          </p>
        </div>
        <div class="detail-card">
          <p class="detail-label">
            Created
          </p>
          <p class="detail-value">
            {{ formattedCreatedAt }}
          </p>
        </div>
      </div>

      <div
        v-if="gameStore.currentGame.safeTimeBlocks.length > 0"
        class="safe-times"
      >
        <h2>Safe times</h2>
        <ul class="safe-time-list">
          <li
            v-for="block in gameStore.currentGame.safeTimeBlocks"
            :key="block.id"
            class="safe-time-item"
          >
            <span>{{ block.startTime }} – {{ block.endTime }}</span>
            <span
              v-if="block.day !== undefined"
              class="safe-time-day"
            >Day {{ block.day }}</span>
            <Button
              v-if="isGameAdmin(gameStore.currentGame.myRole)"
              variant="ghost"
              @click="onRemoveSafeTime(block.id)"
            >
              Remove
            </Button>
          </li>
        </ul>
      </div>

      <div class="action-grid">
        <Button
          v-if="gameStore.currentGame.status === GameStatus.Active"
          size="large"
          full-width
          @click="router.push(`/games/${gameId}/assignment`)"
        >
          My assignment
        </Button>
        <Button
          variant="secondary"
          size="large"
          full-width
          @click="router.push(`/games/${gameId}/leaderboard`)"
        >
          Leaderboard
        </Button>
      </div>

      <div
        v-if="isGameAdmin(gameStore.currentGame.myRole)"
        class="admin-section"
      >
        <Button
          variant="secondary"
          full-width
          @click="adminPanelOpen = true"
        >
          Admin panel
        </Button>
      </div>

      <Button
        variant="ghost"
        @click="onLeave"
      >
        Leave game
      </Button>

      <p
        v-if="localError"
        class="form-error"
        role="alert"
      >
        {{ localError }}
      </p>
    </div>

    <div
      v-else-if="gameStore.isLoading"
      class="loading"
    >
      Loading game...
    </div>
    <div
      v-else
      class="empty"
    >
      <p>Game not found.</p>
      <Button @click="router.push('/')">
        Back home
      </Button>
    </div>

    <Modal
      :open="adminPanelOpen"
      title="Admin panel"
      @close="adminPanelOpen = false"
    >
      <div class="admin-form">
        <h3>Conditions</h3>
        <Input
          v-model="newCondition"
          label="New condition"
          placeholder="Describe the condition"
        />
        <Button
          full-width
          @click="onAddCondition"
        >
          Add condition
        </Button>

        <h3>Safe time block</h3>
        <Input
          v-model="safeTimeStart"
          label="Start time"
          type="time"
          required
        />
        <Input
          v-model="safeTimeEnd"
          label="End time"
          type="time"
          required
        />
        <Input
          v-model="safeTimeDay"
          label="Day (0 = Sunday)"
          type="number"
          inputmode="numeric"
          min="0"
          max="6"
        />
        <Button
          full-width
          @click="onAddSafeTime"
        >
          Add safe time
        </Button>
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
  color: #64748b;
  margin: 0.5rem 0 0;
}

.game-actions {
  display: flex;
  gap: 0.75rem;
}

.invite-card {
  align-items: center;
  background: white;
  border: 1px solid #e2e8f0;
  border-radius: 1rem;
  display: flex;
  justify-content: space-between;
  gap: 1rem;
  padding: 1.25rem;
  margin-bottom: 1.5rem;
}

.invite-label {
  color: #64748b;
  font-size: 0.875rem;
  margin: 0;
}

.invite-code {
  font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
  font-size: 1.5rem;
  font-weight: 700;
  margin: 0.25rem 0 0;
}

.detail-grid {
  display: grid;
  gap: 0.75rem;
  grid-template-columns: repeat(2, 1fr);
  margin-bottom: 1.5rem;
}

.detail-card {
  background: white;
  border: 1px solid #e2e8f0;
  border-radius: 1rem;
  padding: 1rem;
}

.detail-label {
  color: #64748b;
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
  background: white;
  border: 1px solid #e2e8f0;
  border-radius: 0.75rem;
  display: flex;
  gap: 0.75rem;
  justify-content: space-between;
  padding: 0.75rem 1rem;
}

.safe-time-day {
  background: #eff6ff;
  border-radius: 9999px;
  color: #1d4ed8;
  font-size: 0.75rem;
  font-weight: 700;
  padding: 0.25rem 0.625rem;
  text-transform: uppercase;
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

.form-error {
  background: #fef2f2;
  border-radius: 0.5rem;
  color: #991b1b;
  font-size: 0.875rem;
  margin: 0;
  padding: 0.75rem;
}

.loading,
.empty {
  color: #64748b;
  padding: 2rem 0;
  text-align: center;
}

.empty p {
  margin-bottom: 1rem;
}
</style>
