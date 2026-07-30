<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useRouter } from 'vue-router'

import { api } from '@/api/client'
import { useAuthStore } from '@/stores'
import { tagStatusLabel } from '@/types'
import type { DevAssignment, DevPlayer, DevTag, PlayerDto } from '@/types'

interface SeededPlayer {
  player: PlayerDto
  token: string
}

const authStore = useAuthStore()
const router = useRouter()

const isDev = import.meta.env.DEV

const isExpanded = ref(false)
const isLoading = ref(false)
const error = ref<string | null>(null)

const devEmail = ref('test@example.com')
const playerCount = ref(5)

const seededPlayers = ref<SeededPlayer[]>(
  (() => {
    try {
      const saved = sessionStorage.getItem('hakwadag_dev_seeded_players')
      return saved ? (JSON.parse(saved) as SeededPlayer[]) : []
    } catch {
      return []
    }
  })(),
)

const quickActionGameId = ref(
  (() => {
    try {
      return sessionStorage.getItem('hakwadag_dev_quick_game_id') || ''
    } catch {
      return ''
    }
  })(),
)

const activeModal = ref<'submit' | 'confirm' | 'deny' | null>(null)
const actionLoading = ref(false)
const actionError = ref<string | null>(null)
const actionSuccess = ref<string | null>(null)

const modalPlayers = ref<DevPlayer[]>([])
const modalAssignments = ref<DevAssignment[]>([])
const modalTags = ref<DevTag[]>([])
const modalDataLoading = ref(false)

const submitPlayerId = ref('')
const submitAssignmentId = ref('')
const submitConditionId = ref('')
const resolveTagId = ref('')

const currentPlayer = computed(() => authStore.player)
const hasGameId = computed(() => quickActionGameId.value.trim().length > 0)
const pendingTags = computed(() => modalTags.value.filter((t) => t.status === 0))

function saveSeededPlayers() {
  sessionStorage.setItem('hakwadag_dev_seeded_players', JSON.stringify(seededPlayers.value))
}

function saveQuickGameId() {
  sessionStorage.setItem('hakwadag_dev_quick_game_id', quickActionGameId.value)
}

async function handleDevLogin() {
  isLoading.value = true
  error.value = null
  try {
    await authStore.devLogin(devEmail.value)
    await router.push('/')
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Dev login failed'
  } finally {
    isLoading.value = false
  }
}

async function handleSeedGame() {
  isLoading.value = true
  error.value = null
  try {
    const result = await api.seedGame(Number(playerCount.value))
    seededPlayers.value = result.players
    saveSeededPlayers()
    quickActionGameId.value = result.game.id
    saveQuickGameId()
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Seed game failed'
  } finally {
    isLoading.value = false
  }
}

function switchPlayer(token: string, player: PlayerDto) {
  api.setToken(token)
  authStore.player = player
  location.reload()
}

function clearSeededPlayers() {
  seededPlayers.value = []
  sessionStorage.removeItem('hakwadag_dev_seeded_players')
}

function openDashboard() {
  void router.push('/dev/dashboard')
}

function clearActionStatus() {
  actionError.value = null
  actionSuccess.value = null
}

async function loadModalData() {
  if (!quickActionGameId.value.trim()) return
  modalDataLoading.value = true
  try {
    const [players, assignments, tags] = await Promise.all([
      api.devGetGamePlayers(quickActionGameId.value),
      api.devGetGameAssignments(quickActionGameId.value),
      api.devGetGameTags(quickActionGameId.value),
    ])
    modalPlayers.value = players
    modalAssignments.value = assignments
    modalTags.value = tags
  } catch (err) {
    actionError.value = err instanceof Error ? err.message : 'Failed to load game data'
  } finally {
    modalDataLoading.value = false
  }
}

function openModal(type: 'submit' | 'confirm' | 'deny') {
  activeModal.value = type
  clearActionStatus()
  submitPlayerId.value = ''
  submitAssignmentId.value = ''
  submitConditionId.value = ''
  resolveTagId.value = ''
  void loadModalData()
}

function closeModal() {
  activeModal.value = null
  clearActionStatus()
}

