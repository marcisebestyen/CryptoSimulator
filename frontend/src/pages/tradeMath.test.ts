import { describe, expect, it } from 'vitest'
import { amountTextError, floorAmount, maxAmount, tradeError } from './tradeMath'

describe('tradeError', () => {
  it('returns null for a valid buy', () => {
    expect(tradeError('buy', 1, 100, 500, 0)).toBeNull()
  })
  it('returns null for a valid sell', () => {
    expect(tradeError('sell', 1, 100, 0, 2)).toBeNull()
  })
  it('rejects zero and negative amounts', () => {
    expect(tradeError('buy', 0, 100, 500, 0)).not.toBeNull()
    expect(tradeError('sell', -1, 100, 0, 2)).not.toBeNull()
  })
  it('rejects NaN amounts', () => {
    expect(tradeError('buy', Number.NaN, 100, 500, 0)).not.toBeNull()
  })
  it('rejects amounts below 0.0001', () => {
    expect(tradeError('buy', 1e-5, 100, 500, 0)).not.toBeNull()
  })
  it('accepts exactly 0.0001', () => {
    expect(tradeError('buy', 1e-4, 100, 500, 0)).toBeNull()
  })
  it('reports the shortfall when buy total exceeds balance', () => {
    expect(tradeError('buy', 6, 100, 500, 0)).toMatch(/100/)
  })
  it('allows a buy that costs exactly the balance', () => {
    expect(tradeError('buy', 5, 100, 500, 0)).toBeNull()
  })
  it('reports owned amount when selling more than owned', () => {
    expect(tradeError('sell', 3, 100, 0, 2)).toMatch(/2/)
  })
})

describe('maxAmount', () => {
  it('buy floors balance/rate to 4 decimals', () => {
    expect(maxAmount('buy', 100, 3, 0)).toBe(33.3333)
  })
  it('buy is 0 when rate is 0', () => {
    expect(maxAmount('buy', 100, 0, 0)).toBe(0)
  })
  it('sell is the owned amount', () => {
    expect(maxAmount('sell', 100, 3, 1.5)).toBe(1.5)
  })
})

describe('amountTextError', () => {
  it.each(['1', '0.5', '12.1234', '0.0001'])('accepts %s', (text) => {
    expect(amountTextError(text)).toBeNull()
  })
  it.each(['', '  ', '-1', '0x10', '1e-9', '0.12345', '0.00001', '.5', 'abc', '1.', '1,5'])('rejects "%s"', (text) => {
    expect(amountTextError(text)).not.toBeNull()
  })
  it('explains the decimal limit', () => {
    expect(amountTextError('0.12345')).toMatch(/4 decimal/)
  })
})

describe('floorAmount', () => {
  it('floors to 4 decimals', () => {
    expect(floorAmount(1.234567)).toBe(1.2345)
  })
  it('keeps an exact 4 decimal value despite float noise', () => {
    expect(floorAmount(0.0003)).toBe(0.0003)
    expect(floorAmount(1.1)).toBe(1.1)
  })
})
