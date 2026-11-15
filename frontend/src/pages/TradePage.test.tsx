import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { delay, http, HttpResponse } from 'msw'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { MarketOffer, MyOffer } from '../api/offers'
import { endpoints } from '../api/endpoints'
import { PRICE_REFRESH_MS } from '../api/polling'
import { TOKEN_KEY } from '../auth/token'
import { makeJwt } from '../test/jwt'
import { renderApp } from '../test/renderApp'
import { server } from '../test/server'

const url = (path: string) => `http://localhost:3000${path}`

const cryptos = [
  { id: 1, name: 'Bitcoin', currentExchangeRate: 100 },
  { id: 2, name: 'Ether', currentExchangeRate: 5 },
]
const portfolio = {
  userId: 7,
  fiatBalance: 1000,
  holdings: [{ cryptoId: 1, cryptoName: 'Bitcoin', amountOwned: 2, currentExchangeRate: 100, currentValue: 200 }],
  totalCryptoValue: 200,
  totalPortfolioValue: 1200,
}
const myOffer: MyOffer = {
  id: 21,
  cryptoId: 1,
  cryptoName: 'Bitcoin',
  amount: 1,
  remainingAmount: 0.4,
  soldAmount: 0.6,
  askingPrice: 105,
  currentRate: 100,
  status: 'PartlySold',
  createdAt: '2026-01-01T00:00:00Z',
}
const soldOffer: MyOffer = { ...myOffer, id: 22, remainingAmount: 0, soldAmount: 1, status: 'SoldOut' }
const marketOffer: MarketOffer = {
  id: 11,
  sellerUsername: 'alice',
  sellerCompletionRate: 0.95,
  sellerTrades: 12,
  cryptoId: 1,
  cryptoName: 'Bitcoin',
  remainingAmount: 2,
  askingPrice: 90,
  currentRate: 100,
}
const newSeller: MarketOffer = { ...marketOffer, id: 12, sellerUsername: 'newbie', sellerCompletionRate: null, sellerTrades: 0 }

function useHandlers(mineOffers: MyOffer[] = [myOffer, soldOffer], marketOffers: MarketOffer[] = [marketOffer]) {
  server.use(
    http.get(url(endpoints.cryptos), () => HttpResponse.json(cryptos)),
    http.get(url(endpoints.portfolio(7)), () => HttpResponse.json(portfolio)),
    http.get(url(endpoints.tradeOffers), () => HttpResponse.json(marketOffers)),
    http.get(url(endpoints.tradeMyOffers), () => HttpResponse.json(mineOffers)),
  )
}

