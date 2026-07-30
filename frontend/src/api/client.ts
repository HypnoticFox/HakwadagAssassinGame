import type {
  AssignmentDto,
  AuthResponse,
  ConditionDto,
  CreateGameRequest,
  DevAssignment,
  DevGame,
  DevPlayer,
  DevTag,
  GameDto,
  LeaderboardEntryDto,
  PlayerDto,
  SafeTimeBlockDto,
  SubmitTagRequest,
  TagSubmissionDto,
} from '@/types'

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000'

export class ApiError extends Error {
  constructor(
    message: string,
    public status: number,
  ) {
    super(message)
    this.name = 'ApiError'
  }
}

class ApiClient {
  private token: string | null = null

  constructor() {
    this.token = localStorage.getItem('hakwadag_token')
  }

  setToken(token: string) {
    this.token = token
    localStorage.setItem('hakwadag_token', token)
  }

  clearToken() {
    this.token = null
    localStorage.removeItem('hakwadag_token')
  }

  getToken() {
    return this.token
  }

  private buildUrl(path: string) {
    return `${API_URL}${path}`
  }

  private async request<T>(path: string, options: RequestInit = {}): Promise<T> {
    const headers = new Headers(options.headers)
    headers.set('Accept', 'application/json')
    headers.set('skip_zrok_interstitial', '1')

    if (options.body && typeof options.body === 'string') {
      headers.set('Content-Type', 'application/json')
    }

    if (this.token) {
      headers.set('Authorization', `Bearer ${this.token}`)
    }

    const response = await fetch(this.buildUrl(path), {
      ...options,
      headers,
    })

    if (response.status === 204) {
      return undefined as T
    }

    if (response.status === 404) {
      return null as T
    }

    if (!response.ok) {
      let message = `Request failed with status ${response.status}`
      try {
        const errorBody = await response.json()
        if (errorBody && typeof errorBody === 'object') {
          message =
            (errorBody as Record<string, unknown>).message?.toString() ||
            (errorBody as Record<string, unknown>).error?.toString() ||
            message
        }
      } catch {
        // ignore parse error
      }
      throw new ApiError(message, response.status)
    }

    const text = await response.text()
    if (!text) {
      return undefined as T
    }
    return JSON.parse(text) as T
  }

  async sendOtp(email: string): Promise<void> {
    await this.request('/api/auth/send-otp', {
      method: 'POST',
      body: JSON.stringify({ email }),
    })
  }

  async verifyOtp(email: string, code: string): Promise<AuthResponse> {
    return this.request<AuthResponse>('/api/auth/verify-otp', {
      method: 'POST',
      body: JSON.stringify({ email, code }),
    })
  }

  async devLogin(email?: string): Promise<AuthResponse> {
    return this.request<AuthResponse>('/api/auth/dev-login', {
      method: 'POST',
      body: JSON.stringify(email ? { email } : {}),
    })
  }

  async seedGame(playerCount?: number): Promise<{
    game: GameDto
    players: Array<{ player: PlayerDto; token: string }>
  }> {
    return this.request('/api/dev/seed-game', {
      method: 'POST',
      body: JSON.stringify(playerCount ? { playerCount } : {}),
    })
  }

  async devGetGames(): Promise<DevGame[]> {
    return this.request<DevGame[]>('/api/dev/games')
  }

  async devGetGamePlayers(gameId: string): Promise<DevPlayer[]> {
    return this.request<DevPlayer[]>(`/api/dev/games/${gameId}/players`)
  }

  async devGetGameAssignments(gameId: string): Promise<DevAssignment[]> {
    return this.request<DevAssignment[]>(`/api/dev/games/${gameId}/assignments`)
  }

  async devGetGameTags(gameId: string): Promise<DevTag[]> {
    return this.request<DevTag[]>(`/api/dev/games/${gameId}/tags`)
  }

  async devSubmitTag(
    gameId: string,
    playerId: string,
    assignmentId: string,
    conditionId: string,
  ): Promise<TagSubmissionDto> {
    return this.request<TagSubmissionDto>(`/api/dev/games/${gameId}/submit-tag`, {
      method: 'POST',
      body: JSON.stringify({ playerId, assignmentId, conditionId }),
    })
  }

  async devConfirmTag(tagId: string): Promise<TagSubmissionDto> {
    return this.request<TagSubmissionDto>(`/api/dev/tags/${tagId}/confirm`, {
      method: 'POST',
    })
  }

  async devDenyTag(tagId: string): Promise<TagSubmissionDto> {
    return this.request<TagSubmissionDto>(`/api/dev/tags/${tagId}/deny`, {
      method: 'POST',
    })
  }

  async devEndGame(gameId: string): Promise<GameDto> {
    return this.request<GameDto>(`/api/dev/games/${gameId}/end`, {
      method: 'POST',
    })
  }

  async me(): Promise<PlayerDto> {
    return this.request<PlayerDto>('/api/auth/me')
  }

