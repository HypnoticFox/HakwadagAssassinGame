<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'

import Button from '@/components/Button.vue'
import Input from '@/components/Input.vue'
import LoadingSpinner from '@/components/LoadingSpinner.vue'
import Modal from '@/components/Modal.vue'
import { ArrowLeft, Clock, ListChecks, Settings, Shield } from '@lucide/vue'
import { formatTimeOfDay, localTimeToDateTimeOffset } from '@/composables/useSafeTime'
import { useGameSignalR } from '@/composables/useSignalR'
import { useToast } from '@/composables/useToast'
import { useGameStore } from '@/stores'
import { GameRole, gameRoleLabel, isGameAdmin } from '@/types'
import type { GamePlayerDto } from '@/types'
import { timeSpanToMinutes } from '@/utils/format'

const route = useRoute()
const router = useRouter()
const gameStore = useGameStore()

const gameId = computed(() => route.params.id as string)
const { toast } = useToast()
const newCondition = ref('')
const safeTimeStart = ref('')
const safeTimeEnd = ref('')
const timeoutMinutes = ref('')
const cooldownMinutes = ref('')
const playersLoading = ref(false)
const showRemoveModeratorModal = ref(false)
const removeModeratorTarget = ref<GamePlayerDto | null>(null)

useGameSignalR(gameId.value)

onMounted(async () => {
  await gameStore.loadGame(gameId.value)
  if (gameStore.currentGame) {
    const parsed = timeSpanToMinutes(gameStore.currentGame.confirmationTimeout)
    timeoutMinutes.value =
      parsed !== null ? String(parsed) : gameStore.currentGame.confirmationTimeout
    cooldownMinutes.value = String(gameStore.currentGame.assignmentCooldownMinutes)
  }
  if (gameStore.isCreator) {
    await loadPlayers()
  }
})

async function onUpdateTimeout() {
  if (!timeoutMinutes.value) return
  try {
    await gameStore.updateConfirmationTimeout(gameId.value, Number(timeoutMinutes.value))
  } catch (err) {
    if (err instanceof Error) {
      toast(err.message, 'error')
    }
  }
}

async function onUpdateCooldown() {
  if (cooldownMinutes.value === '') return
  try {
    await gameStore.updateAssignmentCooldown(gameId.value, Number(cooldownMinutes.value))
  } catch (err) {
    if (err instanceof Error) {
      toast(err.message, 'error')
    }
  }
}

async function onAddCondition() {
  if (!newCondition.value) return
  try {
    await gameStore.addCondition(gameId.value, newCondition.value)
    newCondition.value = ''
  } catch (err) {
    if (err instanceof Error) {
      toast(err.message, 'error')
    }
  }
}

async function onAddSafeTime() {
  if (!safeTimeStart.value || !safeTimeEnd.value) return
  try {
    await gameStore.addSafeTime(gameId.value, {
      startTime: localTimeToDateTimeOffset(safeTimeStart.value),
      endTime: localTimeToDateTimeOffset(safeTimeEnd.value),
    })
    safeTimeStart.value = ''
    safeTimeEnd.value = ''
    await gameStore.loadGame(gameId.value)
  } catch (err) {
    if (err instanceof Error) {
      toast(err.message, 'error')
    }
  }
}

async function onRemoveSafeTime(blockId: string) {
  try {
    await gameStore.removeSafeTime(gameId.value, blockId)
    await gameStore.loadGame(gameId.value)
  } catch (err) {
    if (err instanceof Error) {
      toast(err.message, 'error')
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
      toast(err.message, 'error')
    }
  } finally {
    playersLoading.value = false
  }
}

async function onPromote(player: GamePlayerDto) {
  try {
    await gameStore.addAdmin(gameId.value, player.playerId)
    await Promise.all([gameStore.loadGame(gameId.value), loadPlayers()])
  } catch (err) {
    if (err instanceof Error) {
      toast(err.message, 'error')
    }
  }
}

function onRemove(player: GamePlayerDto) {
  removeModeratorTarget.value = player
  showRemoveModeratorModal.value = true
}

async function confirmRemoveModerator() {
  const target = removeModeratorTarget.value
  if (!target) return
  showRemoveModeratorModal.value = false
  try {
    await gameStore.removeAdmin(gameId.value, target.playerId)
    await Promise.all([gameStore.loadGame(gameId.value), loadPlayers()])
  } catch (err) {
    if (err instanceof Error) {
      toast(err.message, 'error')
    }
  }
}
</script>

