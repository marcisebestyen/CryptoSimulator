import { http, HttpResponse } from 'msw'
import { afterEach, beforeEach, describe, expect, it } from 'vitest'
import { server } from '../test/server'
import { ApiError, setTokenGetter } from './client'
import { buyOffer, cancelOffer, createOffer, fetchMarketOffers, fetchMyOffers } from './offers'
import type { MarketOffer, MyOffer } from './offers'

const base = 'http://localhost:3000/api'

const myOffer: MyOffer = {
  id: 5,
  cryptoId: 1,
  cryptoName: 'Bitcoin',
  amount: 2,
  remainingAmount: 1.5,
  soldAmount: 0.5,
  askingPrice: 100,
  currentRate: 95,
  status: 'PartlySold',
  createdAt: '2026-11-15T10:00:00Z',
}

beforeEach(() => {
  setTokenGetter(() => 'test-token')
})

afterEach(() => {
  setTokenGetter(() => null)
})

describe('fetchMarketOffers', () => {
  it('gets the market offers with the bearer token', async () => {
    const rows: MarketOffer[] = [
      {
        id: 5,
        sellerUsername: 'alice',
        sellerCompletionRate: null,
        sellerTrades: 0,
        cryptoId: 1,
        cryptoName: 'Bitcoin',
        remainingAmount: 1.5,
        askingPrice: 100,
        currentRate: 95,
      },
    ]
    let auth: string | null = null
    server.use(
      http.get(`${base}/trades/offers`, ({ request }) => {
        auth = request.headers.get('Authorization')
        return HttpResponse.json(rows)
      }),
    )

    await expect(fetchMarketOffers()).resolves.toEqual(rows)
    expect(auth).toBe('Bearer test-token')
  })
})

describe('fetchMyOffers', () => {
  it('gets the offers of the current user', async () => {
    server.use(http.get(`${base}/trades/offers/mine`, () => HttpResponse.json([myOffer])))

    await expect(fetchMyOffers()).resolves.toEqual([myOffer])
  })
})

describe('createOffer', () => {
  const body = { cryptoId: 1, amount: 2, askingPrice: 100 }

  it('posts the offer and returns the created one', async () => {
    let received: unknown
    server.use(
      http.post(`${base}/trades/offers`, async ({ request }) => {
        received = await request.json()
        return HttpResponse.json(myOffer, { status: 201 })
      }),
    )

    await expect(createOffer(body)).resolves.toEqual(myOffer)
    expect(received).toEqual(body)
  })

  it('rejects with the api error on failure', async () => {
    server.use(http.post(`${base}/trades/offers`, () => HttpResponse.text('Not enough crypto', { status: 400 })))

    const error = await createOffer(body).catch((e: unknown) => e)
    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ status: 400, message: 'Not enough crypto' })
  })
})

describe('cancelOffer', () => {
  it('sends DELETE and resolves on 204', async () => {
    let method = ''
    server.use(
      http.delete(`${base}/trades/offers/5`, ({ request }) => {
        method = request.method
        return new HttpResponse(null, { status: 204 })
      }),
    )

    await expect(cancelOffer(5)).resolves.toBeUndefined()
    expect(method).toBe('DELETE')
  })

  it('rejects with the api error on conflict', async () => {
    server.use(http.delete(`${base}/trades/offers/5`, () => new HttpResponse(null, { status: 409 })))

    await expect(cancelOffer(5)).rejects.toMatchObject({ status: 409 })
  })
})

describe('buyOffer', () => {
  it('posts the amount and resolves on 204', async () => {
    let received: unknown
    server.use(
      http.post(`${base}/trades/offers/5/buy`, async ({ request }) => {
        received = await request.json()
        return new HttpResponse(null, { status: 204 })
      }),
    )

    await expect(buyOffer(5, 0.5)).resolves.toBeUndefined()
    expect(received).toEqual({ amount: 0.5 })
  })

  it('rejects with the api error on 400', async () => {
    server.use(http.post(`${base}/trades/offers/5/buy`, () => HttpResponse.text('Insufficient funds', { status: 400 })))

    const error = await buyOffer(5, 0.5).catch((e: unknown) => e)
    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ status: 400, message: 'Insufficient funds' })
  })
})
