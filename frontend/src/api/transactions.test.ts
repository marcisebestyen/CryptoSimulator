import { http, HttpResponse } from 'msw'
import { afterEach, beforeEach, describe, expect, it } from 'vitest'
import { server } from '../test/server'
import { setTokenGetter } from './client'
import { fetchTransaction, fetchTransactions } from './transactions'

const base = 'http://localhost:3000/api'

const transaction = {
  id: 3,
  walletId: 5,
  cryptoId: 1,
  amount: 0.5,
  exchangeRate: 100,
  date: '2026-11-15T10:00:00Z',
  isPurchase: true,
  transactionType: 'Buy',
}

beforeEach(() => {
  setTokenGetter(() => 'test-token')
})

afterEach(() => {
  setTokenGetter(() => null)
})

describe('fetchTransactions', () => {
  it('requests newest first by default', async () => {
    let query = ''
    server.use(
      http.get(`${base}/users/7/transactions`, ({ request }) => {
        query = new URL(request.url).search
        return HttpResponse.json([transaction])
      }),
    )

    await expect(fetchTransactions(7)).resolves.toEqual([transaction])
    expect(query).toBe('?newestFirst=true')
  })

  it('sends newestFirst=false when asked', async () => {
    let query = ''
    server.use(
      http.get(`${base}/users/7/transactions`, ({ request }) => {
        query = new URL(request.url).search
        return HttpResponse.json([])
      }),
    )

    await fetchTransactions(7, false)
    expect(query).toBe('?newestFirst=false')
  })
})

describe('fetchTransaction', () => {
  it('gets a transaction by id', async () => {
    server.use(http.get(`${base}/transactions/3`, () => HttpResponse.json(transaction)))

    await expect(fetchTransaction(3)).resolves.toEqual(transaction)
  })
})
