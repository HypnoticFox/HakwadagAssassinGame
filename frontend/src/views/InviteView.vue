<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'

import { api } from '@/api/client'
import Button from '@/components/Button.vue'
import { useToast } from '@/composables/useToast'
import { useAuthStore, useGameStore } from '@/stores'

const route = useRoute()
const router = useRouter()
const { t } = useI18n()
const authStore = useAuthStore()
const gameStore = useGameStore()

const inviteCode = route.params.inviteCode as string
const joining = ref(true)
const { toast } = useToast()

onMounted(async () => {
  try {
    const game = await api.lookupGame(inviteCode)
    if (!game) {
      toast(t('invite.gameNotFound'), 'error')
      joining.value = false
      return
    }
    const displayName = authStore.player?.displayName || 'Player'
    try {
      await gameStore.joinGame(inviteCode, displayName)
    } catch (err) {
      const message = err instanceof Error ? err.message : ''
      if (!message.toLowerCase().includes('already in this game')) {
        throw err
      }
    }
    await router.push(`/games/${game.id}`)
  } catch (err) {
    toast(err instanceof Error ? err.message || t('invite.error') : t('invite.error'), 'error')
    joining.value = false
  }
})
</script>

<template>
  <section class="page-section">
    <div class="invite-status">
      <p
        v-if="joining"
        class="invite-loading"
      >
        {{ $t('invite.joining') }}
      </p>
      <template v-else>
        <Button @click="router.push('/')">
          {{ $t('common.backHome') }}
        </Button>
      </template>
    </div>
  </section>
</template>

<style scoped>
.invite-status {
  align-items: center;
  display: flex;
  flex-direction: column;
  gap: 1rem;
  justify-content: center;
  min-height: 60vh;
  padding: 2rem 1rem;
  text-align: center;
}

.invite-loading {
  color: var(--text-muted);
  margin: 0;
}
</style>
