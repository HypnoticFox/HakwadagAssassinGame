<script setup lang="ts">
import { Check, Pencil, X } from '@lucide/vue'
import { computed, nextTick, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'

import { api } from '@/api/client'
import Button from '@/components/Button.vue'
import GameCard from '@/components/GameCard.vue'
import Input from '@/components/Input.vue'
import Modal from '@/components/Modal.vue'
import { usePushNotifications } from '@/composables/usePushNotifications'
import { useAuthStore, useGameStore } from '@/stores'
import { GameStatus } from '@/types'

const router = useRouter()
const { t } = useI18n()
const authStore = useAuthStore()
const gameStore = useGameStore()
const push = usePushNotifications()

const joinModalOpen = ref(false)
const inviteCode = ref('')
const displayName = ref(authStore.player?.displayName || '')
const localError = ref<string | null>(null)
const pushEnabled = ref(false)

const isEditingName = ref(false)
const editName = ref(authStore.player?.displayName || '')
const isSavingName = ref(false)
const nameError = ref<string | null>(null)
const nameInputRef = ref<HTMLInputElement | null>(null)

onMounted(async () => {
  try {
    await gameStore.loadMyGames()
  } catch (err) {
    if (err instanceof Error) {
      localError.value = err.message
    }
  }
})

async function enableNotifications() {
  const ok = await push.registerSubscription()
  if (ok) {
    pushEnabled.value = true
  }
}

const sortedGames = computed(() => {
  return [...gameStore.recentGames].sort((a, b) => {
    const statusOrder = [GameStatus.Active, GameStatus.NotStarted, GameStatus.Ended]
    const aIndex = statusOrder.indexOf(a.status)
    const bIndex = statusOrder.indexOf(b.status)
    if (aIndex !== bIndex) return aIndex - bIndex
    return new Date(b.joinedAt).getTime() - new Date(a.joinedAt).getTime()
  })
})

function navigateToGame(gameId: string) {
  void router.push(`/games/${gameId}`)
}

async function onJoin() {
  if (!inviteCode.value) return
  localError.value = null
  try {
    const game = await gameStore.joinGame(inviteCode.value, displayName.value || t('home.joinModal.defaultPlayerName'))
    joinModalOpen.value = false
    inviteCode.value = ''
    await router.push(`/games/${game.id}`)
  } catch (err) {
    if (err instanceof Error) {
      localError.value = err.message
    }
  }
}

async function startEditingName() {
  editName.value = authStore.player?.displayName || ''
  nameError.value = null
  isEditingName.value = true
  await nextTick()
  nameInputRef.value?.focus()
}

function cancelEditingName() {
  isEditingName.value = false
  nameError.value = null
}

async function saveName() {
  const trimmed = editName.value.trim()
  if (!trimmed) return
  isSavingName.value = true
  nameError.value = null
  try {
    const updated = await api.updatePlayer(trimmed)
    authStore.setPlayer(updated)
    isEditingName.value = false
  } catch (err) {
    if (err instanceof Error) {
      nameError.value = err.message
    }
  } finally {
    isSavingName.value = false
  }
}
</script>

<template>
  <section class="page-section">
    <div class="home-header">
      <div>
        <p class="eyebrow">
          {{ $t('home.eyebrow') }}
        </p>
        <h1>{{ $t('home.title') }}</h1>
        <p class="home-subtitle">
          {{ $t('home.subtitle') }}
        </p>
      </div>
      <div
        v-if="authStore.player"
        class="player-chip-wrapper"
      >
        <div
          class="player-chip"
          :class="{ 'player-chip--editing': isEditingName }"
        >
          <div class="player-chip-avatar">
            {{ authStore.player.displayName.charAt(0).toUpperCase() }}
          </div>

          <template v-if="!isEditingName">
            <span class="player-chip-name">{{ authStore.player.displayName }}</span>
            <button
              type="button"
              class="player-chip-action"
              :aria-label="$t('home.editDisplayName.label')"
              @click="startEditingName"
            >
              <Pencil :size="16" />
            </button>
          </template>

          <template v-else>
            <input
              ref="nameInputRef"
              v-model="editName"
              type="text"
              class="player-chip-input"
              :placeholder="$t('home.joinModal.displayNamePlaceholder')"
              :disabled="isSavingName"
              @keyup.enter="saveName"
              @keyup.escape="cancelEditingName"
            >
            <button
              type="button"
              class="player-chip-action player-chip-action--save"
              :aria-label="$t('common.save')"
              :disabled="isSavingName || !editName.trim()"
              @click="saveName"
            >
              <Check :size="18" />
            </button>
            <button
              type="button"
              class="player-chip-action player-chip-action--cancel"
              :aria-label="$t('common.cancel')"
              :disabled="isSavingName"
              @click="cancelEditingName"
            >
              <X :size="18" />
            </button>
          </template>
        </div>
        <p
          v-if="nameError"
          class="name-error"
          role="alert"
        >
          {{ nameError }}
        </p>
      </div>
    </div>

    <div class="home-actions">
      <Button
        size="large"
        full-width
        @click="router.push('/games/create')"
      >
        {{ $t('home.createGame') }}
      </Button>
      <Button
        variant="secondary"
        size="large"
        full-width
        @click="joinModalOpen = true"
      >
        {{ $t('home.joinGame') }}
      </Button>
      <Button
        v-if="push.isSupported && !pushEnabled"
        variant="ghost"
        size="large"
        full-width
        @click="enableNotifications"
      >
        {{ $t('home.enableNotifications') }}
      </Button>
    </div>

    <div class="games-section">
      <h2>{{ $t('home.yourGames') }}</h2>
      <div
        v-if="sortedGames.length > 0"
        class="games-list"
      >
        <GameCard
          v-for="game in sortedGames"
          :key="game.id"
          :game="{
            id: game.id,
            name: game.name,
            inviteCode: '',
            status: game.status,
            createdAt: game.joinedAt,
            maxPlayers: game.maxPlayers,
            basePointsPerTag: game.basePointsPerTag,
            confirmationTimeout: '',
            playerCount: game.playerCount,
            participantCount: 0,
            isParticipating: false,
            myRole: game.myRole,
            safeTimeBlocks: [],
          }"
          @click="navigateToGame"
        />
      </div>
      <div
        v-else
        class="games-empty"
      >
        <p>{{ $t('home.noGames') }}</p>
      </div>
    </div>

    <Modal
      :open="joinModalOpen"
      :title="$t('home.joinModal.title')"
      @close="joinModalOpen = false"
    >
      <div class="join-form">
        <Input
          v-model="inviteCode"
          :label="$t('home.joinModal.inviteCode')"
          :placeholder="$t('home.joinModal.inviteCodePlaceholder')"
          required
        />
        <Input
          v-model="displayName"
          :label="$t('home.joinModal.displayName')"
          :placeholder="$t('home.joinModal.displayNamePlaceholder')"
          required
        />
        <p
          v-if="localError"
          class="form-error"
          role="alert"
        >
          {{ localError }}
        </p>
        <Button
          full-width
          :loading="gameStore.isLoading"
          @click="onJoin"
        >
          {{ $t('home.joinModal.join') }}
        </Button>
      </div>
    </Modal>
  </section>
