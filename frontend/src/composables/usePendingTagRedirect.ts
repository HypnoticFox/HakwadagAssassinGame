import { watch } from 'vue'
import { useRouter } from 'vue-router'

import { api } from '@/api/client'
import { useGameStore, useTagStore } from '@/stores'
import { GameStatus, TagStatus } from '@/types'

export function usePendingTagRedirect() {
  const router = useRouter()
  const gameStore = useGameStore()
  const tagStore = useTagStore()

  async function checkPendingTags() {
    if (router.currentRoute.value.name === 'tag-confirm') {
      return
    }

    const activeGames = gameStore.recentGames.filter((game) => game.status === GameStatus.Active)

    for (const game of activeGames) {
      try {
        const tag = await api.getPendingTag(game.id)
        if (tag && tag.status === TagStatus.Pending) {
          tagStore.setPendingTag(tag)
          await router.push(`/games/${game.id}/tag/${tag.id}`)
          return
        }
      } catch {
        // Ignore failures for individual games and continue checking.
      }
    }
  }

  watch(
    () => tagStore.pendingTagQueue.length,
    (length) => {
      if (length > 0 && router.currentRoute.value.name !== 'tag-confirm') {
        const next = tagStore.dequeuePendingTag()
        if (next) {
          void router.push(`/games/${next.gameId}/tag/${next.tag.id}`)
        }
      }
    },
  )

  return { checkPendingTags }
}
