import { describe, it, expect } from 'vitest'
import { formatMinutes, formatTimeout, parseTimeSpan, timeSpanToMinutes } from '@/utils/format'
import type { Translate } from '@/utils/format'

// Mock translate function backed by the same templates as src/i18n/en.json
function mockTranslate(templates: Record<string, string>): Translate {
  return (key, params) => {
    const template = templates[key]
    if (template === undefined) return key
    return template.replace(/\{(\w+)\}/g, (_, name: string) => String(params?.[name] ?? ''))
  }
}

const en = mockTranslate({
  'gameDetail.minutes': '{count} minutes',
  'gameDetail.minutesSingular': '1 minute',
  'gameDetail.hours': '{count} hours',
  'gameDetail.hoursSingular': '1 hour',
  'gameDetail.hoursMinutes': '{hours} hour {minutes} minutes',
  'gameDetail.hoursMinutesSingular': '{hours} hour 1 minute',
})

describe('parseTimeSpan', () => {
  it('parses hours:minutes:seconds format', () => {
    expect(parseTimeSpan('00:05:00')).toEqual({ days: 0, hours: 0, minutes: 5 })
    expect(parseTimeSpan('01:30:00')).toEqual({ days: 0, hours: 1, minutes: 30 })
    expect(parseTimeSpan('00:01:00')).toEqual({ days: 0, hours: 0, minutes: 1 })
  })

  it('parses minutes:seconds format', () => {
    expect(parseTimeSpan('30:00')).toEqual({ days: 0, hours: 0, minutes: 30 })
  })

  it('parses day-prefixed format', () => {
    expect(parseTimeSpan('1.02:15:00')).toEqual({ days: 1, hours: 2, minutes: 15 })
  })

  it('returns null for invalid input', () => {
    expect(parseTimeSpan('')).toBeNull()
    expect(parseTimeSpan('not-a-timespan')).toBeNull()
    expect(parseTimeSpan('5')).toBeNull()
  })
})

describe('timeSpanToMinutes', () => {
  it('converts time span strings to total minutes', () => {
    expect(timeSpanToMinutes('00:05:00')).toBe(5)
    expect(timeSpanToMinutes('01:30:00')).toBe(90)
    expect(timeSpanToMinutes('1.00:00:00')).toBe(1440)
  })

  it('returns null for invalid input', () => {
    expect(timeSpanToMinutes('garbage')).toBeNull()
  })
})

describe('formatMinutes', () => {
  it('formats a single minute with the singular label', () => {
    expect(formatMinutes(1, en)).toBe('1 minute')
  })

  it('formats multiple minutes with the plural label', () => {
    expect(formatMinutes(30, en)).toBe('30 minutes')
  })
})

describe('formatTimeout', () => {
  it('formats 00:05:00 as "5 minutes"', () => {
    expect(formatTimeout('00:05:00', en)).toBe('5 minutes')
  })

  it('formats 01:30:00 as "1 hour 30 minutes"', () => {
    expect(formatTimeout('01:30:00', en)).toBe('1 hour 30 minutes')
  })

  it('formats 00:30:00 as "30 minutes"', () => {
    expect(formatTimeout('00:30:00', en)).toBe('30 minutes')
  })

  it('formats a single minute with the singular label', () => {
    expect(formatTimeout('00:01:00', en)).toBe('1 minute')
  })

  it('formats whole hours without a zero-minute suffix', () => {
    expect(formatTimeout('01:00:00', en)).toBe('1 hour')
    expect(formatTimeout('02:00:00', en)).toBe('2 hours')
  })

  it('formats one hour with one minute using the singular minutes label', () => {
    expect(formatTimeout('01:01:00', en)).toBe('1 hour 1 minute')
  })

  it('accounts for days in the total hour count', () => {
    expect(formatTimeout('1.00:30:00', en)).toBe('24 hour 30 minutes')
  })

  it('returns the raw value unchanged when it cannot be parsed', () => {
    expect(formatTimeout('nope', en)).toBe('nope')
  })
})