</template>

<style scoped>
.home-header {
  display: grid;
  gap: 1rem;
  margin-bottom: 1.5rem;
}

.home-header h1 {
  color: var(--text);
}

.home-subtitle {
  color: var(--text-muted);
  margin: 0.5rem 0 0;
}

.player-chip-wrapper {
  display: grid;
  gap: 0.5rem;
}

.player-chip {
  align-items: center;
  align-self: start;
  background: var(--surface);
  border: 1px solid var(--border);
  border-radius: var(--radius-full);
  display: flex;
  gap: 0.5rem;
  padding: 0.375rem 0.5rem 0.375rem 0.375rem;
  width: fit-content;
}

.player-chip--editing {
  border-radius: var(--radius);
  padding-right: 0.375rem;
}

.player-chip-avatar {
  align-items: center;
  background: var(--primary);
  border-radius: 50%;
  color: var(--text-inverse);
  display: flex;
  flex-shrink: 0;
  font-size: 0.75rem;
  font-weight: 700;
  height: 1.75rem;
  justify-content: center;
  width: 1.75rem;
}

.player-chip-name {
  font-size: 0.875rem;
  font-weight: 600;
}

.player-chip-action {
  align-items: center;
  background: transparent;
  border: 0;
  border-radius: 50%;
  color: var(--text-muted);
  cursor: pointer;
  display: flex;
  flex-shrink: 0;
  height: 2rem;
  justify-content: center;
  padding: 0;
  transition:
    background-color 0.15s ease,
    color 0.15s ease;
  width: 2rem;
}

.player-chip-action:hover:not(:disabled) {
  background: var(--surface-muted);
  color: var(--text);
}

.player-chip-action:focus-visible {
  outline: 3px solid var(--focus);
  outline-offset: 2px;
}

.player-chip-action:disabled {
  cursor: not-allowed;
  opacity: 0.5;
}

.player-chip-action--save {
  color: var(--success);
}

.player-chip-action--save:hover:not(:disabled) {
  background: var(--success-bg);
  color: var(--success);
}

.player-chip-action--cancel {
  color: var(--danger);
}

.player-chip-action--cancel:hover:not(:disabled) {
  background: var(--danger-bg);
  color: var(--danger);
}

.player-chip-input {
  appearance: none;
  background: var(--surface);
  border: 1px solid var(--border-input);
  border-radius: var(--radius-sm);
  color: var(--text);
  font-family: inherit;
  font-size: 0.875rem;
  font-weight: 600;
  min-height: 2rem;
  min-width: 6rem;
  padding: 0.375rem 0.5rem;
  width: 100%;
}

.player-chip-input:focus {
  border-color: var(--primary);
  box-shadow: 0 0 0 3px var(--primary-ring);
  outline: none;
}

.player-chip-input:disabled {
  opacity: 0.6;
}

.name-error {
  background: var(--danger-bg);
  border-radius: var(--radius-sm);
  color: var(--danger-text);
  font-size: 0.875rem;
  margin: 0;
  padding: 0.75rem;
}

.home-actions {
  display: grid;
  gap: 0.75rem;
  margin-bottom: 2rem;
}

.games-section h2 {
  color: var(--text);
  font-size: 1.125rem;
  margin: 0 0 0.75rem;
}

.games-list {
  display: grid;
  gap: 0.75rem;
}

.games-empty {
  background: var(--surface);
  border: 1px solid var(--border);
  border-radius: 1rem;
  color: var(--text-muted);
  padding: 1.5rem;
  text-align: center;
}

.games-empty p {
  margin: 0;
}

.join-form {
  display: grid;
  gap: 1rem;
}

.form-error {
  background: var(--danger-bg);
  border-radius: 0.5rem;
  color: var(--danger-text);
  font-size: 0.875rem;
  margin: 0;
  padding: 0.75rem;
}
</style>
