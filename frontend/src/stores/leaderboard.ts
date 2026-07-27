import { defineStore } from 'pinia'
import { ref } from 'vue'

import { api } from '@/api/client'
import type { LeaderboardEntryDto } from '@/types'

export const useLeaderboardStore = defineStore('leaderboard', () => {
  const entries = ref<LeaderboardEntryDto[]>([])
  const isLoading = ref(false)
  const error = ref<string | null>(null)

  async function loadLeaderboard(gameId: string) {
    isLoading.value = true
    error.value = null
    try {
      const result = await api.getLeaderboard(gameId)
      entries.value = result
      return result
    } catch (err) {
      if (err instanceof Error) {
        error.value = err.message
      }
      throw err
    } finally {
      isLoading.value = false
    }
  }

  function setEntries(value: LeaderboardEntryDto[]) {
    entries.value = value
  }

  return {
    entries,
    isLoading,
    error,
    loadLeaderboard,
    setEntries,
  }
})
