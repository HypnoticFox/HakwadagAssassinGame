export enum GameStatus {
  NotStarted = 0,
  Active = 1,
  Ended = 2,
}

export enum GameRole {
  Player = 0,
  Creator = 1,
  CoAdmin = 2,
}

export enum TagStatus {
  Pending = 0,
  Confirmed = 1,
  Denied = 2,
  Voided = 3,
}

export enum ConditionType {
  WithSpecificPerson = 0,
  Alone = 1,
  WithXPeople = 2,
  MundaneAction = 3,
  Custom = 4,
}

export interface PlayerDto {
  id: string
  email: string
  displayName: string
  avatarUrl?: string
}

export interface AuthResponse {
  token: string
  player: PlayerDto
}

export interface SafeTimeBlockDto {
  id: string
  startTime: string
  endTime: string
  day?: number
}

export interface GameDto {
  id: string
  name: string
  inviteCode: string
  status: GameStatus
  createdAt: string
  scheduledEndAt?: string
  endedAt?: string
  maxPlayers: number
  basePointsPerTag: number
  confirmationTimeout: string
  playerCount: number
  myRole: GameRole
  safeTimeBlocks: SafeTimeBlockDto[]
}

export interface TargetDto {
  id: string
  displayName: string
  avatarUrl?: string
}

export interface ConditionDto {
  id: string
  type: ConditionType
  description: string
  targetPersonName?: string
  action?: string
  minPeople?: number
}

export interface AssignmentDto {
  id: string
  target: TargetDto
  conditions: ConditionDto[]
  assignedAt: string
}

export interface PushSubscriptionDto {
  endpoint: string
  p256dh: string
  auth: string
}

export interface TagSubmissionDto {
  id: string
  assignmentId: string
  hunterId: string
  targetId: string
  conditionId: string
  status: TagStatus
  submittedAt: string
  resolvedAt?: string
}

export interface LeaderboardEntryDto {
  player: PlayerDto
  score: number
  tags: number
}

export interface CreateGameRequest {
  name: string
  durationHours: number
  maxPlayers?: number
  basePointsPerTag: number
  confirmationTimeoutMinutes: number
  conditionBonuses?: Record<ConditionType, number>
  safeTimeBlocks?: SafeTimeBlockDto[]
}

export interface SubmitTagRequest {
  assignmentId: string
  conditionId: string
}

export interface PushSubscriptionDto {
  endpoint: string
  p256dh: string
  auth: string
}

export interface GameEvent {
  gameId: string
  type:
    | 'ScoreUpdated'
    | 'TagSubmitted'
    | 'TagResolved'
    | 'GameStarted'
    | 'GameEnded'
    | 'AssignmentChanged'
    | 'PlayerLeft'
  payload?: Record<string, unknown>
}

export function gameStatusLabel(status: GameStatus): string {
  switch (status) {
    case GameStatus.NotStarted:
      return 'Not started'
    case GameStatus.Active:
      return 'Active'
    case GameStatus.Ended:
      return 'Ended'
  }
}

export function gameRoleLabel(role: GameRole): string {
  switch (role) {
    case GameRole.Player:
      return 'Player'
    case GameRole.Creator:
      return 'Creator'
    case GameRole.CoAdmin:
      return 'Co-admin'
  }
}

export function tagStatusLabel(status: TagStatus): string {
  switch (status) {
    case TagStatus.Pending:
      return 'Pending'
    case TagStatus.Confirmed:
      return 'Confirmed'
    case TagStatus.Denied:
      return 'Denied'
    case TagStatus.Voided:
      return 'Voided'
  }
}

export function conditionTypeLabel(type: ConditionType): string {
  switch (type) {
    case ConditionType.WithSpecificPerson:
      return 'With specific person'
    case ConditionType.Alone:
      return 'Alone'
    case ConditionType.WithXPeople:
      return 'With group'
    case ConditionType.MundaneAction:
      return 'During action'
    case ConditionType.Custom:
      return 'Custom'
  }
}

export function isGameAdmin(role: GameRole): boolean {
  return role === GameRole.Creator || role === GameRole.CoAdmin
}

export function canStartGame(role: GameRole, status: GameStatus): boolean {
  return isGameAdmin(role) && status === GameStatus.NotStarted
}

export function canEndGame(role: GameRole, status: GameStatus): boolean {
  return isGameAdmin(role) && status === GameStatus.Active
}
