import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr'
import { onMounted, onUnmounted, ref } from 'vue'

import { api } from '@/api/client'
import { useAssignmentStore, useAuthStore, useGameStore, useLeaderboardStore, useTagStore } from '@/stores'
import type { GameDto, TagSubmissionDto } from '@/types'

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000'

export function useSignalR() {
  const connection = ref<HubConnection | null>(null)
  const isConnected = ref(false)
  const error = ref<string | null>(null)

  const gameStore = useGameStore()
  const assignmentStore = useAssignmentStore()
  const tagStore = useTagStore()
  const leaderboardStore = useLeaderboardStore()
  const authStore = useAuthStore()

  async function start() {
    if (connection.value) {
      return
    }

    const token = api.getToken()
    if (!token) {
      return
    }

    const hub = new HubConnectionBuilder()
      .withUrl(`${API_URL}/hubs/game`, {
        accessTokenFactory: () => token,
      })
      .configureLogging(LogLevel.Information)
      .withAutomaticReconnect()
      .build()

    hub.on('ScoreUpdated', (gameId: string) => {
      void leaderboardStore.loadLeaderboard(gameId)
    })

    hub.on('TagSubmitted', (gameId: string, tag: TagSubmissionDto) => {
      if (gameStore.currentGame?.id === gameId) {
        // Check if current player is the hunter or target
        const currentPlayerId = authStore.player?.id
        if (currentPlayerId === tag.hunterId) {
          // Current player is the hunter - set pending outgoing tag
          tagStore.setPendingOutgoingTag(tag)
        } else if (currentPlayerId === tag.targetId) {
          // Current player is the target - set pending tag
          tagStore.setPendingTag(tag)
        }
      }
      void leaderboardStore.loadLeaderboard(gameId)
    })

    hub.on('TagResolved', (gameId: string, tag: TagSubmissionDto) => {
      if (gameStore.currentGame?.id === gameId) {
        // Check if current player is the hunter or target
        const currentPlayerId = authStore.player?.id
        if (currentPlayerId === tag.hunterId) {
          // Current player is the hunter - clear pending outgoing tag and reload assignment
          tagStore.clearPendingOutgoingTag()
          void assignmentStore.loadAssignment(gameId)
        } else if (currentPlayerId === tag.targetId) {
          // Current player is the target - clear pending tag
          tagStore.setPendingTag(null)
        }
      }
      void leaderboardStore.loadLeaderboard(gameId)
    })

    hub.on('GameStarted', (gameId: string, game: GameDto) => {
      if (gameStore.currentGame?.id === gameId) {
        gameStore.setGame(game)
      }
      void assignmentStore.loadAssignment(gameId)
    })

    hub.on('GameEnded', (gameId: string, game: GameDto) => {
      if (gameStore.currentGame?.id === gameId) {
        gameStore.setGame(game)
      }
    })

    hub.on('AssignmentChanged', (gameId: string) => {
      if (gameStore.currentGame?.id === gameId) {
        void assignmentStore.loadAssignment(gameId)
      }
    })

    hub.on('PlayerLeft', (gameId: string) => {
      if (gameStore.currentGame?.id === gameId) {
        void gameStore.loadGame(gameId)
      }
    })

    hub.onclose(() => {
      isConnected.value = false
    })

    hub.onreconnecting(() => {
      isConnected.value = false
    })

    hub.onreconnected(() => {
      isConnected.value = true
    })

    try {
      await hub.start()
      connection.value = hub
      isConnected.value = true
    } catch (err) {
      if (err instanceof Error) {
        error.value = err.message
      }
    }
  }

  async function stop() {
    if (connection.value) {
      await connection.value.stop()
      connection.value = null
      isConnected.value = false
    }
  }

  async function joinGame(gameId: string) {
    if (connection.value?.state === 'Connected') {
      await connection.value.invoke('JoinGame', gameId)
    }
  }

  async function leaveGame(gameId: string) {
    if (connection.value?.state === 'Connected') {
      await connection.value.invoke('LeaveGame', gameId)
    }
  }

  onMounted(() => {
    void start()
  })

  onUnmounted(() => {
    void stop()
  })

  return {
    connection,
    isConnected,
    error,
    start,
    stop,
    joinGame,
    leaveGame,
  }
}

export function useGameSignalR(gameId: string) {
  const signalR = useSignalR()

  onMounted(async () => {
    await signalR.start()
    await signalR.joinGame(gameId)
  })

  onUnmounted(async () => {
    await signalR.leaveGame(gameId)
  })

  return signalR
}
