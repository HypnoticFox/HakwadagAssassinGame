<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'

import Button from '@/components/Button.vue'
import BackButton from '@/components/BackButton.vue'
import Input from '@/components/Input.vue'
import { useToast } from '@/composables/useToast'
import { useGameStore } from '@/stores'

const router = useRouter()
const gameStore = useGameStore()

const name = ref('')
const durationHours = ref('')
const maxPlayers = ref('20')
const basePointsPerTag = ref('100')
const confirmationTimeoutMinutes = ref('5')
const assignmentCooldownMinutes = ref('30')
const { toast } = useToast()

async function onSubmit() {
  try {
    const game = await gameStore.createGame({
      name: name.value,
      durationHours: durationHours.value ? Number(durationHours.value) : undefined,
      maxPlayers: maxPlayers.value ? Number(maxPlayers.value) : undefined,
      basePointsPerTag: Number(basePointsPerTag.value),
      confirmationTimeoutMinutes: Number(confirmationTimeoutMinutes.value),
      assignmentCooldownMinutes: assignmentCooldownMinutes.value
        ? Number(assignmentCooldownMinutes.value)
        : undefined,
    })
    await router.push(`/games/${game.id}`)
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
      @click="router.push('/')"
    />
    <p class="eyebrow">
      {{ $t('createGame.eyebrow') }}
    </p>
    <h1>{{ $t('createGame.title') }}</h1>
    <p>{{ $t('createGame.subtitle') }}</p>

    <form
      class="create-form"
      @submit.prevent="onSubmit"
    >
      <Input
        v-model="name"
        :label="$t('createGame.gameName')"
        :placeholder="$t('createGame.gameNamePlaceholder')"
        required
      />
      <Input
        v-model="durationHours"
        :label="$t('createGame.duration')"
        type="number"
        inputmode="numeric"
        min="1"
      />
      <Input
        v-model="maxPlayers"
        :label="$t('createGame.maxPlayers')"
        type="number"
        inputmode="numeric"
        min="2"
      />
      <Input
        v-model="basePointsPerTag"
        :label="$t('createGame.basePointsPerTag')"
        type="number"
        inputmode="numeric"
        min="1"
        required
      />
      <Input
        v-model="confirmationTimeoutMinutes"
        :label="$t('createGame.confirmationTimeout')"
        type="number"
        inputmode="numeric"
        min="1"
        required
      />
      <Input
        v-model="assignmentCooldownMinutes"
        :label="$t('createGame.assignmentCooldown')"
        type="number"
        inputmode="numeric"
        min="0"
      />

      <Button
        type="submit"
        size="large"
        full-width
        :loading="gameStore.isLoading"
      >
        {{ $t('createGame.create') }}
      </Button>
    </form>
  </section>
</template>

<style scoped>
.create-form {
  display: grid;
  gap: 1rem;
  margin-top: 1.5rem;
}
</style>
