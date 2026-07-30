<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'

import Button from '@/components/Button.vue'
import ConditionCard from '@/components/ConditionCard.vue'
import Modal from '@/components/Modal.vue'
import { useGameSignalR } from '@/composables/useSignalR'
import { useAssignmentStore, useGameStore, useTagStore } from '@/stores'
import { GameStatus } from '@/types'

const route = useRoute()
const router = useRouter()
const gameStore = useGameStore()
const assignmentStore = useAssignmentStore()
const tagStore = useTagStore()

const gameId = computed(() => route.params.id as string)
const selectedConditionId = ref<string | null>(null)
const submitModalOpen = ref(false)
const localError = ref<string | null>(null)

useGameSignalR(gameId.value)

onMounted(async () => {
  await gameStore.loadGame(gameId.value)
  await assignmentStore.loadAssignment(gameId.value)
})

function selectCondition(conditionId: string) {
  selectedConditionId.value = conditionId
  submitModalOpen.value = true
}

async function onSubmitTag() {
  if (!assignmentStore.currentAssignment || !selectedConditionId.value) return
  localError.value = null
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
      localError.value = err.message
    }
  }
}
</script>

<template>
  <section class="page-section">
    <div v-if="assignmentStore.currentAssignment">
      <p class="eyebrow">
        {{ $t('assignment.eyebrow') }}
      </p>
      <div class="target-card">
        <div class="target-avatar">
          <img
            v-if="assignmentStore.currentAssignment.target.avatarUrl"
            :src="assignmentStore.currentAssignment.target.avatarUrl"
            :alt="assignmentStore.currentAssignment.target.displayName"
          >
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

      <Button
        variant="secondary"
        full-width
        @click="router.push(`/games/${gameId}/leaderboard`)"
      >
        {{ $t('assignment.viewLeaderboard') }}
      </Button>
    </div>

    <div
      v-else-if="assignmentStore.isLoading"
      class="loading"
    >
      {{ $t('assignment.loading') }}
    </div>
    <div
      v-else
      class="empty"
    >
      <p>{{ $t('assignment.noAssignment') }}</p>
      <p
        v-if="gameStore.currentGame?.status !== GameStatus.Active"
        class="empty-hint"
      >
        {{ $t('assignment.gameNotStarted') }}
      </p>
      <Button @click="router.push(`/games/${gameId}`)">
        {{ $t('common.backToGame') }}
      </Button>
    </div>

    <Modal
      :open="submitModalOpen"
      :title="$t('assignment.confirmTag.title')"
      @close="submitModalOpen = false"
    >
      <p>{{ $t('assignment.confirmTag.message') }}</p>
      <p
        v-if="localError"
        class="form-error"
        role="alert"
      >
        {{ localError }}
      </p>
      <template #footer>
        <Button
          variant="secondary"
          @click="submitModalOpen = false"
        >
          {{ $t('common.cancel') }}
        </Button>
        <Button
          :loading="tagStore.isLoading"
          @click="onSubmitTag"
        >
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

.form-error {
  background: var(--danger-bg);
  border-radius: 0.5rem;
  color: var(--danger-text);
  font-size: 0.875rem;
  margin: 0 0 1rem;
  padding: 0.75rem;
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
