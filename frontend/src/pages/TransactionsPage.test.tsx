import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { beforeEach, describe, expect, it } from 'vitest'
import { endpoints } from '../api/endpoints'
import { TOKEN_KEY } from '../auth/token'
import { makeJwt } from '../test/jwt'
import { renderApp } from '../test/renderApp'
import { server } from '../test/server'

const url = (path: string) => `http://localhost:3000${path}`

const cryptos = [
  { id: 1, name: 'Bitcoin', currentExchangeRate: 100 },
  { id: 2, name: 'Ether', currentExchangeRate: 5 },
]
const tx = (id: number, cryptoId: number, amount: number, exchangeRate: number, isPurchase: boolean) => ({
  id,
  walletId: 42,
  cryptoId,
  amount,
  exchangeRate,
  date: '2026-01-01T12:00:00Z',
  isPurchase,
  transactionType: isPurchase ? 'Buy' : 'Sell',
})
const transactions = [tx(1, 1, 1, 100, true), tx(2, 2, 10, 5, false), tx(3, 1, 3, 100, true), tx(4, 9, 1, 10, true)]

function useHandlers(list = transactions) {
  const requested: string[] = []
  server.use(
    http.get(url(endpoints.cryptos), () => HttpResponse.json(cryptos)),
    http.get(url(endpoints.transactions(7)), ({ request }) => {
      requested.push(new URL(request.url).searchParams.get('newestFirst') ?? '')
      return HttpResponse.json(list)
    }),
  )
  return requested
}

const bodyRows = () => screen.getAllByRole('row').slice(1)

describe('TransactionsPage', () => {
  beforeEach(() => {
    localStorage.clear()
    localStorage.setItem(TOKEN_KEY, makeJwt(Math.floor(Date.now() / 1000) + 3600, { nameid: '7' }))
  })

  it('shows stats over all transactions and a back link', async () => {
    useHandlers()
    renderApp('/market/transactions')

    expect(await screen.findAllByText('Bitcoin', { selector: 'p' })).toHaveLength(2)
    expect(screen.getByText('$460')).toBeInTheDocument()
    expect(screen.getByTestId('buy-count')).toHaveTextContent('3')
    expect(screen.getByTestId('sell-count')).toHaveTextContent('1')
    expect(screen.getByRole('link', { name: /back to market/i })).toHaveAttribute('href', '/market')
    expect(bodyRows()).toHaveLength(4)
  })

  it('filters by type', async () => {
    useHandlers()
    const ui = userEvent.setup()
    renderApp('/market/transactions')
    await screen.findByText('Ether', { selector: 'p' })

    await ui.click(screen.getByRole('radio', { name: 'Sell' }))

    expect(bodyRows()).toHaveLength(1)
    expect(within(bodyRows()[0]).getByText('SELL')).toBeInTheDocument()
    expect(screen.getByTestId('buy-count')).toHaveTextContent('3')
  })

  it('searches by coin name ignoring case', async () => {
    useHandlers()
    const ui = userEvent.setup()
    renderApp('/market/transactions')
    await screen.findByText('Ether', { selector: 'p' })

    await ui.type(screen.getByLabelText('Search coin'), 'eTH')

    expect(bodyRows()).toHaveLength(1)
    expect(within(bodyRows()[0]).getByText('Ether')).toBeInTheDocument()
  })

  it('sends newestFirst=false when sorting oldest', async () => {
    const requested = useHandlers()
    const ui = userEvent.setup()
    renderApp('/market/transactions')
    await screen.findByText('Ether', { selector: 'p' })

    await ui.selectOptions(screen.getByLabelText('Sort by'), 'Oldest')

    await waitFor(() => expect(requested).toContain('false'))
  })

  it('sorts by highest value on the client', async () => {
    const requested = useHandlers()
    const ui = userEvent.setup()
    renderApp('/market/transactions')
    await screen.findByText('Ether', { selector: 'p' })

    await ui.selectOptions(screen.getByLabelText('Sort by'), 'Highest value')

    await waitFor(() => expect(within(bodyRows()[0]).getByText('#3')).toBeInTheDocument())
    expect(within(bodyRows()[1]).getByText('#1')).toBeInTheDocument()
    expect(requested.every((value) => value === 'true')).toBe(true)
  })

  it('opens details from the fetched transaction when a row is clicked', async () => {
    useHandlers()
    server.use(
      http.get(url(endpoints.transaction(2)), () => HttpResponse.json({ ...tx(2, 2, 10, 5, false), walletId: 77 })),
    )
    const ui = userEvent.setup()
    renderApp('/market/transactions')

    await ui.click(await screen.findByText('#2'))
    const dialog = await screen.findByRole('dialog')

    expect(await within(dialog).findByText('77')).toBeInTheDocument()
    expect(within(dialog).getByText('Ether')).toBeInTheDocument()
    expect(within(dialog).getByText('$50')).toBeInTheDocument()
    expect(within(dialog).getByText('SELL')).toBeInTheDocument()
  })

  it('labels a deleted coin', async () => {
    useHandlers()
    renderApp('/market/transactions')

    expect(await screen.findByText('Crypto #9')).toBeInTheDocument()
  })

  it('shows an empty state with no transactions', async () => {
    useHandlers([])
    renderApp('/market/transactions')

    expect(await screen.findByText('No transactions found.')).toBeInTheDocument()
  })
})
