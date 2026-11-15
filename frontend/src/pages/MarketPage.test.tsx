import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { beforeEach, describe, expect, it } from 'vitest'
import { endpoints } from '../api/endpoints'
import { TOKEN_KEY } from '../auth/token'
import { makeJwt } from '../test/jwt'
import { renderApp } from '../test/renderApp'
import { server } from '../test/server'

const base = 'http://localhost:3000'
const url = (path: string) => `${base}${path}`

const cryptos = [
  { id: 1, name: 'Bitcoin', currentExchangeRate: 100 },
  { id: 2, name: 'Ether', currentExchangeRate: 5 },
]
const portfolioWith = (fiatBalance: number) => ({
  userId: 7,
  fiatBalance,
  holdings: [{ cryptoId: 1, cryptoName: 'Bitcoin', amountOwned: 2, currentExchangeRate: 100, currentValue: 200 }],
  totalCryptoValue: 200,
  totalPortfolioValue: fiatBalance + 200,
})
const rows = [
  {
    cryptoId: 1,
    cryptoName: 'Bitcoin',
    amountOwned: 2,
    avgPurchasePrice: 80,
    currentExchangeRate: 100,
    totalCostBasis: 160,
    currentValue: 200,
    profitLossAmount: 40,
    profitLossPercentage: 25,
  },
]
const summary = {
  userId: 7,
  totalCurrentCryptoValue: 200,
  totalCostBasis: 160,
  totalProfitLossAmount: 40,
  totalProfitLossPercentage: 25,
}
const transactions = [1, 2, 3].map((id) => ({
  id,
  walletId: 1,
  cryptoId: 1,
  amount: 1,
  exchangeRate: 100,
  date: '2026-01-01T00:00:00Z',
  isPurchase: true,
  transactionType: 'Buy',
}))

function useHandlers(balance = { current: 1000 }) {
  server.use(
    http.get(url(endpoints.cryptos), () => HttpResponse.json(cryptos)),
    http.get(url(endpoints.portfolio(7)), () => HttpResponse.json(portfolioWith(balance.current))),
    http.get(url(endpoints.profitLoss(7)), () => HttpResponse.json(rows)),
    http.get(url(endpoints.profitLossSummary(7)), () => HttpResponse.json(summary)),
    http.get(url(endpoints.transactions(7)), () => HttpResponse.json(transactions)),
  )
}

async function openBuyModal(ui: ReturnType<typeof userEvent.setup>) {
  await ui.click(await screen.findByRole('button', { name: 'Buy Ether' }))
  return screen.findByRole('dialog')
}