  async createGame(request: CreateGameRequest): Promise<GameDto> {
    return this.request<GameDto>('/api/games', {
      method: 'POST',
      body: JSON.stringify(request),
    })
  }

  async getMyGames(): Promise<GameDto[]> {
    return this.request<GameDto[]>('/api/games')
  }

  async getGame(gameId: string): Promise<GameDto> {
    return this.request<GameDto>(`/api/games/${gameId}`)
  }

  async joinGame(inviteCode: string, displayName: string): Promise<GameDto> {
    return this.request<GameDto>(`/api/games/join/${encodeURIComponent(inviteCode)}`, {
      method: 'POST',
      body: JSON.stringify({ displayName }),
    })
  }

  async startGame(gameId: string): Promise<GameDto> {
    return this.request<GameDto>(`/api/games/${gameId}/start`, {
      method: 'POST',
    })
  }

  async endGame(gameId: string): Promise<GameDto> {
    return this.request<GameDto>(`/api/games/${gameId}/end`, {
      method: 'POST',
    })
  }

  async setParticipation(gameId: string, isParticipating: boolean): Promise<void> {
    await this.request<void>(`/api/games/${gameId}/participation`, {
      method: 'PUT',
      body: JSON.stringify({ isParticipating }),
    })
  }

  async leaveGame(gameId: string): Promise<void> {
    await this.request<void>(`/api/games/${gameId}/leave`, {
      method: 'POST',
    })
  }

  async rejoinGame(gameId: string): Promise<GameDto> {
    return this.request<GameDto>(`/api/games/${gameId}/rejoin`, {
      method: 'POST',
    })
  }

  async getMyAssignment(gameId: string): Promise<AssignmentDto | null> {
    return this.request<AssignmentDto | null>(`/api/games/${gameId}/assignments/me`)
  }

  async submitTag(gameId: string, request: SubmitTagRequest): Promise<TagSubmissionDto> {
    return this.request<TagSubmissionDto>(`/api/games/${gameId}/tag`, {
      method: 'POST',
      body: JSON.stringify(request),
    })
  }

  async getPendingTag(gameId: string): Promise<TagSubmissionDto | null> {
    return this.request<TagSubmissionDto | null>(`/api/games/${gameId}/tag/pending`)
  }

  async confirmTag(gameId: string, tagId: string): Promise<TagSubmissionDto> {
    return this.request<TagSubmissionDto>(`/api/games/${gameId}/tag/${tagId}/confirm`, {
      method: 'POST',
    })
  }

  async denyTag(gameId: string, tagId: string): Promise<TagSubmissionDto> {
    return this.request<TagSubmissionDto>(`/api/games/${gameId}/tag/${tagId}/deny`, {
      method: 'POST',
    })
  }

  async voidTag(gameId: string, tagId: string): Promise<TagSubmissionDto> {
    return this.request<TagSubmissionDto>(`/api/games/${gameId}/tag/${tagId}/void`, {
      method: 'POST',
    })
  }

  async getLeaderboard(gameId: string): Promise<LeaderboardEntryDto[]> {
    return this.request<LeaderboardEntryDto[]>(`/api/games/${gameId}/leaderboard`)
  }

  async addAdmin(gameId: string, playerId: string): Promise<void> {
    await this.request<void>(`/api/games/${gameId}/admins`, {
      method: 'POST',
      body: JSON.stringify({ playerId }),
    })
  }

  async removeAdmin(gameId: string, playerId: string): Promise<void> {
    await this.request<void>(`/api/games/${gameId}/admins/${playerId}`, {
      method: 'DELETE',
    })
  }

  async addSafeTime(gameId: string, block: Omit<SafeTimeBlockDto, 'id'>): Promise<string> {
    const response = await this.request<{ blockId: string }>(`/api/games/${gameId}/safe-times`, {
      method: 'POST',
      body: JSON.stringify(block),
    })
    return response.blockId
  }

  async removeSafeTime(gameId: string, blockId: string): Promise<void> {
    await this.request<void>(`/api/games/${gameId}/safe-times/${blockId}`, {
      method: 'DELETE',
    })
  }

  async addCondition(gameId: string, description: string): Promise<ConditionDto> {
    return this.request<ConditionDto>(`/api/games/${gameId}/conditions`, {
      method: 'POST',
      body: JSON.stringify({ description }),
    })
  }

  async getVapidPublicKey(): Promise<string> {
    const response = await this.request<{ publicKey: string }>('/api/push/vapid-public-key')
    return response.publicKey
  }

  async subscribePush(subscription: PushSubscription): Promise<void> {
    const keys = subscription.toJSON().keys
    if (!keys?.p256dh || !keys?.auth) {
      throw new ApiError('Invalid push subscription keys', 400)
    }

    await this.request<void>('/api/push/subscribe', {
      method: 'POST',
      body: JSON.stringify({
        endpoint: subscription.endpoint,
        p256dh: keys.p256dh,
        auth: keys.auth,
      }),
    })
  }
}

export const api = new ApiClient()
