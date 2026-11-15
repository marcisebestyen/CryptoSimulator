import { http, HttpResponse } from 'msw'
import { afterEach, beforeEach, describe, expect, it } from 'vitest'
import { server } from '../test/server'
import { ApiError, setTokenGetter } from './client'
import { createDeposit, deleteCard, fetchCards, fetchDeposits } from './deposits'

const base = 'http://localhost:3000/api'

beforeEach(() => {
  setTokenGetter(() => 'test-token')
})

afterEach(() => {
  setTokenGetter(() => null)
})

describe('createDeposit', () => {
  const request = {
    userId: 7,
    amount: 50,
    holderName: 'Jane Doe',
    cardNumber: '4111111111111111',
    expiryMonth: 12,
    expiryYear: 2030,
    cvv: '123',
    saveCard: true,
  }

  it('posts the deposit with the bearer token and returns the result', async () => {
    const result = {
      id: 1,
      amount: 50,
      last4: '1111',
      network: 'Visa',
      date: '2026-11-15T10:00:00Z',
      newBalance: 150,
    }
    let received: unknown
    let auth: string | null = null
    server.use(
      http.post(`${base}/deposits`, async ({ request: req }) => {
        auth = req.headers.get('Authorization')
        received = await req.json()
        return HttpResponse.json(result, { status: 201 })
      }),
    )

    await expect(createDeposit(request)).resolves.toEqual(result)
    expect(received).toEqual(request)
    expect(auth).toBe('Bearer test-token')
  })

  it('rejects with the api error on failure', async () => {
    server.use(http.post(`${base}/deposits`, () => HttpResponse.text('Card expired', { status: 400 })))

    const error = await createDeposit(request).catch((e: unknown) => e)
    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ status: 400, message: 'Card expired' })
  })
})

describe('fetchDeposits', () => {
  it('gets the deposit history of the user', async () => {
    const rows = [{ id: 2, amount: 20, last4: '1111', network: 'Visa', date: '2026-11-15T10:00:00Z' }]
    server.use(http.get(`${base}/users/7/deposits`, () => HttpResponse.json(rows)))

    await expect(fetchDeposits(7)).resolves.toEqual(rows)
  })
})

describe('fetchCards', () => {
  it('gets the saved cards of the user', async () => {
    const cards = [
      { id: 3, holderName: 'Jane Doe', last4: '1111', network: 'Visa', expiryMonth: 12, expiryYear: 2030 },
    ]
    server.use(http.get(`${base}/users/7/cards`, () => HttpResponse.json(cards)))

    await expect(fetchCards(7)).resolves.toEqual(cards)
  })
})

describe('deleteCard', () => {
  it('sends DELETE and resolves on 204', async () => {
    let method = ''
    server.use(
      http.delete(`${base}/users/7/cards/3`, ({ request }) => {
        method = request.method
        return new HttpResponse(null, { status: 204 })
      }),
    )

    await expect(deleteCard(7, 3)).resolves.toBeUndefined()
    expect(method).toBe('DELETE')
  })

  it('rejects with the api error when the card is missing', async () => {
    server.use(http.delete(`${base}/users/7/cards/3`, () => new HttpResponse(null, { status: 404 })))

    const error = await deleteCard(7, 3).catch((e: unknown) => e)
    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ status: 404 })
  })
})
