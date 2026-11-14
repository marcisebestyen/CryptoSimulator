import { http, HttpResponse } from 'msw'
import { describe, expect, it } from 'vitest'
import { server } from '../test/server'
import { fetchCryptos } from './cryptos'
import { endpoints } from './endpoints'

const url = `http://localhost:3000${endpoints.cryptos}`

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
