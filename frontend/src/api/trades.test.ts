import { http, HttpResponse } from 'msw'
import { afterEach, beforeEach, describe, expect, it } from 'vitest'
import { server } from '../test/server'
import { ApiError, setTokenGetter } from './client'
import {
  buyCrypto,
  fetchPortfolio,
  fetchProfitLoss,
  fetchProfitLossSummary,
  sellCrypto,
} from './trades'

const base = 'http://localhost:3000/api'

beforeEach(() => {
  setTokenGetter(() => 'test-token')
})

afterEach(() => {
  setTokenGetter(() => null)
})

describe('fetchPortfolio', () => {
  it('gets the portfolio of the user', async () => {
    const portfolio = {
      userId: 7,
      fiatBalance: 1000,
      holdings: [
        {
          cryptoId: 1,
          cryptoName: 'Bitcoin',
          amountOwned: 2,
          currentExchangeRate: 100,
          currentValue: 200,
        },
      ],
      totalCryptoValue: 200,
      totalPortfolioValue: 1200,
    }
    server.use(http.get(`${base}/users/7/portfolio`, () => HttpResponse.json(portfolio)))

    await expect(fetchPortfolio(7)).resolves.toEqual(portfolio)
  })
})

describe('fetchProfitLoss', () => {
  it('gets the per-crypto profit and loss', async () => {
    const rows = [
      {
        cryptoId: 1,
        cryptoName: 'Bitcoin',
        amountOwned: 2,
        avgPurchasePrice: 90,
        currentExchangeRate: 100,
        totalCostBasis: 180,
        currentValue: 200,
        profitLossAmount: 20,
        profitLossPercentage: 11.1,
      },
    ]
    server.use(http.get(`${base}/users/7/portfolio/profit-loss`, () => HttpResponse.json(rows)))

    await expect(fetchProfitLoss(7)).resolves.toEqual(rows)
  })
})

describe('fetchProfitLossSummary', () => {
  it('gets the portfolio profit and loss summary', async () => {
    const summary = {
      userId: 7,
      totalCurrentCryptoValue: 200,
      totalCostBasis: 180,
      totalProfitLossAmount: 20,
      totalProfitLossPercentage: 11.1,
    }
    server.use(http.get(`${base}/users/7/portfolio/profit-loss/summary`, () => HttpResponse.json(summary)))

    await expect(fetchProfitLossSummary(7)).resolves.toEqual(summary)
  })
})

describe.each([
  ['buyCrypto', buyCrypto, 'buy'],
  ['sellCrypto', sellCrypto, 'sell'],
])('%s', (_name, trade, path) => {
  const order = { userId: 7, cryptoId: 1, amount: 0.5 }

  it('posts the order and resolves on 204', async () => {
    let received: unknown
    let method = ''
    server.use(
      http.post(`${base}/trades/${path}`, async ({ request }) => {
        method = request.method
        received = await request.json()
        return new HttpResponse(null, { status: 204 })
      }),
    )

    await expect(trade(order)).resolves.toBeUndefined()
    expect(method).toBe('POST')
    expect(received).toEqual(order)
  })

  it('rejects with the api error on failure', async () => {
    server.use(
      http.post(`${base}/trades/${path}`, () =>
        HttpResponse.text('Transaction failed', { status: 400 }),
      ),
    )

    const error = await trade(order).catch((e: unknown) => e)
    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ status: 400, message: 'Transaction failed' })
  })
})
