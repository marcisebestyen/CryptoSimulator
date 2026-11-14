import { describe, expect, it } from 'vitest'
import { lastN, sessionStats, stepDeltaPercent } from './priceStats'

describe('sessionStats', () => {
  it('returns null for empty', () => {
    expect(sessionStats([])).toBeNull()
  })

  it('computes change, high, low', () => {
    expect(sessionStats([100, 150, 50, 120])).toEqual({
      changePercent: 20,
      high: 150,
      low: 50,
    })
  })

  it('gives 0 change when first value is 0', () => {
    expect(sessionStats([0, 10, 20])?.changePercent).toBe(0)
  })

  it('handles a single value', () => {
    expect(sessionStats([42])).toEqual({ changePercent: 0, high: 42, low: 42 })
  })
})

describe('stepDeltaPercent', () => {
  it('compares to older value', () => {
    expect(stepDeltaPercent(110, 100)).toBeCloseTo(10)
  })

  it('is null without older value', () => {
    expect(stepDeltaPercent(110, undefined)).toBeNull()
  })

  it('is null when older value is 0', () => {
    expect(stepDeltaPercent(110, 0)).toBeNull()
  })
})

describe('lastN', () => {
  it('keeps newest N', () => {
    expect(lastN([1, 2, 3, 4, 5], 2)).toEqual([4, 5])
  })

  it('returns all when fewer than N', () => {
    expect(lastN([1, 2], 20)).toEqual([1, 2])
  })

  it("returns all for 'all'", () => {
    expect(lastN([1, 2, 3], 'all')).toEqual([1, 2, 3])
  })
})
