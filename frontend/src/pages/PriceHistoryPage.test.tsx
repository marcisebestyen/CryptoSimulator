import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { beforeEach, describe, expect, it } from 'vitest'
import { endpoints } from '../api/endpoints'
import { loginAsValidUser, renderApp } from '../test/renderApp'
import { server } from '../test/server'

const cryptoUrl = `http://localhost:3000${endpoints.crypto(1)}`
const historyUrl = `http://localhost:3000${endpoints.cryptoPriceHistory(1)}`

function makeItems(count: number, offset = 0) {
  return Array.from({ length: count }, (_, i) => ({
    value: 1000 + offset + i,
    from: '2024-01-01T00:00:00Z',
    to: '2024-02-01T00:00:00Z',
  }))
}

describe('PriceHistoryPage', () => {
  beforeEach(() => {
    localStorage.clear()
    loginAsValidUser()
    server.use(http.get(cryptoUrl, () => HttpResponse.json({ id: 1, name: 'Bitcoin' })))
  })

  it('shows crypto name and first 25 records', async () => {
    server.use(
      http.get(historyUrl, () =>
        HttpResponse.json({ items: makeItems(25), totalCount: 60, page: 1, pageSize: 25 }),
      ),
    )
    renderApp('/cryptos/1/price-history')

    expect(await screen.findByRole('heading', { name: /Bitcoin/ })).toBeInTheDocument()
    expect(await screen.findByText('1,000')).toBeInTheDocument()
    expect(screen.getByText('1,024')).toBeInTheDocument()
    expect(screen.getAllByRole('row')).toHaveLength(26)
    expect(screen.getByRole('link', { name: /back/i })).toHaveAttribute('href', '/cryptos')
  })

  it('requests next page when next is clicked', async () => {
    const requested: string[] = []
    server.use(
      http.get(historyUrl, ({ request }) => {
        const params = new URL(request.url).searchParams
        requested.push(`${params.get('page')}:${params.get('pageSize')}`)
        const page = Number(params.get('page'))
        return HttpResponse.json({
          items: makeItems(25, (page - 1) * 25),
          totalCount: 60,
          page,
          pageSize: 25,
        })
      }),
    )
    renderApp('/cryptos/1/price-history')
    const user = userEvent.setup()

    await screen.findByText('1,000')
    await user.click(screen.getByRole('button', { name: /next page/i }))

    expect(await screen.findByText('1,025')).toBeInTheDocument()
    expect(requested).toEqual(['1:25', '2:25'])
  })

  it('shows Current for open-ended record', async () => {
    server.use(
      http.get(historyUrl, () =>
        HttpResponse.json({
          items: [{ value: 2000, from: '2024-03-01T00:00:00Z', to: '9999-12-31T23:59:59Z' }],
          totalCount: 1,
          page: 1,
          pageSize: 25,
        }),
      ),
    )
    renderApp('/cryptos/1/price-history')

    expect(await screen.findByText('Current')).toBeInTheDocument()
  })

  it('shows not found for unknown crypto', async () => {
    server.use(
      http.get(cryptoUrl, () => new HttpResponse('Crypto with ID 1 not found.', { status: 404 })),
      http.get(historyUrl, () => new HttpResponse('Crypto with ID 1 not found.', { status: 404 })),
    )
    renderApp('/cryptos/1/price-history')

    expect(await screen.findByText('Crypto not found.')).toBeInTheDocument()
  })

  it('redirects to login when unauthenticated', async () => {
    localStorage.clear()
    renderApp('/cryptos/1/price-history')

    expect(await screen.findByRole('heading', { name: 'Welcome back' })).toBeInTheDocument()
  })
})