<template>
  <section class="page-section">
    <div v-if="gameStore.currentGame">
      <template v-if="isGameAdmin(gameStore.currentGame.myRole)">
        <div class="admin-header">
          <div>
            <p class="eyebrow">
              {{ gameStore.currentGame.name }}
            </p>
            <h1>{{ $t('gameDetail.admin.title') }}</h1>
          </div>
          <Button variant="secondary" @click="router.push(`/games/${gameId}`)">
            <ArrowLeft :size="18" />
            {{ $t('common.backToGame') }}
          </Button>
        </div>

        <section class="admin-card">
          <div class="section-heading">
            <Settings :size="20" />
            <h2>{{ $t('gameDetail.admin.settings') }}</h2>
          </div>
          <div class="settings-grid">
            <div class="setting-form">
              <Input
                v-model="timeoutMinutes"
                :label="$t('gameDetail.admin.confirmationTimeoutLabel')"
                type="number"
                inputmode="numeric"
                min="1"
                required
              />
              <Button variant="secondary" :loading="gameStore.isLoading" @click="onUpdateTimeout">
                {{ $t('gameDetail.admin.updateSettings') }}
              </Button>
            </div>
            <div class="setting-form">
              <Input
                v-model="cooldownMinutes"
                :label="$t('gameDetail.admin.assignmentCooldownLabel')"
                type="number"
                inputmode="numeric"
                min="0"
              />
              <Button variant="secondary" :loading="gameStore.isLoading" @click="onUpdateCooldown">
                {{ $t('gameDetail.admin.updateSettings') }}
              </Button>
            </div>
          </div>
        </section>

        <section class="admin-card">
          <div class="section-heading">
            <ListChecks :size="20" />
            <h2>{{ $t('gameDetail.admin.conditions') }}</h2>
          </div>
          <div class="condition-form">
            <Input
              v-model="newCondition"
              :label="$t('gameDetail.admin.newCondition')"
              :placeholder="$t('gameDetail.admin.newConditionPlaceholder')"
            />
            <Button :loading="gameStore.isLoading" @click="onAddCondition">
              {{ $t('gameDetail.admin.addCondition') }}
            </Button>
          </div>
        </section>

        <section class="admin-card">
          <div class="section-heading">
            <Clock :size="20" />
            <h2>{{ $t('gameDetail.admin.safeTimeBlock') }}</h2>
          </div>
          <ul v-if="gameStore.currentGame.safeTimeBlocks.length > 0" class="safe-time-list">
            <li
              v-for="block in gameStore.currentGame.safeTimeBlocks"
              :key="block.id"
              class="safe-time-item"
            >
              <span>{{ formatTimeOfDay(block.startTime) }} – {{ formatTimeOfDay(block.endTime) }}</span>
              <Button variant="ghost" @click="onRemoveSafeTime(block.id)">
                {{ $t('common.remove') }}
              </Button>
            </li>
          </ul>
          <p v-else class="section-empty">
            {{ $t('gameDetail.noSafeTimes') }}
          </p>
          <div class="safe-time-form">
            <Input
              v-model="safeTimeStart"
              :label="$t('gameDetail.admin.startTime')"
              type="time"
              required
            />
            <Input
              v-model="safeTimeEnd"
              :label="$t('gameDetail.admin.endTime')"
              type="time"
              required
            />
            <Button :loading="gameStore.isLoading" @click="onAddSafeTime">
              {{ $t('gameDetail.admin.addSafeTime') }}
            </Button>
          </div>
        </section>

        <section v-if="gameStore.isCreator" class="admin-card">
          <div class="section-heading">
            <Shield :size="20" />
            <h2>{{ $t('gameDetail.admin.moderators') }}</h2>
          </div>
          <LoadingSpinner
            v-if="playersLoading"
            inline
          />
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
        </section>

        <!-- Remove moderator confirmation dialog -->
        <Modal
          :open="showRemoveModeratorModal"
          :title="$t('gameDetail.admin.removeModerator')"
          @close="showRemoveModeratorModal = false"
        >
          <p>{{ $t('gameDetail.admin.confirmRemoveModerator') }}</p>
          <template #footer>
            <Button
              variant="secondary"
              @click="showRemoveModeratorModal = false"
            >
              {{ $t('common.cancel') }}
            </Button>
            <Button
              variant="danger"
              :loading="gameStore.isLoading"
              @click="confirmRemoveModerator"
            >
              {{ $t('common.confirm') }}
            </Button>
          </template>
        </Modal>
      </template>

      <div v-else class="empty">
        <p>{{ $t('gameDetail.admin.notAdmin') }}</p>
        <Button @click="router.push(`/games/${gameId}`)">
          {{ $t('common.backToGame') }}
        </Button>
      </div>
    </div>

    <LoadingSpinner v-else-if="gameStore.isLoading" />
    <div v-else class="empty">
      <p>{{ $t('gameDetail.notFound') }}</p>
      <Button @click="router.push('/')">
        {{ $t('common.backHome') }}
      </Button>
    </div>
  </section>
</template>

<style scoped>
.admin-header {
  align-items: flex-start;
  display: grid;
  gap: 1rem;
  margin-bottom: 1.5rem;
}

.admin-card {
  background: var(--surface);
  border: 1px solid var(--border);
  border-radius: 1rem;
  display: grid;
  gap: 1rem;
  margin-bottom: 1.5rem;
  padding: 1.25rem;
}

.section-heading {
  align-items: center;
  display: flex;
  gap: 0.5rem;
}

.section-heading h2 {
  font-size: 1.125rem;
  margin: 0;
}

.section-heading svg {
  color: var(--text-secondary);
}

.settings-grid {
  display: grid;
  gap: 1rem;
}

.setting-form,
.condition-form,
.safe-time-form {
  align-items: flex-end;
  display: grid;
  gap: 0.75rem;
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
  background: var(--surface-muted);
  border: 1px solid var(--border);
  border-radius: 0.75rem;
  display: flex;
  gap: 0.75rem;
  justify-content: space-between;
  padding: 0.625rem 0.75rem;
}

.section-empty {
  color: var(--text-muted);
  font-size: 0.875rem;
  margin: 0;
}

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
  padding: 0;
}

.player-management-item {
  align-items: center;
  background: var(--surface-muted);
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
  background: var(--surface);
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

.empty {
  color: var(--text-muted);
  padding: 2rem 0;
  text-align: center;
}

.empty p {
  margin-bottom: 1rem;
}

@media (min-width: 640px) {
  .settings-grid {
    grid-template-columns: repeat(2, 1fr);
  }

  .safe-time-form {
    grid-template-columns: 1fr 1fr auto;
  }
}
</style>