async function handleSubmitTag() {
  if (
    !quickActionGameId.value ||
    !submitPlayerId.value ||
    !submitAssignmentId.value ||
    !submitConditionId.value
  ) {
    actionError.value = 'Please select player, assignment, and enter a condition ID'
    return
  }
  actionLoading.value = true
  clearActionStatus()
  try {
    await api.devSubmitTag(
      quickActionGameId.value,
      submitPlayerId.value,
      submitAssignmentId.value,
      submitConditionId.value,
    )
    actionSuccess.value = 'Tag submitted'
    void loadModalData()
  } catch (err) {
    actionError.value = err instanceof Error ? err.message : 'Submit tag failed'
  } finally {
    actionLoading.value = false
  }
}

async function handleConfirmTag() {
  if (!resolveTagId.value) {
    actionError.value = 'Please select a pending tag'
    return
  }
  actionLoading.value = true
  clearActionStatus()
  try {
    await api.devConfirmTag(resolveTagId.value)
    actionSuccess.value = 'Tag confirmed'
    void loadModalData()
  } catch (err) {
    actionError.value = err instanceof Error ? err.message : 'Confirm tag failed'
  } finally {
    actionLoading.value = false
  }
}

async function handleDenyTag() {
  if (!resolveTagId.value) {
    actionError.value = 'Please select a pending tag'
    return
  }
  actionLoading.value = true
  clearActionStatus()
  try {
    await api.devDenyTag(resolveTagId.value)
    actionSuccess.value = 'Tag denied'
    void loadModalData()
  } catch (err) {
    actionError.value = err instanceof Error ? err.message : 'Deny tag failed'
  } finally {
    actionLoading.value = false
  }
}

async function handleEndGame() {
  if (!quickActionGameId.value) {
    actionError.value = 'No game selected'
    return
  }
  if (!confirm('End the current game? This cannot be undone.')) return
  actionLoading.value = true
  clearActionStatus()
  try {
    await api.devEndGame(quickActionGameId.value)
    actionSuccess.value = 'Game ended'
  } catch (err) {
    actionError.value = err instanceof Error ? err.message : 'End game failed'
  } finally {
    actionLoading.value = false
  }
}

watch(activeModal, (modal) => {
  if (modal) {
    void loadModalData()
  }
})

watch(quickActionGameId, () => {
  saveQuickGameId()
})
</script>

