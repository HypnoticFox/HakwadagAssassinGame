import { defineStore } from 'pinia'
import { ref } from 'vue'

import { api } from '@/api/client'
import type { AssignmentDto, NextAssignmentAvailabilityDto } from '@/types'

export const useAssignmentStore = defineStore('assignment', () => {
  const currentAssignment = ref<AssignmentDto | null>(null)
  const nextAvailability = ref<NextAssignmentAvailabilityDto | null>(null)
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
      // No active assignment — clear the stale one so the UI shows the cooldown
      currentAssignment.value = null
      if (err instanceof Error) {
        error.value = err.message
      }
      throw err
    } finally {
      isLoading.value = false
    }
  }

  async function loadNextAvailability(gameId: string) {
    isLoading.value = true
    error.value = null
    try {
      const availability = await api.getNextAssignmentAvailability(gameId)
      nextAvailability.value = availability
      return availability
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
    nextAvailability.value = null
  }

  return {
    currentAssignment,
    nextAvailability,
    isLoading,
    error,
    loadAssignment,
    loadNextAvailability,
    setAssignment,
    clearAssignment,
  }
})