describe('MarketPage', () => {
  beforeEach(() => {
    localStorage.clear()
    localStorage.setItem(TOKEN_KEY, makeJwt(Math.floor(Date.now() / 1000) + 3600, { nameid: '7' }))
  })

  it('shows KPI cards, holdings, live market and transactions link', async () => {
    useHandlers()
    renderApp('/market')

    expect(await screen.findByText('$1,000')).toBeInTheDocument()
    expect(screen.getByText('Cash balance')).toBeInTheDocument()
    expect(screen.getAllByText('+$40')).toHaveLength(2)
    expect(screen.getByText(/\+25%/)).toBeInTheDocument()
    expect(await screen.findByText('3')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: /view transactions/i })).toHaveAttribute('href', '/market/transactions')
    expect(screen.getByText('Your Holdings')).toBeInTheDocument()
    expect(screen.getByText('Live Market')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Sell Bitcoin' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Buy Ether' })).toBeInTheDocument()
  })

  it('buys, posts the order and shows the updated cash after refetch', async () => {
    const balance = { current: 1000 }
    useHandlers(balance)
    let posted: unknown = null
    server.use(
      http.post(url(endpoints.tradeBuy), async ({ request }) => {
        posted = await request.json()
        balance.current = 975
        return new HttpResponse(null, { status: 204 })
      }),
    )
    const ui = userEvent.setup()
    renderApp('/market')

    const dialog = await openBuyModal(ui)
    await ui.type(within(dialog).getByLabelText('Amount'), '5')
    expect(within(dialog).getByText('$25')).toBeInTheDocument()
    await ui.click(within(dialog).getByRole('button', { name: 'Confirm' }))

    await waitFor(() => expect(posted).toEqual({ userId: 7, cryptoId: 2, amount: 5 }))
    expect(await screen.findByText('$975')).toBeInTheDocument()
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument())
  })

  it('opens the modal in sell mode with the coin preselected', async () => {
    useHandlers()
    const ui = userEvent.setup()
    renderApp('/market')

    await ui.click(await screen.findByRole('button', { name: 'Sell Bitcoin' }))
    const dialog = await screen.findByRole('dialog')

    expect(within(dialog).getByRole('radio', { name: 'Sell' })).toBeChecked()
    expect(within(dialog).getByLabelText('Coin')).toHaveValue('1')
  })

  it('fills the max sell amount from the Max chip', async () => {
    useHandlers()
    const ui = userEvent.setup()
    renderApp('/market')

    await ui.click(await screen.findByRole('button', { name: 'Sell Bitcoin' }))
    const dialog = await screen.findByRole('dialog')
    await ui.click(within(dialog).getByRole('button', { name: 'Max' }))

    expect(within(dialog).getByLabelText('Amount')).toHaveValue('2')
  })

  it('fills the max buy amount from the balance', async () => {
    useHandlers()
    const ui = userEvent.setup()
    renderApp('/market')

    const dialog = await openBuyModal(ui)
    await ui.click(within(dialog).getByRole('button', { name: 'Max' }))

    expect(within(dialog).getByLabelText('Amount')).toHaveValue('200')
  })

  it('disables Confirm for empty, zero and tiny amounts', async () => {
    useHandlers()
    const ui = userEvent.setup()
    renderApp('/market')

    const dialog = await openBuyModal(ui)
    const confirm = within(dialog).getByRole('button', { name: 'Confirm' })
    const amount = within(dialog).getByLabelText('Amount')
    expect(confirm).toBeDisabled()
    await ui.type(amount, '0')
    expect(confirm).toBeDisabled()
    await ui.clear(amount)
    await ui.type(amount, '0.000000001')
    expect(confirm).toBeDisabled()
    await ui.clear(amount)
    await ui.type(amount, '1')
    expect(confirm).toBeEnabled()
  })

  it('disables Confirm for hex, exponent, negative and over-precise amounts', async () => {
    useHandlers()
    const ui = userEvent.setup()
    renderApp('/market')

    const dialog = await openBuyModal(ui)
    const confirm = within(dialog).getByRole('button', { name: 'Confirm' })
    const amount = within(dialog).getByLabelText('Amount')
    for (const text of ['0x10', '1e-9', '-1', '1.123456789']) {
      await ui.clear(amount)
      await ui.type(amount, text)
      expect(confirm).toBeDisabled()
      expect(within(dialog).getByText(/valid amount/i)).toBeInTheDocument()
    }
  })

  it('shows alerts when profit and loss and transactions fail to load', async () => {
    useHandlers()
    server.use(
      http.get(url(endpoints.profitLoss(7)), () => new HttpResponse('boom', { status: 500 })),
      http.get(url(endpoints.profitLossSummary(7)), () => new HttpResponse('boom', { status: 500 })),
      http.get(url(endpoints.transactions(7)), () => new HttpResponse('boom', { status: 500 })),
    )
    renderApp('/market')

    expect(await screen.findByText('Could not load profit and loss')).toBeInTheDocument()
    expect(await screen.findByText('Could not load transactions')).toBeInTheDocument()
  })

  it('disables Confirm and explains the shortfall when buy exceeds the balance', async () => {
    useHandlers()
    const ui = userEvent.setup()
    renderApp('/market')

    const dialog = await openBuyModal(ui)
    await ui.type(within(dialog).getByLabelText('Amount'), '300')

    expect(within(dialog).getByRole('button', { name: 'Confirm' })).toBeDisabled()
    expect(within(dialog).getByText(/insufficient balance/i)).toBeInTheDocument()
  })

  it('disables Confirm and shows owned amount when selling more than owned', async () => {
    useHandlers()
    const ui = userEvent.setup()
    renderApp('/market')

    await ui.click(await screen.findByRole('button', { name: 'Sell Bitcoin' }))
    const dialog = await screen.findByRole('dialog')
    await ui.type(within(dialog).getByLabelText('Amount'), '3')

    expect(within(dialog).getByRole('button', { name: 'Confirm' })).toBeDisabled()
    expect(within(dialog).getByText(/you only own 2/i)).toBeInTheDocument()
  })

  it('shows the server error and keeps the modal open on a 400', async () => {
    useHandlers()
    server.use(http.post(url(endpoints.tradeBuy), () => new HttpResponse('Transaction failed', { status: 400 })))
    const ui = userEvent.setup()
    renderApp('/market')

    const dialog = await openBuyModal(ui)
    await ui.type(within(dialog).getByLabelText('Amount'), '1')
    await ui.click(within(dialog).getByRole('button', { name: 'Confirm' }))

    expect(await within(dialog).findByText('Transaction failed')).toBeInTheDocument()
    expect(screen.getByRole('dialog')).toBeInTheDocument()
  })

  it('switches mode in the modal and limits sell coins to holdings', async () => {
    useHandlers()
    const ui = userEvent.setup()
    renderApp('/market')

    const dialog = await openBuyModal(ui)
    await ui.click(within(dialog).getByRole('radio', { name: 'Sell' }))

    const options = within(within(dialog).getByLabelText('Coin')).getAllByRole('option')
    expect(options.map((option) => option.textContent)).toEqual(['Bitcoin'])
  })
})
