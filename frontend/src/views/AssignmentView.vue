<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'

import Button from '@/components/Button.vue'
import BackButton from '@/components/BackButton.vue'
import AssignmentCooldownTimer from '@/components/AssignmentCooldownTimer.vue'
import ConditionCard from '@/components/ConditionCard.vue'
import Modal from '@/components/Modal.vue'
import PendingTagCountdown from '@/components/PendingTagCountdown.vue'
import { useGameSignalR } from '@/composables/useSignalR'
import { useToast } from '@/composables/useToast'
import { useAssignmentStore, useGameStore, useTagStore } from '@/stores'
import { GameStatus } from '@/types'
import { parseTimeSpan } from '@/utils/format'

const route = useRoute()
const router = useRouter()
const gameStore = useGameStore()
const assignmentStore = useAssignmentStore()
const tagStore = useTagStore()

const gameId = computed(() => route.params.id as string)
const selectedConditionId = ref<string | null>(null)
const submitModalOpen = ref(false)
const { toast } = useToast()
const isReady = ref(false)

const cooldownAvailableAt = computed(() => {
  const availableAt = assignmentStore.nextAvailability?.availableAt
  if (!availableAt) return null
  // Add 30 seconds buffer to account for background service delay
  const bufferedTime = new Date(availableAt).getTime() + 5000
  return bufferedTime > Date.now() ? new Date(bufferedTime).toISOString() : null
})

/**
 * When the hunter has a pending outgoing tag, compute when it auto-confirms:
 * submittedAt + confirmationTimeout + 5 seconds buffer.
 */
const pendingAvailableAt = computed(() => {
  const tag = tagStore.pendingOutgoingTag
  if (!tagStore.isTagPending(tag)) return null
  const timeout = gameStore.currentGame?.confirmationTimeout
  if (!timeout) return null
  const parts = parseTimeSpan(timeout)
  if (!parts) return null
  const timeoutMs = (parts.days * 24 * 60 + parts.hours * 60 + parts.minutes) * 60 * 1000
  const submittedMs = new Date(tag.submittedAt).getTime()
  if (Number.isNaN(submittedMs)) return null
  // Add 5 seconds buffer to account for background service delay
  return new Date(submittedMs + timeoutMs + 5000).toISOString()
})

useGameSignalR(gameId.value)

// Watch for assignment/pending tag changes from SignalR and refresh.
// Watch stable values (assignment id, pending tag reference) instead of the
// whole objects: refreshAssignment() re-fetches the assignment, and replacing
// currentAssignment with a new-but-identical object would re-trigger a watch
// on the object reference and loop forever.
watch(
  () => assignmentStore.currentAssignment?.id ?? null,
  () => {
    void refreshAssignment()
  },
)

watch(
  () => tagStore.pendingOutgoingTag,
  (newTag, oldTag) => {
    // Only refresh assignment when the tag is resolved (cleared), not when it's submitted
    if (oldTag && !newTag) {
      void refreshAssignment()
    }
  },
)

onMounted(async () => {
  // Load all data in parallel so the template renders the correct state
  // directly instead of flashing through intermediate states.
  const [, , pendingTagResult] = await Promise.all([
    gameStore.loadGame(gameId.value),
    refreshAssignment(),
    tagStore.loadPendingOutgoingTag(gameId.value).catch(() => undefined),
  ])
  void pendingTagResult
  isReady.value = true
})

async function refreshAssignment() {
  try {
    const assignment = await assignmentStore.loadAssignment(gameId.value)
    if (!assignment) {
      await assignmentStore.loadNextAvailability(gameId.value)
    }
  } catch {
    // No active assignment — load the cooldown
    await assignmentStore.loadNextAvailability(gameId.value).catch(() => undefined)
  }
}

async function onCooldownExpired() {
  // The cooldown elapsed — the backend should have created the assignment.
  // Just reload to get the new assignment.
  try {
    await refreshAssignment()
  } catch {
    // Assignment not ready yet — reload the cooldown so the timer keeps ticking
    await assignmentStore.loadNextAvailability(gameId.value).catch(() => undefined)
  }
}

async function onPendingTagExpired() {
  // The tag auto-confirmed — the backend should have processed it.
  // Just reload to get the new assignment or cooldown.
  tagStore.clearPendingOutgoingTag()

  try {
    await refreshAssignment()
  } catch {
    // Assignment not ready yet — fall back to the cooldown timer
    await assignmentStore.loadNextAvailability(gameId.value).catch(() => undefined)
  }
}

function selectCondition(conditionId: string) {
  selectedConditionId.value = conditionId
  submitModalOpen.value = true
}

async function onSubmitTag() {
  if (!assignmentStore.currentAssignment || !selectedConditionId.value) return
  try {
    await tagStore.submitTag(
      gameId.value,
      assignmentStore.currentAssignment.id,
      selectedConditionId.value,
    )
    submitModalOpen.value = false
    selectedConditionId.value = null
  } catch (err) {
    if (err instanceof Error) {
      toast(err.message, 'error')
    }
  }
}
</script>