<template>
  <div
    v-if="isDev"
    class="dev-switcher"
    :class="{ 'dev-switcher--expanded': isExpanded }"
  >
    <button
      type="button"
      class="dev-switcher__toggle"
      :aria-label="isExpanded ? 'Close dev player switcher' : 'Open dev player switcher'"
      @click="isExpanded = !isExpanded"
    >
      <span class="dev-switcher__icon">🎮</span>
      <span
        v-if="isExpanded"
        class="dev-switcher__title"
      >Dev</span>
    </button>

    <div
      v-if="isExpanded"
      class="dev-switcher__panel"
      role="dialog"
      aria-label="Dev player switcher"
    >
      <div class="dev-switcher__section">
        <p class="dev-switcher__label">
          Current player
        </p>
        <div v-if="currentPlayer">
          <p class="dev-switcher__name">
            {{ currentPlayer.displayName }}
          </p>
          <p class="dev-switcher__email">
            {{ currentPlayer.email }}
          </p>
        </div>
        <p
          v-else
          class="dev-switcher__muted"
        >
          Not logged in
        </p>
      </div>

      <div class="dev-switcher__section">
        <p class="dev-switcher__label">
          Dashboard
        </p>
        <button
          id="dev-open-dashboard"
          type="button"
          class="dev-switcher__button dev-switcher__button--secondary"
          @click="openDashboard"
        >
          Open Dashboard
        </button>
      </div>

      <div class="dev-switcher__section">
        <label
          class="dev-switcher__label"
          for="dev-email"
        >Dev login</label>
        <input
          id="dev-email"
          v-model="devEmail"
          type="email"
          class="dev-switcher__input"
          placeholder="test@example.com"
          @keydown.enter="handleDevLogin"
        >
        <button
          id="dev-login-button"
          type="button"
          class="dev-switcher__button"
          :disabled="isLoading"
          @click="handleDevLogin"
        >
          {{ isLoading ? 'Logging in...' : 'Dev Login' }}
        </button>
      </div>

      <div class="dev-switcher__section">
        <label
          class="dev-switcher__label"
          for="dev-player-count"
        >Seed game</label>
        <input
          id="dev-player-count"
          v-model="playerCount"
          type="number"
          min="2"
          max="20"
          class="dev-switcher__input"
          @keydown.enter="handleSeedGame"
        >
        <button
          id="dev-seed-game-button"
          type="button"
          class="dev-switcher__button"
          :disabled="isLoading"
          @click="handleSeedGame"
        >
          {{ isLoading ? 'Seeding...' : 'Seed Game' }}
        </button>
      </div>

      <div
        v-if="seededPlayers.length"
        class="dev-switcher__section"
      >
        <div class="dev-switcher__row">
          <p class="dev-switcher__label">
            Seeded players
          </p>
          <button
            type="button"
            class="dev-switcher__text-button"
            @click="clearSeededPlayers"
          >
            Clear
          </button>
        </div>
        <ul class="dev-switcher__list">
          <li
            v-for="{ player, token } in seededPlayers"
            :key="player.id"
            class="dev-switcher__player"
          >
            <span class="dev-switcher__player-info">
              <span class="dev-switcher__player-name">{{ player.displayName }}</span>
              <span class="dev-switcher__player-email">{{ player.email }}</span>
            </span>
            <button
              type="button"
              class="dev-switcher__switch-button"
              @click="switchPlayer(token, player)"
            >
              Switch
            </button>
          </li>
        </ul>
      </div>

      <div class="dev-switcher__section">
        <p class="dev-switcher__label">
          Quick Actions
        </p>
        <label
          class="dev-switcher__label dev-switcher__label--small"
          for="dev-quick-game-id"
        >Current game ID</label>
        <input
          id="dev-quick-game-id"
          v-model="quickActionGameId"
          type="text"
          class="dev-switcher__input"
          placeholder="Paste game ID here"
        >
        <div class="dev-switcher__quick-actions">
          <button
            type="button"
            class="dev-switcher__quick-button"
            :disabled="!hasGameId || actionLoading"
            @click="openModal('submit')"
          >
            Submit Tag
          </button>
          <button
            type="button"
            class="dev-switcher__quick-button"
            :disabled="!hasGameId || actionLoading"
            @click="openModal('confirm')"
          >
            Confirm Tag
          </button>
          <button
            type="button"
            class="dev-switcher__quick-button"
            :disabled="!hasGameId || actionLoading"
            @click="openModal('deny')"
          >
            Deny Tag
          </button>
          <button
            type="button"
            class="dev-switcher__quick-button dev-switcher__quick-button--danger"
            :disabled="!hasGameId || actionLoading"
            @click="handleEndGame"
          >
            End Game
          </button>
        </div>
        <p
          v-if="actionError"
          class="dev-switcher__error"
          role="alert"
        >
          {{ actionError }}
        </p>
        <p
          v-if="actionSuccess"
          class="dev-switcher__success"
          role="status"
        >
          {{ actionSuccess }}
        </p>
      </div>

      <p
        v-if="error"
        class="dev-switcher__error"
        role="alert"
      >
        {{ error }}
      </p>
    </div>

    <Teleport to="body">
      <Transition name="dev-modal">
        <div
          v-if="activeModal"
          class="dev-switcher__modal-backdrop"
          @click="closeModal"
        >
          <div
            class="dev-switcher__modal"
            role="dialog"
            aria-modal="true"
            @click.stop
          >
            <header class="dev-switcher__modal-header">
              <h3 class="dev-switcher__modal-title">
                {{
                  activeModal === 'submit'
                    ? 'Submit Tag'
                    : activeModal === 'confirm'
                      ? 'Confirm Tag'
                      : 'Deny Tag'
                }}
              </h3>
              <button
                type="button"
                class="dev-switcher__modal-close"
                aria-label="Close"
                @click="closeModal"
              >
                ×
              </button>
            </header>

            <div class="dev-switcher__modal-body">
              <p
                v-if="modalDataLoading"
                class="dev-switcher__modal-loading"
              >
                Loading...
              </p>

              <template v-if="!modalDataLoading">
                <div
                  v-if="activeModal === 'submit'"
                  class="dev-switcher__modal-form"
                >
                  <label
                    class="dev-switcher__modal-label"
                    for="dev-submit-player"
                  >Player</label>
                  <select
                    id="dev-submit-player"
                    v-model="submitPlayerId"
                    class="dev-switcher__input"
                  >
                    <option value="">
                      Select a player
                    </option>
                    <option
                      v-for="player in modalPlayers"
                      :key="player.playerId"
                      :value="player.playerId"
                    >
                      {{ player.displayName }} ({{ player.email }})
                    </option>
                  </select>

                  <label
                    class="dev-switcher__modal-label"
                    for="dev-submit-assignment"
                  >Assignment</label>
                  <select
                    id="dev-submit-assignment"
                    v-model="submitAssignmentId"
                    class="dev-switcher__input"
                  >
                    <option value="">
                      Select an assignment
                    </option>
                    <option
                      v-for="assignment in modalAssignments"
                      :key="assignment.id"
                      :value="assignment.id"
                    >
                      {{ assignment.hunterName }} → {{ assignment.targetName }}
                    </option>
                  </select>

                  <label
                    class="dev-switcher__modal-label"
                    for="dev-submit-condition"
                  >Condition ID</label>
                  <input
                    id="dev-submit-condition"
                    v-model="submitConditionId"
                    type="text"
                    class="dev-switcher__input"
                    placeholder="Assignment condition ID"
                  >
                </div>

                <div
                  v-if="activeModal === 'confirm' || activeModal === 'deny'"
                  class="dev-switcher__modal-form"
                >
                  <label
                    class="dev-switcher__modal-label"
                    for="dev-resolve-tag"
                  >Pending tag</label>
                  <select
                    id="dev-resolve-tag"
                    v-model="resolveTagId"
                    class="dev-switcher__input"
                  >
                    <option value="">
                      Select a pending tag
                    </option>
                    <option
                      v-for="tag in pendingTags"
                      :key="tag.id"
                      :value="tag.id"
                    >
                      {{ tag.hunterName }} → {{ tag.targetName }} ({{ tagStatusLabel(tag.status) }})
                    </option>
                  </select>
                  <p
                    v-if="!pendingTags.length"
                    class="dev-switcher__modal-hint"
                  >
                    No pending tags available.
                  </p>
                </div>

                <p
                  v-if="actionError"
                  class="dev-switcher__modal-error"
                  role="alert"
                >
                  {{ actionError }}
                </p>
                <p
                  v-if="actionSuccess"
                  class="dev-switcher__modal-success"
                  role="status"
                >
                  {{ actionSuccess }}
                </p>
              </template>
            </div>

            <footer class="dev-switcher__modal-footer">
              <button
                type="button"
                class="dev-switcher__modal-button dev-switcher__modal-button--secondary"
                @click="closeModal"
              >
                Cancel
              </button>
              <button
                v-if="activeModal === 'submit'"
                type="button"
                class="dev-switcher__modal-button"
                :disabled="actionLoading || modalDataLoading"
                @click="handleSubmitTag"
              >
                {{ actionLoading ? 'Submitting...' : 'Submit' }}
              </button>
              <button
                v-if="activeModal === 'confirm'"
                type="button"
                class="dev-switcher__modal-button"
                :disabled="actionLoading || modalDataLoading || !resolveTagId"
                @click="handleConfirmTag"
              >
                {{ actionLoading ? 'Confirming...' : 'Confirm' }}
              </button>
              <button
                v-if="activeModal === 'deny'"
                type="button"
                class="dev-switcher__modal-button dev-switcher__modal-button--danger"
                :disabled="actionLoading || modalDataLoading || !resolveTagId"
                @click="handleDenyTag"
              >
                {{ actionLoading ? 'Denying...' : 'Deny' }}
              </button>
            </footer>
          </div>
        </div>
      </Transition>
    </Teleport>
  </div>
