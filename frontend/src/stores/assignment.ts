import { defineStore } from 'pinia'
import { ref } from 'vue'

import { api } from '@/api/client'
import type { AssignmentDto } from '@/types'

export const useAssignmentStore = defineStore('assignment', () => {
  const currentAssignment = ref<AssignmentDto | null>(null)
  const isLoading = ref(false)
  const error = ref<string | null>(null)

  async function loadAssignment(gameId: string) {
    isLoading.value = true
    error.value = null
    try {
      const assignment = await api.getMyAssignment(gameId)
      currentAssignment.value = assignment
      return assignment
    } catch (err) {
      if (err instanceof Error) {
        error.value = err.message
      }
      throw err
    } finally {
      isLoading.value = false
    }
  }

  function setAssignment(assignment: AssignmentDto | null) {
    currentAssignment.value = assignment
  }

  function clearAssignment() {
    currentAssignment.value = null
  }

  return {
    currentAssignment,
    isLoading,
    error,
    loadAssignment,
    setAssignment,
    clearAssignment,
  }
})
