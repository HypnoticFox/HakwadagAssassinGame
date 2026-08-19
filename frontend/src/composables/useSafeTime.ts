import { onUnmounted, ref, watch, type Ref } from 'vue'

import type { SafeTimeBlockDto } from '@/types'

/** Parses an ISO 8601 DateTimeOffset string and returns the time-of-day and offset in seconds. */
function parseDateTimeOffset(iso: string): { timeOfDaySec: number; offsetSec: number } | null {
  // Parse "2025-06-15T22:00:00+02:00" or "2025-06-15T22:00:00Z"
  const match = /^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2}):(\d{2})(?:([+-])(\d{2}):(\d{2})|Z)$/.exec(iso)
  if (!match) return null

  const hours = parseInt(match[4], 10)
  const minutes = parseInt(match[5], 10)
  const seconds = parseInt(match[6], 10)

  let offsetSec: number
  if (match[7]) {
    const offsetHours = parseInt(match[8], 10)
    const offsetMinutes = parseInt(match[9], 10)
    offsetSec = (offsetHours * 3600 + offsetMinutes * 60) * (match[7] === '+' ? 1 : -1)
  } else {
    offsetSec = 0
  }

  const timeOfDaySec = hours * 3600 + minutes * 60 + seconds
  return { timeOfDaySec, offsetSec }
}

/** Formats a DateTimeOffset ISO string as "HH:MM" in the viewer's local timezone. */
export function formatTimeOfDay(iso: string): string {
  const date = new Date(iso)
  if (isNaN(date.getTime())) return iso
  const hours = String(date.getHours()).padStart(2, '0')
  const minutes = String(date.getMinutes()).padStart(2, '0')
  return `${hours}:${minutes}`
}

/** Converts a local time input ("HH:MM" from <input type="time">) to an ISO 8601 DateTimeOffset string with the browser's timezone offset. */
export function localTimeToDateTimeOffset(time: string): string {
  const [hours, minutes] = time.split(':').map(Number)
  if (isNaN(hours) || isNaN(minutes)) return time

  const now = new Date()
  const date = new Date(now.getFullYear(), now.getMonth(), now.getDate(), hours, minutes, 0)

  const pad = (n: number) => String(n).padStart(2, '0')
  const offsetMinutes = -date.getTimezoneOffset()
  const offsetSign = offsetMinutes >= 0 ? '+' : '-'
  const offsetH = pad(Math.floor(Math.abs(offsetMinutes) / 60))
  const offsetM = pad(Math.abs(offsetMinutes) % 60)

  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(hours)}:${pad(minutes)}:00${offsetSign}${offsetH}:${offsetM}`
}

export function useSafeTime(blocks: Ref<SafeTimeBlockDto[]>) {
  const isInSafeTime = ref(false)
  const currentBlock = ref<SafeTimeBlockDto | null>(null)
  let timer: ReturnType<typeof setTimeout> | null = null

  function clearTimer() {
    if (timer !== null) {
      clearTimeout(timer)
      timer = null
    }
  }

  function evaluate() {
    clearTimer()
    const now = new Date()
    const nowUtcSec = now.getUTCHours() * 3600 + now.getUTCMinutes() * 60 + now.getUTCSeconds()

    let active: SafeTimeBlockDto | null = null
    let activeEndsInSec = 0
    let nextStartsInSec = Infinity

    for (const block of blocks.value) {
      const startInfo = parseDateTimeOffset(block.startTime)
      const endInfo = parseDateTimeOffset(block.endTime)
      if (!startInfo || !endInfo) continue

      // Convert block times to UTC time-of-day by subtracting the offset
      const startUtcSec = ((startInfo.timeOfDaySec - startInfo.offsetSec) % 86400 + 86400) % 86400
      const endUtcSec = ((endInfo.timeOfDaySec - endInfo.offsetSec) % 86400 + 86400) % 86400

      const inBlock = startUtcSec <= endUtcSec
        ? nowUtcSec >= startUtcSec && nowUtcSec < endUtcSec
        : nowUtcSec >= startUtcSec || nowUtcSec < endUtcSec

      if (inBlock) {
        let endsInSec: number
        if (startUtcSec <= endUtcSec) {
          endsInSec = endUtcSec - nowUtcSec
        } else {
          endsInSec = nowUtcSec >= startUtcSec ? (86400 - nowUtcSec) + endUtcSec : endUtcSec - nowUtcSec
        }
        if (active === null || endsInSec > activeEndsInSec) {
          active = block
          activeEndsInSec = endsInSec
        }
      } else {
        let startsInSec: number
        if (startUtcSec <= endUtcSec) {
          startsInSec = nowUtcSec < startUtcSec ? startUtcSec - nowUtcSec : (86400 - nowUtcSec) + startUtcSec
        } else {
          startsInSec = startUtcSec - nowUtcSec
        }
        if (startsInSec < nextStartsInSec) {
          nextStartsInSec = startsInSec
        }
      }
    }

    isInSafeTime.value = active !== null
    currentBlock.value = active

    const nextBoundarySec = active !== null ? activeEndsInSec : nextStartsInSec
    if (nextBoundarySec < Infinity && nextBoundarySec > 0) {
      timer = setTimeout(evaluate, (nextBoundarySec + 1) * 1000)
    }
  }

  watch(blocks, evaluate, { immediate: true })
  onUnmounted(clearTimer)

  return { isInSafeTime, currentBlock }
}
