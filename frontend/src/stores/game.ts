import { defineStore } from 'pinia'
import { computed, ref } from 'vue'

import { api } from '@/api/client'
import { GameStatus, type GameDto } from '@/types'

export interface RecentGame {
  id: string
  name: string
  status: GameStatus
  myRole: number
  joinedAt: string
}

const RECENT_GAMES_KEY = 'hakwadag_recent_games'

function readRecentGames(): RecentGame[] {
  try {
    const stored = localStorage.getItem(RECENT_GAMES_KEY)
    if (!stored) return []
    return JSON.parse(stored) as RecentGame[]
  } catch {
    return []
  }
}

function writeRecentGames(games: RecentGame[]) {
  localStorage.setItem(RECENT_GAMES_KEY, JSON.stringify(games.slice(0, 10)))
}

export const useGameStore = defineStore('game', () => {
  const currentGame = ref<GameDto | null>(null)
  const recentGames = ref<RecentGame[]>(readRecentGames())
  const isLoading = ref(false)
  const error = ref<string | null>(null)

  const isActive = computed(() => currentGame.value?.status === GameStatus.Active)
  const isCreator = computed(() => currentGame.value?.myRole === 1)
  const isAdmin = computed(() => {
    if (!currentGame.value) return false
    return currentGame.value.myRole === 1 || currentGame.value.myRole === 2
  })

  function addToRecentGames(game: GameDto) {
    const entry: RecentGame = {
      id: game.id,
      name: game.name,
      status: game.status,
      myRole: game.myRole,
      joinedAt: new Date().toISOString(),
    }
    const existing = recentGames.value.filter((g) => g.id !== game.id)
    recentGames.value = [entry, ...existing]
    writeRecentGames(recentGames.value)
  }

  function updateCurrentGame(game: GameDto) {
    currentGame.value = game
    addToRecentGames(game)
  }

  async function createGame(request: {
    name: string
    durationHours: number
    maxPlayers?: number
    basePointsPerTag: number
    confirmationTimeoutMinutes: number
  }) {
    isLoading.value = true
    error.value = null
    try {
      const game = await api.createGame({
        name: request.name,
        durationHours: request.durationHours,
        maxPlayers: request.maxPlayers,
        basePointsPerTag: request.basePointsPerTag,
        confirmationTimeoutMinutes: request.confirmationTimeoutMinutes,
      })
      updateCurrentGame(game)
      return game
    } catch (err) {
      if (err instanceof Error) {
        error.value = err.message
      }
      throw err
    } finally {
      isLoading.value = false
    }
  }

  async function loadMyGames() {
    isLoading.value = true
    error.value = null
    try {
      const games = await api.getMyGames()
      recentGames.value = games.map((game) => ({
        id: game.id,
        name: game.name,
        status: game.status,
        myRole: game.myRole,
        joinedAt: game.createdAt,
      }))
      writeRecentGames(recentGames.value)
      return games
    } catch (err) {
      if (err instanceof Error) {
        error.value = err.message
      }
      throw err
    } finally {
      isLoading.value = false
    }
  }

  async function joinGame(inviteCode: string, displayName: string) {
    isLoading.value = true
    error.value = null
    try {
      const game = await api.joinGame(inviteCode, displayName)
      updateCurrentGame(game)
      return game
    } catch (err) {
      if (err instanceof Error) {
        error.value = err.message
      }
      throw err
    } finally {
      isLoading.value = false
    }
  }

  async function loadGame(gameId: string) {
    isLoading.value = true
    error.value = null
    try {
      const game = await api.getGame(gameId)
      if (game) {
        updateCurrentGame(game)
      }
      return game
    } catch (err) {
      if (err instanceof Error) {
        error.value = err.message
      }
      throw err
    } finally {
      isLoading.value = false
    }
  }

  async function startGame(gameId: string) {
    isLoading.value = true
    error.value = null
    try {
      const game = await api.startGame(gameId)
      updateCurrentGame(game)
      return game
    } catch (err) {
      if (err instanceof Error) {
        error.value = err.message
      }
      throw err
    } finally {
      isLoading.value = false
    }
  }

  async function endGame(gameId: string) {
    isLoading.value = true
    error.value = null
    try {
      const game = await api.endGame(gameId)
      updateCurrentGame(game)
      return game
    } catch (err) {
      if (err instanceof Error) {
        error.value = err.message
      }
      throw err
    } finally {
      isLoading.value = false
    }
  }

  async function setParticipation(gameId: string, isParticipating: boolean) {
    isLoading.value = true
    error.value = null
    try {
      await api.setParticipation(gameId, isParticipating)
      if (currentGame.value) {
        await loadGame(gameId)
      }
    } catch (err) {
      if (err instanceof Error) {
        error.value = err.message
      }
      throw err
    } finally {
      isLoading.value = false
    }
  }

  async function leaveGame(gameId: string) {
    isLoading.value = true
    error.value = null
    try {
      const wasActive = currentGame.value?.status === GameStatus.Active
      await api.leaveGame(gameId)
      if (wasActive) {
        // Reload game to get updated participation status
        // Player remains a member, just not participating
        await loadGame(gameId)
      } else {
        currentGame.value = null
        recentGames.value = recentGames.value.filter((g) => g.id !== gameId)
        writeRecentGames(recentGames.value)
      }
    } catch (err) {
      if (err instanceof Error) {
        error.value = err.message
      }
      throw err
    } finally {
      isLoading.value = false
    }
  }

  async function rejoinGame(gameId: string) {
    isLoading.value = true
    error.value = null
    try {
      const game = await api.rejoinGame(gameId)
      updateCurrentGame(game)
      return game
    } catch (err) {
      if (err instanceof Error) {
        error.value = err.message
      }
      throw err
    } finally {
      isLoading.value = false
    }
  }

  async function addAdmin(gameId: string, playerId: string) {
    await api.addAdmin(gameId, playerId)
  }

  async function removeAdmin(gameId: string, playerId: string) {
    await api.removeAdmin(gameId, playerId)
  }

  async function addSafeTime(
    gameId: string,
    block: { startTime: string; endTime: string; day?: number },
  ) {
    await api.addSafeTime(gameId, block)
  }

  async function removeSafeTime(gameId: string, blockId: string) {
    await api.removeSafeTime(gameId, blockId)
  }

  async function addCondition(gameId: string, description: string) {
    return await api.addCondition(gameId, description)
  }

  function setGame(game: GameDto) {
    currentGame.value = game
    addToRecentGames(game)
  }

  return {
    currentGame,
    recentGames,
    isLoading,
    error,
    isActive,
    isCreator,
    isAdmin,
    createGame,
    loadMyGames,
    joinGame,
    loadGame,
    startGame,
    endGame,
    leaveGame,
    rejoinGame,
    setParticipation,
    addAdmin,
    removeAdmin,
    addSafeTime,
    removeSafeTime,
    addCondition,
    setGame,
  }
})