</template>

<style scoped>
.dev-switcher {
  --dev-bg: rgba(15, 23, 42, 0.92);
  --dev-border: rgba(148, 163, 184, 0.2);
  --dev-text: #f8fafc;
  --dev-muted: #94a3b8;
  --dev-accent: #f59e0b;
  --dev-accent-hover: #d97706;
  --dev-danger: #f87171;
  --dev-radius: 0.875rem;

  bottom: 1rem;
  color: var(--dev-text);
  font-family: 'DM Sans', ui-sans-serif, system-ui, sans-serif;
  position: fixed;
  right: 1rem;
  z-index: 100;
}

.dev-switcher__toggle {
  align-items: center;
  background: var(--dev-bg);
  backdrop-filter: blur(8px);
  border: 1px solid var(--dev-border);
  border-radius: var(--dev-radius);
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.25);
  color: var(--dev-text);
  cursor: pointer;
  display: inline-flex;
  font-size: 0.875rem;
  font-weight: 700;
  gap: 0.5rem;
  min-height: 2.75rem;
  padding: 0.5rem 0.75rem;
  transition:
    background-color 0.15s ease,
    transform 0.15s ease;
  -webkit-tap-highlight-color: transparent;
}

.dev-switcher__toggle:hover {
  background: rgba(15, 23, 42, 0.98);
}

