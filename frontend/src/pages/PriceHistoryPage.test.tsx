import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { act } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { PRICE_REFRESH_MS } from '../api/polling'
import { endpoints } from '../api/endpoints'
import { loginAsValidUser, renderApp } from '../test/renderApp'
import { server } from '../test/server'

vi.mock('./PriceHistoryChart', () => ({
  PriceHistoryChart: ({ items }: { items: { value: number }[] }) =>
    items.length === 0 ? null : (
      <div role="img" aria-label="Price chart" data-count={items.length}>
        {items.map((item) => item.value).join(',')}
      </div>
    ),
}))

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

  it('shows crypto name and first 10 records', async () => {
    server.use(
      http.get(historyUrl, () =>
        HttpResponse.json({ items: makeItems(10), totalCount: 60, page: 1, pageSize: 10 }),
      ),
    )
    renderApp('/cryptos/1/price-history')

    expect(await screen.findByRole('heading', { name: /Bitcoin/ })).toBeInTheDocument()
    const table = await screen.findByRole('table')
    expect(await within(table).findByText('$1,000')).toBeInTheDocument()
    expect(within(table).getByText('$1,009')).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Price History' })).toBeInTheDocument()
    expect(screen.getAllByRole('row')).toHaveLength(11)
    expect(screen.getByRole('link', { name: /back/i })).toHaveAttribute('href', '/cryptos')
  })

  it('requests next page when next is clicked', async () => {
    const requested: string[] = []
    server.use(
      http.get(historyUrl, ({ request }) => {
        const params = new URL(request.url).searchParams
        if (params.get('pageSize') !== '10') return HttpResponse.json({ items: [], totalCount: 0, page: 1, pageSize: 100 })
        requested.push(`${params.get('page')}:${params.get('pageSize')}`)
        const page = Number(params.get('page'))
        return HttpResponse.json({
          items: makeItems(10, (page - 1) * 10),
          totalCount: 60,
          page,
          pageSize: 10,
        })
      }),
    )
    renderApp('/cryptos/1/price-history')
    const user = userEvent.setup()

    await screen.findByText('$1,000')
    await user.click(screen.getByRole('button', { name: /next page/i }))

    expect(await screen.findByText('$1,010')).toBeInTheDocument()
    expect(requested).toEqual(['1:10', '2:10'])
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

  it('shows current rate in header', async () => {
    server.use(
      http.get(historyUrl, () =>
        HttpResponse.json({
          items: [
            { value: 1500, from: '2024-02-01T00:00:00Z', to: '2024-03-01T00:00:00Z' },
            { value: 2000.5, from: '2024-03-01T00:00:00Z', to: '9999-12-31T23:59:59Z' },
          ],
          totalCount: 2,
          page: 1,
          pageSize: 25,
        }),
      ),
    )
    renderApp('/cryptos/1/price-history')

    expect(await screen.findByText('Current rate: 2,000.5')).toBeInTheDocument()
  })

  it('shows not found for unknown crypto', async () => {
    server.use(
      http.get(cryptoUrl, () => new HttpResponse('Crypto with ID 1 not found.', { status: 404 })),
      http.get(historyUrl, () => new HttpResponse('Crypto with ID 1 not found.', { status: 404 })),
    )
    renderApp('/cryptos/1/price-history')

    expect(await screen.findByText('Crypto not found.')).toBeInTheDocument()
  })

  it('renders a price chart when history exists', async () => {
    server.use(
      http.get(historyUrl, () =>
        HttpResponse.json({ items: makeItems(3), totalCount: 3, page: 1, pageSize: 25 }),
      ),
    )
    renderApp('/cryptos/1/price-history')

    expect(await screen.findByLabelText('Price chart')).toBeInTheDocument()
  })

  it('requests up to 100 records for the chart', async () => {
    const pageSizes: (string | null)[] = []
    server.use(
      http.get(historyUrl, ({ request }) => {
        pageSizes.push(new URL(request.url).searchParams.get('pageSize'))
        return HttpResponse.json({ items: makeItems(3), totalCount: 3, page: 1, pageSize: 25 })
      }),
    )
    renderApp('/cryptos/1/price-history')

    await screen.findByLabelText('Price chart')
    expect(pageSizes).toContain('100')
  })

  it('hides the chart when there is no history', async () => {
    server.use(
      http.get(historyUrl, () => HttpResponse.json({ items: [], totalCount: 0, page: 1, pageSize: 25 })),
    )
    renderApp('/cryptos/1/price-history')

    expect(await screen.findByText('No price history yet.')).toBeInTheDocument()
    expect(screen.queryByLabelText('Price chart')).not.toBeInTheDocument()
  })

  describe('header, step delta and range', () => {
    const chartPoints = (count: number) =>
      Array.from({ length: count }, (_, i) => ({
        value: 1000 + (count - 1 - i),
        from: '2024-01-01T00:00:00Z',
        to: '2024-02-01T00:00:00Z',
      }))

    function respond(table: unknown[], chart: unknown[]) {
      server.use(
        http.get(historyUrl, ({ request }) => {
          const items = new URL(request.url).searchParams.get('pageSize') === '100' ? chart : table
          return HttpResponse.json({ items, totalCount: items.length, page: 1, pageSize: 10 })
        }),
      )
    }

    const point = (value: number) => ({ value, from: '2024-01-01T00:00:00Z', to: '2024-02-01T00:00:00Z' })

    it('shows session change, high and low from chart history', async () => {
      respond([point(150)], [point(150), point(100), point(50)])
      renderApp('/cryptos/1/price-history')

      expect(await screen.findByText('+200.00% (Session)')).toBeInTheDocument()
      expect(screen.getByText('Session high').parentElement).toHaveTextContent('150')
      expect(screen.getByText('Session low').parentElement).toHaveTextContent('50')
    })

    it('shows a negative session change and keeps stats when the rate is unknown', async () => {
      respond([point(50)], [point(50), point(100)])
      renderApp('/cryptos/1/price-history')

      expect(await screen.findByText('-50.00% (Session)')).toBeInTheDocument()
      expect(screen.queryByText(/Current rate/)).not.toBeInTheDocument()
    })

    it('shows 0.00% session change when the first value is 0', async () => {
      respond([point(5)], [point(5), point(0)])
      renderApp('/cryptos/1/price-history')

      expect(await screen.findByText('+0.00% (Session)')).toBeInTheDocument()
      expect(screen.queryByText(/NaN/)).not.toBeInTheDocument()
    })

    it('shows a Step delta column with percent vs the older row and blank for the oldest row', async () => {
      respond([point(110), point(100)], [])
      renderApp('/cryptos/1/price-history')

      expect(await screen.findByRole('columnheader', { name: /Step delta/ })).toBeInTheDocument()
      const rows = await screen.findAllByRole('row')
      expect(rows[1]).toHaveTextContent('+10.00%')
      expect(rows[2]).not.toHaveTextContent('%')
    })

    it('does not crash with a single history item', async () => {
      respond([point(100)], [point(100)])
      renderApp('/cryptos/1/price-history')

      expect(await screen.findByText('+0.00% (Session)')).toBeInTheDocument()
      const rows = await screen.findAllByRole('row')
      expect(rows).toHaveLength(2)
      expect(rows[1]).not.toHaveTextContent('%')
    })

    it('range buttons limit the chart data', async () => {
      respond([point(1)], chartPoints(30))
      renderApp('/cryptos/1/price-history')
      const user = userEvent.setup()

      expect(await screen.findByLabelText('Price chart')).toHaveAttribute('data-count', '30')
      await user.click(screen.getByText('Last 10'))
      expect(screen.getByLabelText('Price chart')).toHaveAttribute('data-count', '10')
      await user.click(screen.getByText('Last 20'))
      expect(screen.getByLabelText('Price chart')).toHaveAttribute('data-count', '20')
    })

    it('shows all points for Last 20 when there are fewer than 20', async () => {
      respond([point(1)], chartPoints(5))
      renderApp('/cryptos/1/price-history')
      const user = userEvent.setup()

      await screen.findByLabelText('Price chart')
      await user.click(screen.getByText('Last 20'))
      expect(screen.getByLabelText('Price chart')).toHaveAttribute('data-count', '5')
    })
  })

  describe('polling', () => {
    beforeEach(() => {
      vi.useFakeTimers({ shouldAdvanceTime: true })
    })
    afterEach(() => {
      vi.useRealTimers()
    })

    it('refreshes the table and current rate after the refresh interval', async () => {
      let calls = 0
      server.use(
        http.get(historyUrl, () => {
          calls += 1
          const value = calls <= 2 ? 1500 : 1750
          return HttpResponse.json({
            items: [{ value, from: '2024-03-01T00:00:00Z', to: '9999-12-31T23:59:59Z' }],
            totalCount: 1,
            page: 1,
            pageSize: 25,
          })
        }),
      )
      renderApp('/cryptos/1/price-history')
      expect(await screen.findByText('Current rate: 1,500')).toBeInTheDocument()

      await act(() => vi.advanceTimersByTimeAsync(PRICE_REFRESH_MS))

      expect(await screen.findByText('Current rate: 1,750')).toBeInTheDocument()
    })
  })

  it('redirects to login when unauthenticated', async () => {
    localStorage.clear()
    renderApp('/cryptos/1/price-history')

    expect(await screen.findByRole('heading', { name: 'Welcome back' })).toBeInTheDocument()
  })
})
