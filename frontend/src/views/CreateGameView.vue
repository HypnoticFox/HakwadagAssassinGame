<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'

import Button from '@/components/Button.vue'
import Input from '@/components/Input.vue'
import { useGameStore } from '@/stores'

const router = useRouter()
const gameStore = useGameStore()

const name = ref('')
const durationHours = ref('24')
const maxPlayers = ref('20')
const basePointsPerTag = ref('100')
const confirmationTimeoutMinutes = ref('5')
const localError = ref<string | null>(null)

async function onSubmit() {
  localError.value = null
  try {
    const game = await gameStore.createGame({
      name: name.value,
      durationHours: Number(durationHours.value),
      maxPlayers: maxPlayers.value ? Number(maxPlayers.value) : undefined,
      basePointsPerTag: Number(basePointsPerTag.value),
      confirmationTimeoutMinutes: Number(confirmationTimeoutMinutes.value),
    })
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
    <p class="eyebrow">
      New game
    </p>
    <h1>Create a game</h1>
    <p>Set the rules for your group's tagging game.</p>

    <form
      class="create-form"
      @submit.prevent="onSubmit"
    >
      <Input
        v-model="name"
        label="Game name"
        placeholder="Friday Night Assassin"
        required
      />
      <Input
        v-model="durationHours"
        label="Duration (hours)"
        type="number"
        inputmode="numeric"
        min="1"
        required
      />
      <Input
        v-model="maxPlayers"
        label="Max players"
        type="number"
        inputmode="numeric"
        min="2"
      />
      <Input
        v-model="basePointsPerTag"
        label="Base points per tag"
        type="number"
        inputmode="numeric"
        min="1"
        required
      />
      <Input
        v-model="confirmationTimeoutMinutes"
        label="Confirmation timeout (minutes)"
        type="number"
        inputmode="numeric"
        min="1"
        required
      />

      <p
        v-if="localError || gameStore.error"
        class="form-error"
        role="alert"
      >
        {{ localError || gameStore.error }}
      </p>

      <Button
        type="submit"
        size="large"
        full-width
        :loading="gameStore.isLoading"
      >
        Create game
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

.form-error {
  background: #fef2f2;
  border-radius: 0.5rem;
  color: #991b1b;
  font-size: 0.875rem;
  margin: 0;
  padding: 0.75rem;
}
</style>