describe('TradePage', () => {
  beforeEach(() => {
    localStorage.clear()
    localStorage.setItem(TOKEN_KEY, makeJwt(Math.floor(Date.now() / 1000) + 3600, { nameid: '7' }))
  })

  it('renders both tables from the API', async () => {
    useHandlers([myOffer, soldOffer], [marketOffer, newSeller])
    renderApp('/trade')

    const mineTable = await screen.findByRole('table', { name: 'My offers' })
    expect(await within(mineTable).findByText('Partly sold')).toBeInTheDocument()
    expect(within(mineTable).getByText('0.4 left of 1')).toBeInTheDocument()
    expect(within(mineTable).getByText('Sold out')).toBeInTheDocument()
    expect(within(mineTable).getAllByRole('button', { name: /cancel/i })).toHaveLength(1)

    const marketTable = screen.getByRole('table', { name: 'Offers' })
    expect(await within(marketTable).findByText('alice')).toBeInTheDocument()
    expect(within(marketTable).getByText(/95%/)).toBeInTheDocument()
    expect(within(marketTable).getByText(/n\/a/)).toBeInTheDocument()
    expect(within(marketTable).getAllByText(/10% below market/)).toHaveLength(2)
    expect(screen.getByText('Cash')).toBeInTheDocument()
    expect(screen.getByText('$1,000')).toBeInTheDocument()
  })

  it('hides own offers from the Offers table', async () => {
    useHandlers([myOffer], [{ ...marketOffer, id: 21 }, newSeller])
    renderApp('/trade')

    const marketTable = await screen.findByRole('table', { name: 'Offers' })
    expect(await within(marketTable).findByText('newbie')).toBeInTheDocument()
    expect(within(marketTable).queryByText('alice')).not.toBeInTheDocument()
    expect(within(marketTable).getAllByRole('button', { name: /buy/i })).toHaveLength(1)
  })

  it('creates an offer and refreshes the list', async () => {
    const mineOffers: MyOffer[] = []
    useHandlers(mineOffers)
    let posted: unknown = null
    server.use(
      http.get(url(endpoints.tradeMyOffers), () => HttpResponse.json(mineOffers)),
      http.post(url(endpoints.tradeOffers), async ({ request }) => {
        posted = await request.json()
        mineOffers.push({ ...myOffer, status: 'Open', remainingAmount: 0.5, soldAmount: 0, amount: 0.5 })
        return HttpResponse.json(mineOffers[0])
      }),
    )
    const ui = userEvent.setup()
    renderApp('/trade')

    await ui.click(await screen.findByRole('button', { name: 'Create offer' }))
    const dialog = await screen.findByRole('dialog')
    await ui.type(within(dialog).getByLabelText('Quantity'), '0.5')
    await ui.type(within(dialog).getByLabelText('Asking price'), '105')
    expect(within(dialog).getByText('$52.5')).toBeInTheDocument()
    await ui.click(within(dialog).getByRole('button', { name: 'List offer' }))

    await waitFor(() => expect(posted).toEqual({ cryptoId: 1, amount: 0.5, askingPrice: 105 }))
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument())
    const mineTable = screen.getByRole('table', { name: 'My offers' })
    expect(await within(mineTable).findByRole('button', { name: 'Cancel 0.5 Bitcoin offer at $105' })).toBeInTheDocument()
  })

  it('buys part of an offer', async () => {
    useHandlers([], [marketOffer])
    let posted: unknown = null
    server.use(
      http.post(url(endpoints.tradeOfferBuy(11)), async ({ request }) => {
        posted = await request.json()
        return new HttpResponse(null, { status: 204 })
      }),
    )
    const ui = userEvent.setup()
    renderApp('/trade')

    await ui.click(await screen.findByRole('button', { name: 'Buy Bitcoin from alice' }))
    const dialog = await screen.findByRole('dialog')
    const quantity = within(dialog).getByLabelText('Quantity')
    expect(quantity).toHaveValue('2')
    await ui.clear(quantity)
    await ui.type(quantity, '0.5')
    expect(within(dialog).getByText('$45')).toBeInTheDocument()
    await ui.click(within(dialog).getByRole('button', { name: 'Confirm purchase' }))

    await waitFor(() => expect(posted).toEqual({ amount: 0.5 }))
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument())
  })

  it('cancels an offer with DELETE', async () => {
    useHandlers([myOffer], [])
    let deleted = false
    server.use(
      http.delete(url(endpoints.tradeOffer(21)), () => {
        deleted = true
        return new HttpResponse(null, { status: 204 })
      }),
    )
    const ui = userEvent.setup()
    renderApp('/trade')

    await ui.click(await screen.findByRole('button', { name: 'Cancel 0.4 Bitcoin offer at $105' }))

    await waitFor(() => expect(deleted).toBe(true))
  })

  it('shows the server error in an alert on a 400', async () => {
    useHandlers([], [marketOffer])
    server.use(http.post(url(endpoints.tradeOfferBuy(11)), () => new HttpResponse('Offer no longer available', { status: 400 })))
    const ui = userEvent.setup()
    renderApp('/trade')

    await ui.click(await screen.findByRole('button', { name: 'Buy Bitcoin from alice' }))
    const dialog = await screen.findByRole('dialog')
    await ui.click(within(dialog).getByRole('button', { name: 'Confirm purchase' }))

    expect(await within(dialog).findByRole('alert')).toHaveTextContent('Offer no longer available')
  })

  it('never shows own offers in the market table while my offers are loading', async () => {
    server.use(
      http.get(url(endpoints.cryptos), () => HttpResponse.json(cryptos)),
      http.get(url(endpoints.portfolio(7)), () => HttpResponse.json(portfolio)),
      http.get(url(endpoints.tradeOffers), () => HttpResponse.json([{ ...marketOffer, id: 21 }, newSeller])),
      http.get(url(endpoints.tradeMyOffers), async () => {
        await delay(200)
        return HttpResponse.json([myOffer])
      }),
    )
    renderApp('/trade')

    await screen.findByText('Locked value')
    await delay(100)
    expect(screen.queryByRole('table', { name: 'Offers' })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /^Buy/ })).not.toBeInTheDocument()

    const marketTable = await screen.findByRole('table', { name: 'Offers' })
    expect(within(marketTable).getAllByRole('button', { name: /^Buy/ })).toHaveLength(1)
    expect(within(marketTable).getByText('newbie')).toBeInTheDocument()
    expect(within(marketTable).queryByText('alice')).not.toBeInTheDocument()
  })

  it('blocks submit when the quantity is above what is left', async () => {
    useHandlers([], [marketOffer])
    let called = false
    server.use(
      http.post(url(endpoints.tradeOfferBuy(11)), () => {
        called = true
        return new HttpResponse(null, { status: 204 })
      }),
    )
    const ui = userEvent.setup()
    renderApp('/trade')

    await ui.click(await screen.findByRole('button', { name: 'Buy Bitcoin from alice' }))
    const dialog = await screen.findByRole('dialog')
    const quantity = within(dialog).getByLabelText('Quantity')
    await ui.clear(quantity)
    await ui.type(quantity, '3')

    expect(within(dialog).getByText('Only 2.')).toBeInTheDocument()
    expect(within(dialog).getByRole('button', { name: 'Confirm purchase' })).toBeDisabled()
    await ui.type(quantity, '{Enter}')
    expect(called).toBe(false)
  })

  it('fills Max with the holding floored to 4 decimals', async () => {
    useHandlers([], [])
    server.use(
      http.get(url(endpoints.portfolio(7)), () =>
        HttpResponse.json({ ...portfolio, holdings: [{ ...portfolio.holdings[0], amountOwned: 1.234567 }] }),
      ),
    )
    const ui = userEvent.setup()
    renderApp('/trade')

    await ui.click(await screen.findByRole('button', { name: 'Create offer' }))
    const dialog = await screen.findByRole('dialog')
    await ui.click(within(dialog).getByRole('button', { name: 'Max' }))

    expect(within(dialog).getByLabelText('Quantity')).toHaveValue('1.2345')
  })

  it.each([400, 409])('refreshes the market list when a purchase fails with %i', async (status) => {
    let marketCalls = 0
    useHandlers([], [marketOffer])
    server.use(
      http.get(url(endpoints.tradeOffers), () => {
        marketCalls += 1
        return HttpResponse.json([marketOffer])
      }),
      http.post(url(endpoints.tradeOfferBuy(11)), () => new HttpResponse('Offer is closed.', { status })),
    )
    const ui = userEvent.setup()
    renderApp('/trade')

    await ui.click(await screen.findByRole('button', { name: 'Buy Bitcoin from alice' }))
    const dialog = await screen.findByRole('dialog')
    const callsBefore = marketCalls
    await ui.click(within(dialog).getByRole('button', { name: 'Confirm purchase' }))

    expect(await within(dialog).findByRole('alert')).toHaveTextContent('Offer is closed.')
    await waitFor(() => expect(marketCalls).toBeGreaterThan(callsBefore))
  })

  it('shows small asking prices with up to 8 decimals', async () => {
    useHandlers([{ ...myOffer, askingPrice: 0.00012345 }], [{ ...marketOffer, askingPrice: 0.00012345 }])
    renderApp('/trade')

    const marketTable = await screen.findByRole('table', { name: 'Offers' })
    expect(within(marketTable).getByText('$0.00012345')).toBeInTheDocument()
    const mineTable = screen.getByRole('table', { name: 'My offers' })
    expect(within(mineTable).getByText('$0.00012345')).toBeInTheDocument()
  })

  it('polls the portfolio so the cash card follows a buyer fill', async () => {
    vi.useFakeTimers({ toFake: ['setInterval', 'clearInterval'] })
    try {
      let balance = 1000
      useHandlers([], [])
      server.use(http.get(url(endpoints.portfolio(7)), () => HttpResponse.json({ ...portfolio, fiatBalance: balance })))
      renderApp('/trade')

      expect(await screen.findByText('$1,000')).toBeInTheDocument()
      balance = 1050
      await vi.advanceTimersByTimeAsync(PRICE_REFRESH_MS)

      expect(await screen.findByText('$1,050')).toBeInTheDocument()
    } finally {
      vi.useRealTimers()
    }
  })
})
