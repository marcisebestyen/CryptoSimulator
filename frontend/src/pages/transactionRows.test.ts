import { describe, expect, it } from 'vitest'
import type { Transaction } from '../api/transactions'
import { toRows } from './transactionRows'

const tx = (overrides: Partial<Transaction>): Transaction => ({
  id: 1,
  walletId: 1,
  cryptoId: 1,
  amount: 2,
  exchangeRate: 50,
  date: '2026-01-01T00:00:00Z',
  isPurchase: true,
  transactionType: 'Buy',
  ...overrides,
})

describe('toRows', () => {
  it('joins the coin name and computes the total', () => {
    const rows = toRows([tx({})], [{ id: 1, name: 'Bitcoin', currentExchangeRate: 1 }])

    expect(rows[0]).toMatchObject({ id: 1, cryptoName: 'Bitcoin', total: 100 })
  })

  it('falls back to a numbered label when the coin is gone', () => {
    const rows = toRows([tx({ cryptoId: 9 })], [])

    expect(rows[0].cryptoName).toBe('Crypto #9')
  })
})