<template>
  <section class="page-section">
    <BackButton
      :label="$t('common.back')"
      @click="router.push(`/games/${gameId}`)"
    />
    <div v-if="!isReady" class="loading">
      {{ $t('assignment.loading') }}
    </div>

    <div v-else-if="pendingAvailableAt" class="empty">
      <PendingTagCountdown :available-at="pendingAvailableAt" @expired="onPendingTagExpired" />
    </div>

    <div v-else-if="assignmentStore.currentAssignment">
      <p class="eyebrow">
        {{ $t('assignment.eyebrow') }}
      </p>
      <div class="target-card">
        <div class="target-avatar">
          <img
            v-if="assignmentStore.currentAssignment.target.avatarUrl"
            :src="assignmentStore.currentAssignment.target.avatarUrl"
            :alt="assignmentStore.currentAssignment.target.displayName"
          />
          <span v-else>{{
            assignmentStore.currentAssignment.target.displayName.charAt(0).toUpperCase()
          }}</span>
        </div>
        <div>
          <h1 class="target-name">
            {{ assignmentStore.currentAssignment.target.displayName }}
          </h1>
          <p class="target-hint">
            {{ $t('assignment.hint') }}
          </p>
        </div>
      </div>

      <div class="conditions-section">
        <h2>{{ $t('assignment.conditions') }}</h2>
        <div class="conditions-list">
          <ConditionCard
            v-for="condition in assignmentStore.currentAssignment.conditions"
            :key="condition.id"
            :condition="condition"
            selectable
            :selected="selectedConditionId === condition.id"
            @select="selectCondition"
          />
        </div>
      </div>

      <Button variant="secondary" full-width @click="router.push(`/games/${gameId}/leaderboard`)">
        {{ $t('assignment.viewLeaderboard') }}
      </Button>
    </div>

    <div v-else-if="assignmentStore.isLoading" class="loading">
      {{ $t('assignment.loading') }}
    </div>
    <div v-else class="empty">
      <AssignmentCooldownTimer
        v-if="cooldownAvailableAt"
        :available-at="cooldownAvailableAt"
        @expired="onCooldownExpired"
      />
      <template v-else>
        <p>{{ $t('assignment.noAssignment') }}</p>
        <p v-if="gameStore.currentGame?.status !== GameStatus.Active" class="empty-hint">
          {{ $t('assignment.gameNotStarted') }}
        </p>
        <Button @click="router.push(`/games/${gameId}`)">
          {{ $t('common.backToGame') }}
        </Button>
      </template>
    </div>

    <Modal
      :open="submitModalOpen"
      :title="$t('assignment.confirmTag.title')"
      @close="submitModalOpen = false"
    >
      <p>{{ $t('assignment.confirmTag.message') }}</p>
      <template #footer>
        <Button variant="secondary" @click="submitModalOpen = false">
          {{ $t('common.cancel') }}
        </Button>
        <Button :loading="tagStore.isLoading" @click="onSubmitTag">
          {{ $t('assignment.confirmTag.submit') }}
        </Button>
      </template>
    </Modal>
  </section>
</template>

<style scoped>
.target-card {
  align-items: center;
  background: linear-gradient(135deg, var(--primary) 0%, var(--primary-dark) 100%);
  border-radius: 1.25rem;
  box-shadow: 0 12px 32px rgba(23, 37, 84, 0.25);
  color: var(--text-inverse);
  display: flex;
  gap: 1rem;
  margin-bottom: 1.5rem;
  padding: 1.5rem;
}

.target-avatar {
  align-items: center;
  background: var(--text-inverse);
  border-radius: 50%;
  color: var(--primary-dark);
  display: flex;
  flex-shrink: 0;
  font-size: 1.5rem;
  font-weight: 700;
  height: 4rem;
  justify-content: center;
  overflow: hidden;
  width: 4rem;
}

.target-avatar img {
  height: 100%;
  object-fit: cover;
  width: 100%;
}

.target-name {
  font-size: 1.5rem;
  margin: 0;
}

.target-hint {
  margin: 0.25rem 0 0;
  opacity: 0.9;
}

.conditions-section {
  margin-bottom: 1.5rem;
}

.conditions-section h2 {
  font-size: 1.125rem;
  margin: 0 0 0.75rem;
}

.conditions-list {
  display: grid;
  gap: 0.75rem;
}

.submit-modal p {
  color: var(--text-muted);
  margin: 0 0 1rem;
}

.loading,
.empty {
  color: var(--text-muted);
  padding: 2rem 0;
  text-align: center;
}

.empty p {
  margin: 0 0 1rem;
}

.empty-hint {
  color: var(--text-faint);
  font-size: 0.875rem;
}
</style>
