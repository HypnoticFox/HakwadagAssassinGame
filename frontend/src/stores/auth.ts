import { defineStore } from 'pinia'
import { computed, ref } from 'vue'

import { api } from '@/api/client'
import type { PlayerDto } from '@/types'

export const useAuthStore = defineStore('auth', () => {
  const token = ref<string | null>(api.getToken())
  const player = ref<PlayerDto | null>(null)
  const isLoading = ref(false)
  const error = ref<string | null>(null)

  const isAuthenticated = computed(() => !!token.value && !!player.value)

  async function loadFromStorage() {
    if (!token.value) return
    isLoading.value = true
    error.value = null
    try {
      player.value = await api.me()
    } catch (err) {
      if (err instanceof Error) {
        error.value = err.message
      }
      token.value = null
      api.clearToken()
    } finally {
      isLoading.value = false
    }
  }

  async function sendOtp(email: string) {
    isLoading.value = true
    error.value = null
    try {
      await api.sendOtp(email)
    } catch (err) {
      if (err instanceof Error) {
        error.value = err.message
      }
      throw err
    } finally {
      isLoading.value = false
    }
  }

  async function verifyOtp(email: string, code: string) {
    isLoading.value = true
    error.value = null
    try {
      const response = await api.verifyOtp(email, code)
      token.value = response.token
      player.value = response.player
      api.setToken(response.token)
    } catch (err) {
      if (err instanceof Error) {
        error.value = err.message
      }
      throw err
    } finally {
      isLoading.value = false
    }
  }

  function logout() {
    token.value = null
    player.value = null
    api.clearToken()
    localStorage.removeItem('hakwadag_recent_games')
  }

  function setPlayer(value: PlayerDto) {
    player.value = value
  }

  return {
    token,
    player,
    isLoading,
    error,
    isAuthenticated,
    loadFromStorage,
    sendOtp,
    verifyOtp,
    logout,
    setPlayer,
  }
})
