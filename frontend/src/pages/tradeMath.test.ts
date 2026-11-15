import { describe, expect, it } from 'vitest'
import { amountTextError, maxAmount, tradeError } from './tradeMath'

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
  it('rejects amounts below 0.00000001', () => {
    expect(tradeError('buy', 1e-9, 100, 500, 0)).not.toBeNull()
  })
  it('accepts exactly 0.00000001', () => {
    expect(tradeError('buy', 1e-8, 100, 500, 0)).toBeNull()
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
  it('buy floors balance/rate to 8 decimals', () => {
    expect(maxAmount('buy', 100, 3, 0)).toBe(33.33333333)
  })
  it('buy is 0 when rate is 0', () => {
    expect(maxAmount('buy', 100, 0, 0)).toBe(0)
  })
  it('sell is the owned amount', () => {
    expect(maxAmount('sell', 100, 3, 1.5)).toBe(1.5)
  })
})

describe('amountTextError', () => {
  it.each(['1', '0.5', '12.12345678', '0.00000001'])('accepts %s', (text) => {
    expect(amountTextError(text)).toBeNull()
  })
  it.each(['', '  ', '-1', '0x10', '1e-9', '1.123456789', '.5', 'abc', '1.', '1,5'])('rejects "%s"', (text) => {
    expect(amountTextError(text)).not.toBeNull()
  })
  it('explains the decimal limit', () => {
    expect(amountTextError('1.123456789')).toMatch(/8 decimal/)
  })
})
