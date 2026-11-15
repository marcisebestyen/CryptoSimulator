import { describe, expect, it } from 'vitest'
import type { MarketOffer, MyOffer } from '../api/offers'
import { filterAndSortOffers, lockedValue, offerTotal, priceTextError, vsMarketPct } from './offerMath'

const market = (over: Partial<MarketOffer>): MarketOffer => ({
  id: 1,
  sellerUsername: 'alice',
  sellerCompletionRate: 0.9,
  sellerTrades: 3,
  cryptoId: 1,
  cryptoName: 'Bitcoin',
  remainingAmount: 1,
  askingPrice: 100,
  currentRate: 100,
  ...over,
})
const mine = (over: Partial<MyOffer>): MyOffer => ({
  id: 1,
  cryptoId: 1,
  cryptoName: 'Bitcoin',
  amount: 2,
  remainingAmount: 2,
  soldAmount: 0,
  askingPrice: 100,
  currentRate: 50,
  status: 'Open',
  createdAt: '2026-01-01T00:00:00Z',
  ...over,
})

describe('vsMarketPct', () => {
  it('is negative when the offer is below the market rate', () => {
    expect(vsMarketPct(89750, 91400)).toBeCloseTo(-1.8, 1)
  })
  it('is positive when above the market rate', () => {
    expect(vsMarketPct(110, 100)).toBeCloseTo(10)
  })
})

describe('vsMarketPct without a rate', () => {
  it('returns 0 when the rate is not positive', () => {
    expect(vsMarketPct(100, 0)).toBe(0)
  })
})

describe('offerTotal', () => {
  it('rounds to 2 decimals, half away from zero', () => {
    expect(offerTotal(0.5, 0.01)).toBe(0.01)
    expect(offerTotal(1.005, 1)).toBe(1.01)
    expect(offerTotal(0.0001, 0.4)).toBe(0)
  })
})

describe('priceTextError', () => {
  it('accepts up to 8 decimals', () => {
    expect(priceTextError('0.00000001')).toBeNull()
  })
  it('rejects zero, too many decimals and huge prices', () => {
    expect(priceTextError('0')).not.toBeNull()
    expect(priceTextError('0.000000001')).not.toBeNull()
    expect(priceTextError('10000000000')).not.toBeNull()
    expect(priceTextError('abc')).not.toBeNull()
  })
})

describe('filterAndSortOffers', () => {
  const offers = [
    market({ id: 1, askingPrice: 105, sellerUsername: 'alice' }),
    market({ id: 2, askingPrice: 90, sellerUsername: 'bob' }),
    market({ id: 3, cryptoId: 2, cryptoName: 'Ether', askingPrice: 5, currentRate: 4, remainingAmount: 100 }),
  ]
  const base = { search: '', coin: 'all', sort: 'best-deal' as const }

  it('sorts best deal first by discount to market', () => {
    expect(filterAndSortOffers(offers, base).map((o) => o.id)).toEqual([2, 1, 3])
  })
  it('sorts by lowest price', () => {
    expect(filterAndSortOffers(offers, { ...base, sort: 'lowest-price' }).map((o) => o.id)).toEqual([3, 2, 1])
  })
  it('sorts by highest total', () => {
    expect(filterAndSortOffers(offers, { ...base, sort: 'highest-total' }).map((o) => o.id)).toEqual([3, 1, 2])
  })
  it('filters by coin id', () => {
    expect(filterAndSortOffers(offers, { ...base, coin: '2' }).map((o) => o.id)).toEqual([3])
  })
  it('searches coin and seller case-insensitively', () => {
    expect(filterAndSortOffers(offers, { ...base, search: 'BOB' }).map((o) => o.id)).toEqual([2])
    expect(filterAndSortOffers(offers, { ...base, search: 'ethe' }).map((o) => o.id)).toEqual([3])
  })
  it('does not mutate the input', () => {
    const copy = [...offers]
    filterAndSortOffers(offers, { ...base, sort: 'lowest-price' })
    expect(offers).toEqual(copy)
  })
})

describe('lockedValue', () => {
  it('sums remaining x current rate of open and partly sold offers only', () => {
    const offers = [
      mine({ id: 1, remainingAmount: 2, currentRate: 50 }),
      mine({ id: 2, status: 'PartlySold', remainingAmount: 1, currentRate: 10 }),
      mine({ id: 3, status: 'SoldOut', remainingAmount: 0 }),
      mine({ id: 4, status: 'Cancelled', remainingAmount: 5 }),
    ]
    expect(lockedValue(offers)).toBe(110)
  })
})
