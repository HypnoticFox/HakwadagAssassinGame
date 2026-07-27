import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr'
import { onMounted, onUnmounted, ref } from 'vue'

import { api } from '@/api/client'
import { useAssignmentStore, useGameStore, useLeaderboardStore, useTagStore } from '@/stores'
import type { AssignmentDto, GameDto, TagSubmissionDto } from '@/types'

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000'

export function useSignalR() {
  const connection = ref<HubConnection | null>(null)
  const isConnected = ref(false)
  const error = ref<string | null>(null)

  const gameStore = useGameStore()
  const assignmentStore = useAssignmentStore()
  const tagStore = useTagStore()
  const leaderboardStore = useLeaderboardStore()

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
        tagStore.setPendingTag(tag)
      }
      void leaderboardStore.loadLeaderboard(gameId)
    })

    hub.on('TagResolved', (gameId: string, tag: TagSubmissionDto) => {
      if (gameStore.currentGame?.id === gameId) {
        tagStore.setPendingTag(tag)
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

    hub.on('AssignmentChanged', (gameId: string, assignment: AssignmentDto) => {
      if (gameStore.currentGame?.id === gameId) {
        assignmentStore.setAssignment(assignment)
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