.dev-switcher__toggle:active {
  transform: scale(0.96);
}

.dev-switcher__icon {
  font-size: 1.125rem;
  line-height: 1;
}

.dev-switcher__panel {
  background: var(--dev-bg);
  backdrop-filter: blur(8px);
  border: 1px solid var(--dev-border);
  border-radius: var(--dev-radius);
  box-shadow: 0 20px 40px rgba(0, 0, 0, 0.35);
  margin-top: 0.75rem;
  max-height: 70vh;
  max-width: 22rem;
  min-width: 18rem;
  overflow-y: auto;
  padding: 1rem;
  width: calc(100vw - 2rem);
}

.dev-switcher__section + .dev-switcher__section {
  border-top: 1px solid var(--dev-border);
  margin-top: 1rem;
  padding-top: 1rem;
}

.dev-switcher__row {
  align-items: center;
  display: flex;
  justify-content: space-between;
  gap: 0.75rem;
}

.dev-switcher__label {
  color: var(--dev-accent);
  display: block;
  font-size: 0.75rem;
  font-weight: 700;
  letter-spacing: 0.05em;
  margin: 0 0 0.5rem;
  text-transform: uppercase;
}

.dev-switcher__name {
  font-size: 1rem;
  font-weight: 700;
  margin: 0;
}

.dev-switcher__email,
.dev-switcher__muted,
.dev-switcher__player-email {
  color: var(--dev-muted);
  font-size: 0.8125rem;
  margin: 0.125rem 0 0;
}

.dev-switcher__input {
  background: rgba(30, 41, 59, 0.8);
  border: 1px solid var(--dev-border);
  border-radius: 0.625rem;
  color: var(--dev-text);
  font: inherit;
  font-size: 0.875rem;
  margin-bottom: 0.5rem;
  min-height: 2.25rem;
  padding: 0.5rem 0.75rem;
  width: 100%;
}

.dev-switcher__input:focus {
  border-color: var(--dev-accent);
  box-shadow: 0 0 0 2px rgba(245, 158, 11, 0.25);
  outline: none;
}

.dev-switcher__button {
  background: var(--dev-accent);
  border: 0;
  border-radius: 0.625rem;
  color: #0f172a;
  cursor: pointer;
  font: inherit;
  font-size: 0.875rem;
  font-weight: 700;
  min-height: 2.25rem;
  padding: 0.5rem 0.75rem;
  transition: background-color 0.15s ease;
  width: 100%;
}

.dev-switcher__button:hover:not(:disabled) {
  background: var(--dev-accent-hover);
}

.dev-switcher__button:disabled {
  cursor: not-allowed;
  opacity: 0.6;
}

.dev-switcher__text-button {
  background: transparent;
  border: 0;
  color: var(--dev-muted);
  cursor: pointer;
  font: inherit;
  font-size: 0.75rem;
  padding: 0;
}

