import { http, HttpResponse } from 'msw'
import { afterEach, beforeEach, describe, expect, it } from 'vitest'
import { server } from '../test/server'
import { ApiError, setTokenGetter } from './client'
import {
  createCrypto,
  deleteCrypto,
  fetchCrypto,
  fetchCryptos,
  fetchPriceHistory,
  renameCrypto,
  setCryptoPrice,
} from './cryptos'
import { endpoints } from './endpoints'

const url = `http://localhost:3000${endpoints.cryptos}`

beforeEach(() => {
  setTokenGetter(() => 'test-token')
})

afterEach(() => {
  setTokenGetter(() => null)
})

describe('fetchCryptos', () => {
  it('returns the parsed cryptos', async () => {
    const cryptos = [{ id: 1, name: 'Bitcoin', currentExchangeRate: 100.5 }]
    server.use(http.get(url, () => HttpResponse.json(cryptos)))

    await expect(fetchCryptos()).resolves.toEqual(cryptos)
  })

  it('returns an empty array when there are none', async () => {
    server.use(http.get(url, () => HttpResponse.json([])))

    await expect(fetchCryptos()).resolves.toEqual([])
  })
})

describe('fetchCrypto', () => {
  it('gets one crypto with the bearer token', async () => {
    let auth: string | null = null
    server.use(
      http.get(`${url}/7`, ({ request }) => {
        auth = request.headers.get('Authorization')
        return HttpResponse.json({ id: 7, name: 'Ether' })
      }),
    )

    await expect(fetchCrypto(7)).resolves.toEqual({ id: 7, name: 'Ether' })
    expect(auth).toBe('Bearer test-token')
  })
})

describe('createCrypto', () => {
  it('posts the name as JSON with the bearer token', async () => {
    let body: unknown = null
    let auth: string | null = null
    server.use(
      http.post(url, async ({ request }) => {
        body = await request.json()
        auth = request.headers.get('Authorization')
        return HttpResponse.json({ id: 3, name: 'Doge', currentExchangeRate: 0 }, { status: 201 })
      }),
    )

    await expect(createCrypto('Doge')).resolves.toMatchObject({ id: 3, name: 'Doge' })
    expect(body).toEqual({ name: 'Doge' })
    expect(auth).toBe('Bearer test-token')
  })

  it('surfaces a 409 as ApiError', async () => {
    server.use(http.post(url, () => new HttpResponse('duplicate', { status: 409 })))

    await expect(createCrypto('Doge')).rejects.toMatchObject({
      name: 'Error',
      status: 409,
    })
    await expect(createCrypto('Doge')).rejects.toBeInstanceOf(ApiError)
  })
})

describe('renameCrypto', () => {
  it('puts the name as JSON with the bearer token', async () => {
    let body: unknown = null
    let auth: string | null = null
    server.use(
      http.put(`${url}/4`, async ({ request }) => {
        body = await request.json()
        auth = request.headers.get('Authorization')
        return new HttpResponse(null, { status: 204 })
      }),
    )

    await expect(renameCrypto(4, 'New')).resolves.toBeUndefined()
    expect(body).toEqual({ name: 'New' })
    expect(auth).toBe('Bearer test-token')
  })
})

describe('setCryptoPrice', () => {
  it('puts newPrice as JSON with the bearer token', async () => {
    let body: unknown = null
    let auth: string | null = null
    server.use(
      http.put(`${url}/4/price`, async ({ request }) => {
        body = await request.json()
        auth = request.headers.get('Authorization')
        return new HttpResponse(null, { status: 204 })
      }),
    )

    await expect(setCryptoPrice(4, 12.5)).resolves.toBeUndefined()
    expect(body).toEqual({ newPrice: 12.5 })
    expect(auth).toBe('Bearer test-token')
  })
})

describe('deleteCrypto', () => {
  it('sends DELETE with the bearer token', async () => {
    let auth: string | null = null
    server.use(
      http.delete(`${url}/9`, ({ request }) => {
        auth = request.headers.get('Authorization')
        return new HttpResponse(null, { status: 204 })
      }),
    )

    await expect(deleteCrypto(9)).resolves.toBeUndefined()
    expect(auth).toBe('Bearer test-token')
  })
})

describe('fetchPriceHistory', () => {
  it('sends page and pageSize and returns the paged result', async () => {
    let search: string | null = null
    let auth: string | null = null
    const result = {
      items: [{ value: 1, from: '2026-01-01T00:00:00Z', to: '9999-12-31T00:00:00Z' }],
      totalCount: 1,
      page: 2,
      pageSize: 25,
    }
    server.use(
      http.get(`${url}/5/price-history`, ({ request }) => {
        search = new URL(request.url).search
        auth = request.headers.get('Authorization')
        return HttpResponse.json(result)
      }),
    )

    await expect(fetchPriceHistory(5, 2, 25)).resolves.toEqual(result)
    expect(search).toBe('?page=2&pageSize=25')
    expect(auth).toBe('Bearer test-token')
  })
})
