<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'

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
        class="player-chip"
      >
        <div class="player-chip-avatar">
          {{ authStore.player.displayName.charAt(0).toUpperCase() }}
        </div>
        <span class="player-chip-name">{{ authStore.player.displayName }}</span>
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
            maxPlayers: 0,
            basePointsPerTag: 0,
            confirmationTimeout: '',
            playerCount: 0,
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

.home-subtitle {
  color: #64748b;
  margin: 0.5rem 0 0;
}

.player-chip {
  align-items: center;
  align-self: start;
  background: white;
  border: 1px solid #e2e8f0;
  border-radius: 9999px;
  display: flex;
  gap: 0.625rem;
  padding: 0.375rem 0.875rem 0.375rem 0.375rem;
  width: fit-content;
}

.player-chip-avatar {
  align-items: center;
  background: #1d4ed8;
  border-radius: 50%;
  color: white;
  display: flex;
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

.home-actions {
  display: grid;
  gap: 0.75rem;
  margin-bottom: 2rem;
}

.games-section h2 {
  font-size: 1.125rem;
  margin: 0 0 0.75rem;
}

.games-list {
  display: grid;
  gap: 0.75rem;
}

.games-empty {
  background: white;
  border: 1px solid #e2e8f0;
  border-radius: 1rem;
  color: #64748b;
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
  background: #fef2f2;
  border-radius: 0.5rem;
  color: #991b1b;
  font-size: 0.875rem;
  margin: 0;
  padding: 0.75rem;
}
</style>
