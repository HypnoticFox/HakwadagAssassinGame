import { describe, it, expect, beforeAll } from 'vitest'
import {
  GameStatus,
  GameRole,
  TagStatus,
  ConditionType,
  gameStatusLabel,
  gameRoleLabel,
  tagStatusLabel,
  conditionTypeLabel,
  isGameAdmin,
  canStartGame,
  canEndGame,
} from '@/types'
import { i18n } from '@/i18n'

beforeAll(() => {
  i18n.global.locale.value = 'en'
})

describe('gameStatusLabel', () => {
  it('returns "Not started" for NotStarted', () => {
    expect(gameStatusLabel(GameStatus.NotStarted)).toBe('Not started')
  })

  it('returns "Active" for Active', () => {
    expect(gameStatusLabel(GameStatus.Active)).toBe('Active')
  })

  it('returns "Ended" for Ended', () => {
    expect(gameStatusLabel(GameStatus.Ended)).toBe('Ended')
  })
})

describe('gameRoleLabel', () => {
  it('returns "Player" for Player', () => {
    expect(gameRoleLabel(GameRole.Player)).toBe('Player')
  })

  it('returns "Creator" for Creator', () => {
    expect(gameRoleLabel(GameRole.Creator)).toBe('Creator')
  })

  it('returns "Co-admin" for CoAdmin', () => {
    expect(gameRoleLabel(GameRole.CoAdmin)).toBe('Co-admin')
  })
})

describe('tagStatusLabel', () => {
  it('returns "Pending" for Pending', () => {
    expect(tagStatusLabel(TagStatus.Pending)).toBe('Pending')
  })

  it('returns "Confirmed" for Confirmed', () => {
    expect(tagStatusLabel(TagStatus.Confirmed)).toBe('Confirmed')
  })

  it('returns "Denied" for Denied', () => {
    expect(tagStatusLabel(TagStatus.Denied)).toBe('Denied')
  })

  it('returns "Voided" for Voided', () => {
    expect(tagStatusLabel(TagStatus.Voided)).toBe('Voided')
  })
})

describe('conditionTypeLabel', () => {
  it('returns "With specific person" for WithSpecificPerson', () => {
    expect(conditionTypeLabel(ConditionType.WithSpecificPerson)).toBe('With specific person')
  })

  it('returns "Alone" for Alone', () => {
    expect(conditionTypeLabel(ConditionType.Alone)).toBe('Alone')
  })

  it('returns "With group" for WithXPeople', () => {
    expect(conditionTypeLabel(ConditionType.WithXPeople)).toBe('With group')
  })

  it('returns "During action" for MundaneAction', () => {
    expect(conditionTypeLabel(ConditionType.MundaneAction)).toBe('During action')
  })

  it('returns "Custom" for Custom', () => {
    expect(conditionTypeLabel(ConditionType.Custom)).toBe('Custom')
  })
})

describe('isGameAdmin', () => {
  it('returns true for Creator', () => {
    expect(isGameAdmin(GameRole.Creator)).toBe(true)
  })

  it('returns true for CoAdmin', () => {
    expect(isGameAdmin(GameRole.CoAdmin)).toBe(true)
  })

  it('returns false for Player', () => {
    expect(isGameAdmin(GameRole.Player)).toBe(false)
  })
})

describe('canStartGame', () => {
  it('returns true for admin with NotStarted game', () => {
    expect(canStartGame(GameRole.Creator, GameStatus.NotStarted)).toBe(true)
  })

  it('returns false for admin with Active game', () => {
    expect(canStartGame(GameRole.Creator, GameStatus.Active)).toBe(false)
  })

  it('returns false for admin with Ended game', () => {
    expect(canStartGame(GameRole.Creator, GameStatus.Ended)).toBe(false)
  })

  it('returns false for non-admin regardless of status', () => {
    expect(canStartGame(GameRole.Player, GameStatus.NotStarted)).toBe(false)
  })
})

describe('canEndGame', () => {
  it('returns true for admin with Active game', () => {
    expect(canEndGame(GameRole.Creator, GameStatus.Active)).toBe(true)
  })

  it('returns false for admin with NotStarted game', () => {
    expect(canEndGame(GameRole.Creator, GameStatus.NotStarted)).toBe(false)
  })

  it('returns false for admin with Ended game', () => {
    expect(canEndGame(GameRole.Creator, GameStatus.Ended)).toBe(false)
  })

  it('returns false for non-admin even when game is Active', () => {
    expect(canEndGame(GameRole.Player, GameStatus.Active)).toBe(false)
  })
})
