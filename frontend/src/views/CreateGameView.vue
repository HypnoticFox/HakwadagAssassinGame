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
        required
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

.form-error {
  background: var(--danger-bg);
  border-radius: 0.5rem;
  color: var(--danger-text);
  font-size: 0.875rem;
  margin: 0;
  padding: 0.75rem;
}
</style>
