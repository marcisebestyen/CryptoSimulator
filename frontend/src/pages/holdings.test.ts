import { describe, expect, it } from 'vitest'
import type { OwnedCrypto } from '../api/users'
import { initials, sharePercent, sortByValue, toSlices, totalValue } from './holdings'

const holding = (cryptoName: string, currentValue: number): OwnedCrypto => ({
  cryptoId: 1,
  cryptoName,
  amount: 1,
  currentPrice: currentValue,
  currentValue,
})

describe('toSlices', () => {
  it('keeps only holdings with value above zero', () => {
    expect(toSlices([holding('Bitcoin', 200), holding('Ether', 0)])).toEqual([
      { name: 'Bitcoin', value: 200, color: expect.any(String) },
    ])
  })
})

describe('totalValue', () => {
  it('sums holding values', () => {
    expect(totalValue([holding('A', 10), holding('B', 5.5)])).toBe(15.5)
  })

  it('is zero for no holdings', () => {
    expect(totalValue([])).toBe(0)
  })
})

describe('sharePercent', () => {
  it('returns the percentage of the total', () => {
    expect(sharePercent(25, 100)).toBe(25)
  })

  it('returns 0 when the total is 0', () => {
    expect(sharePercent(0, 0)).toBe(0)
  })
})

describe('sortByValue', () => {
  it('orders by value descending without mutating the input', () => {
    const input = [holding('A', 1), holding('B', 3), holding('C', 2)]
    expect(sortByValue(input).map((h) => h.cryptoName)).toEqual(['B', 'C', 'A'])
    expect(input[0].cryptoName).toBe('A')
  })
})

describe('initials', () => {
  it('uses the first letters of both names, uppercase', () => {
    expect(initials('jane', 'doe')).toBe('JD')
  })

  it('falls back to U when both names are blank', () => {
    expect(initials(' ', '')).toBe('U')
  })

  it('works with one name only', () => {
    expect(initials('jane', '')).toBe('J')
  })
})
