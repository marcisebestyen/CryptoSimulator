export interface SessionStats {
  changePercent: number
  high: number
  low: number
}

export function sessionStats(values: number[]): SessionStats | null {
  if (values.length === 0) return null
  const first = values[0]
  const last = values[values.length - 1]
  return {
    changePercent: first === 0 ? 0 : ((last - first) / first) * 100,
    high: Math.max(...values),
    low: Math.min(...values),
  }
}

export function stepDeltaPercent(
  value: number,
  olderValue: number | undefined,
): number | null {
  if (olderValue === undefined || olderValue === 0) return null
  return ((value - olderValue) / olderValue) * 100
}

export function lastN<T>(items: T[], count: number | 'all'): T[] {
  if (count === 'all' || items.length <= count) return items
  return items.slice(items.length - count)
}
