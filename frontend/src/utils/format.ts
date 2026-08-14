/**
 * Translate function compatible with vue-i18n's `t`.
 * Accepts a named-params record for interpolation.
 */
export type Translate = (key: string, params?: Record<string, unknown>) => string

export interface TimeSpanParts {
  days: number
  hours: number
  minutes: number
}

/**
 * Parses a .NET TimeSpan string (e.g. "00:05:00", "01:30:00", "30:00")
 * into its day/hour/minute parts. Returns null for invalid input.
 */
export function parseTimeSpan(value: string): TimeSpanParts | null {
  const match = /^(?:(\d+)\.)?(?:(\d{1,2}):)?(\d{1,2}):(\d{2})$/.exec(value.trim())
  if (!match) return null
  return {
    days: match[1] ? Number(match[1]) : 0,
    hours: match[2] ? Number(match[2]) : 0,
    minutes: match[3] ? Number(match[3]) : 0,
  }
}

/**
 * Converts a TimeSpan string to a total number of minutes.
 * Returns null for invalid input.
 */
export function timeSpanToMinutes(value: string): number | null {
  const parts = parseTimeSpan(value)
  if (!parts) return null
  return parts.days * 24 * 60 + parts.hours * 60 + parts.minutes
}

/**
 * Formats a plain minute count as a localized duration string,
 * e.g. 5 -> "5 minutes", 1 -> "1 minute".
 */
export function formatMinutes(minutes: number, t: Translate): string {
  if (minutes === 1) {
    return t('gameDetail.minutesSingular')
  }
  return t('gameDetail.minutes', { count: minutes })
}

/**
 * Formats a .NET TimeSpan string (e.g. "00:05:00") as a human-readable,
 * localized duration, e.g. "5 minutes", "1 hour 30 minutes".
 * Returns the raw value unchanged when it cannot be parsed.
 */
export function formatTimeout(value: string, t: Translate): string {
  const parts = parseTimeSpan(value)
  if (!parts) return value

  const hours = parts.days * 24 + parts.hours
  const minutes = parts.minutes

  if (hours > 0 && minutes > 0) {
    if (minutes === 1) {
      return t('gameDetail.hoursMinutesSingular', { hours })
    }
    return t('gameDetail.hoursMinutes', { hours, minutes })
  }
  if (hours > 0) {
    if (hours === 1) {
      return t('gameDetail.hoursSingular')
    }
    return t('gameDetail.hours', { count: hours })
  }
  return formatMinutes(minutes, t)
}