.dev-switcher__text-button:hover {
  color: var(--dev-text);
}

.dev-switcher__list {
  display: grid;
  gap: 0.5rem;
  list-style: none;
  margin: 0;
  padding: 0;
}

.dev-switcher__player {
  align-items: center;
  background: rgba(30, 41, 59, 0.6);
  border-radius: 0.625rem;
  display: flex;
  gap: 0.75rem;
  justify-content: space-between;
  padding: 0.5rem 0.5rem 0.5rem 0.75rem;
}

.dev-switcher__player-info {
  display: grid;
  gap: 0.125rem;
  min-width: 0;
}

.dev-switcher__player-name {
  font-size: 0.875rem;
  font-weight: 600;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.dev-switcher__player-email {
  margin: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.dev-switcher__switch-button {
  background: rgba(245, 158, 11, 0.15);
  border: 1px solid rgba(245, 158, 11, 0.3);
  border-radius: 0.5rem;
  color: var(--dev-accent);
  cursor: pointer;
  flex-shrink: 0;
  font: inherit;
  font-size: 0.75rem;
  font-weight: 700;
  min-height: 2rem;
  padding: 0 0.625rem;
  transition: background-color 0.15s ease;
}

.dev-switcher__switch-button:hover {
  background: rgba(245, 158, 11, 0.25);
}

.dev-switcher__error {
  color: var(--dev-danger);
  font-size: 0.8125rem;
  margin: 1rem 0 0;
}

.dev-switcher__success {
  color: #86efac;
  font-size: 0.8125rem;
  margin: 0.75rem 0 0;
}

.dev-switcher__button--secondary {
  background: rgba(30, 41, 59, 0.8);
  border: 1px solid var(--dev-border);
  color: var(--dev-text);
}

.dev-switcher__button--secondary:hover:not(:disabled) {
  background: rgba(51, 65, 85, 0.9);
}

.dev-switcher__label--small {
  color: var(--dev-muted);
  font-size: 0.6875rem;
  text-transform: none;
}

.dev-switcher__quick-actions {
  display: grid;
  gap: 0.5rem;
  grid-template-columns: repeat(2, 1fr);
}

.dev-switcher__quick-button {
  background: rgba(30, 41, 59, 0.8);
  border: 1px solid var(--dev-border);
  border-radius: 0.625rem;
  color: var(--dev-text);
  cursor: pointer;
  font: inherit;
  font-size: 0.8125rem;
  font-weight: 600;
  min-height: 2.25rem;
  padding: 0.5rem;
  transition: background-color 0.15s ease;
}

.dev-switcher__quick-button:hover:not(:disabled) {
  background: rgba(51, 65, 85, 0.9);
}

.dev-switcher__quick-button:disabled {
  cursor: not-allowed;
  opacity: 0.5;
}

.dev-switcher__quick-button--danger {
  border-color: rgba(248, 113, 113, 0.4);
  color: var(--dev-danger);
}

.dev-switcher__quick-button--danger:hover:not(:disabled) {
  background: rgba(248, 113, 113, 0.15);
}

.dev-switcher__modal-backdrop {
  align-items: center;
  background: rgba(15, 23, 42, 0.75);
  backdrop-filter: blur(2px);
  display: flex;
  inset: 0;
  justify-content: center;
  padding: 1rem;
  position: fixed;
  z-index: 200;
}

.dev-switcher__modal {
  background: var(--dev-bg);
  border: 1px solid var(--dev-border);
  border-radius: var(--dev-radius);
  box-shadow: 0 20px 40px rgba(0, 0, 0, 0.4);
  max-height: 80vh;
  max-width: 24rem;
  overflow-y: auto;
  width: calc(100vw - 2rem);
}

.dev-switcher__modal-header {
  align-items: center;
  border-bottom: 1px solid var(--dev-border);
  display: flex;
  justify-content: space-between;
  padding: 0.875rem 1rem;
}

.dev-switcher__modal-title {
  color: var(--dev-text);
  font-family: 'DM Sans', ui-sans-serif, sans-serif;
  font-size: 1rem;
  font-weight: 700;
  margin: 0;
}

.dev-switcher__modal-close {
  align-items: center;
  background: transparent;
  border: 0;
  border-radius: 0.5rem;
  color: var(--dev-muted);
  cursor: pointer;
  display: flex;
  font-size: 1.25rem;
  height: 2rem;
  justify-content: center;
  padding: 0;
  width: 2rem;
}

.dev-switcher__modal-close:hover {
  background: rgba(30, 41, 59, 0.8);
  color: var(--dev-text);
}

.dev-switcher__modal-body {
  padding: 1rem;
}

.dev-switcher__modal-loading {
  color: var(--dev-muted);
  font-size: 0.875rem;
  margin: 0;
}

.dev-switcher__modal-form {
  display: grid;
  gap: 0.75rem;
}

.dev-switcher__modal-form .dev-switcher__input {
  margin-bottom: 0;
}

.dev-switcher__modal-label {
  color: var(--dev-accent);
  display: block;
  font-size: 0.75rem;
  font-weight: 700;
  letter-spacing: 0.05em;
  text-transform: uppercase;
}

.dev-switcher__modal-hint {
  color: var(--dev-muted);
  font-size: 0.8125rem;
  margin: 0;
}

.dev-switcher__modal-error {
  color: var(--dev-danger);
  font-size: 0.8125rem;
  margin: 0.75rem 0 0;
}

.dev-switcher__modal-success {
  color: #86efac;
  font-size: 0.8125rem;
  margin: 0.75rem 0 0;
}

.dev-switcher__modal-footer {
  border-top: 1px solid var(--dev-border);
  display: flex;
  gap: 0.75rem;
  justify-content: flex-end;
  padding: 0.875rem 1rem;
}

.dev-switcher__modal-button {
  background: var(--dev-accent);
  border: 0;
  border-radius: 0.625rem;
  color: #0f172a;
  cursor: pointer;
  font: inherit;
  font-size: 0.875rem;
  font-weight: 700;
  min-height: 2.25rem;
  padding: 0.5rem 1rem;
  transition: background-color 0.15s ease;
}

.dev-switcher__modal-button:hover:not(:disabled) {
  background: var(--dev-accent-hover);
}

.dev-switcher__modal-button:disabled {
  cursor: not-allowed;
  opacity: 0.6;
}

.dev-switcher__modal-button--secondary {
  background: rgba(30, 41, 59, 0.8);
  border: 1px solid var(--dev-border);
  color: var(--dev-text);
}

.dev-switcher__modal-button--secondary:hover:not(:disabled) {
  background: rgba(51, 65, 85, 0.9);
}

.dev-switcher__modal-button--danger {
  background: rgba(248, 113, 113, 0.9);
  color: #0f172a;
}

.dev-switcher__modal-button--danger:hover:not(:disabled) {
  background: #f87171;
}

.dev-modal-enter-active,
.dev-modal-leave-active {
  transition: opacity 0.2s ease;
}

.dev-modal-enter-from,
.dev-modal-leave-to {
  opacity: 0;
}

@media (max-width: 640px) {
  .dev-switcher {
    bottom: 0;
    left: 0;
    right: 0;
  }

  .dev-switcher__toggle {
    border-bottom-left-radius: 0;
    border-bottom-right-radius: 0;
    bottom: 0;
    margin: 0 0.75rem;
    position: absolute;
    right: 0;
  }

  .dev-switcher__panel {
    border-bottom-left-radius: 0;
    border-bottom-right-radius: 0;
    margin: 0;
    max-height: 60vh;
    max-width: none;
    min-width: auto;
    width: 100%;
  }

  .dev-switcher--expanded .dev-switcher__toggle {
    border-bottom: 0;
    border-bottom-left-radius: 0;
    border-bottom-right-radius: 0;
  }
}

@media (min-width: 641px) {
  .dev-switcher--expanded .dev-switcher__toggle {
    border-bottom-left-radius: 0;
    border-bottom-right-radius: 0;
  }

  .dev-switcher--expanded .dev-switcher__panel {
    border-top-left-radius: 0;
  }
}
</style>
