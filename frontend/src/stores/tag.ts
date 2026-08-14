import { defineStore } from 'pinia'
import { ref } from 'vue'

import { api } from '@/api/client'
import { TagStatus, type TagSubmissionDto } from '@/types'

export const useTagStore = defineStore('tag', () => {
  const pendingTag = ref<TagSubmissionDto | null>(null)
  const pendingOutgoingTag = ref<TagSubmissionDto | null>(null)
  const isLoading = ref(false)
  const error = ref<string | null>(null)

  async function loadPendingTag(gameId: string) {
    isLoading.value = true
    error.value = null
    try {
      const tag = await api.getPendingTag(gameId)
      pendingTag.value = tag
      return tag
    } catch (err) {
      if (err instanceof Error) {
        error.value = err.message
      }
      throw err
    } finally {
      isLoading.value = false
    }
  }

  async function loadPendingOutgoingTag(gameId: string) {
    isLoading.value = true
    error.value = null
    try {
      const tag = await api.getPendingOutgoingTag(gameId)
      pendingOutgoingTag.value = tag
      return tag
    } catch (err) {
      if (err instanceof Error) {
        error.value = err.message
      }
      throw err
    } finally {
      isLoading.value = false
    }
  }

  async function submitTag(gameId: string, assignmentId: string, conditionId: string) {
    isLoading.value = true
    error.value = null
    try {
      const tag = await api.submitTag(gameId, { assignmentId, conditionId })
      pendingTag.value = tag
      pendingOutgoingTag.value = tag
      return tag
    } catch (err) {
      if (err instanceof Error) {
        error.value = err.message
      }
      throw err
    } finally {
      isLoading.value = false
    }
  }

  async function confirmTag(gameId: string, tagId: string) {
    isLoading.value = true
    error.value = null
    try {
      const tag = await api.confirmTag(gameId, tagId)
      pendingTag.value = tag
      return tag
    } catch (err) {
      if (err instanceof Error) {
        error.value = err.message
      }
      throw err
    } finally {
      isLoading.value = false
    }
  }

  async function denyTag(gameId: string, tagId: string) {
    isLoading.value = true
    error.value = null
    try {
      const tag = await api.denyTag(gameId, tagId)
      pendingTag.value = tag
      return tag
    } catch (err) {
      if (err instanceof Error) {
        error.value = err.message
      }
      throw err
    } finally {
      isLoading.value = false
    }
  }

  async function voidTag(gameId: string, tagId: string) {
    isLoading.value = true
    error.value = null
    try {
      const tag = await api.voidTag(gameId, tagId)
      pendingTag.value = tag
      return tag
    } catch (err) {
      if (err instanceof Error) {
        error.value = err.message
      }
      throw err
    } finally {
      isLoading.value = false
    }
  }

  function setPendingTag(tag: TagSubmissionDto | null) {
    pendingTag.value = tag
  }

  function setPendingOutgoingTag(tag: TagSubmissionDto | null) {
    pendingOutgoingTag.value = tag
  }

  function clearPendingOutgoingTag() {
    pendingOutgoingTag.value = null
  }

  function isTagPending(tag: TagSubmissionDto | null): tag is TagSubmissionDto {
    return tag !== null && tag.status === TagStatus.Pending
  }

  return {
    pendingTag,
    pendingOutgoingTag,
    isLoading,
    error,
    loadPendingTag,
    loadPendingOutgoingTag,
    submitTag,
    confirmTag,
    denyTag,
    voidTag,
    setPendingTag,
    setPendingOutgoingTag,
    clearPendingOutgoingTag,
    isTagPending,
  }
})
