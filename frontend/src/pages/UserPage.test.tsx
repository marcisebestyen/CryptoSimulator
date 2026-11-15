import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { beforeEach, describe, expect, it } from 'vitest'
import { endpoints } from '../api/endpoints'
import { TOKEN_KEY } from '../auth/token'
import { makeJwt } from '../test/jwt'
import { renderApp } from '../test/renderApp'
import { server } from '../test/server'

const userUrl = `http://localhost:3000${endpoints.user(7)}`
const portfolioUrl = `http://localhost:3000${endpoints.portfolio(7)}`
const profitLossUrl = `http://localhost:3000${endpoints.profitLoss(7)}`
const summaryUrl = `http://localhost:3000${endpoints.profitLossSummary(7)}`

const user = { id: 7, role: 'User', username: 'jane', firstName: 'Jane', lastName: 'Doe', email: 'jane@example.com' }
const portfolio = {
  userId: 7,
  fiatBalance: 1234.5,
  holdings: [
    { cryptoId: 1, cryptoName: 'Bitcoin', amountOwned: 2, currentExchangeRate: 100, currentValue: 200 },
    { cryptoId: 2, cryptoName: 'Ether', amountOwned: 0, currentExchangeRate: 5, currentValue: 0 },
  ],
  totalCryptoValue: 200,
  totalPortfolioValue: 1434.5,
}
const rows = [
  {
    cryptoId: 1,
    cryptoName: 'Bitcoin',
    amountOwned: 2,
    avgPurchasePrice: 77.25,
    currentExchangeRate: 100,
    totalCostBasis: 154.5,
    currentValue: 200,
    profitLossAmount: 44.25,
    profitLossPercentage: 28.5,
  },
]
const summary = {
  userId: 7,
  totalCurrentCryptoValue: 200,
  totalCostBasis: 154.5,
  totalProfitLossAmount: 45.5,
  totalProfitLossPercentage: 29.45,
}

const money = (value: number) => value.toLocaleString(undefined, { maximumFractionDigits: 2 })

function useHandlers(portfolioBody: typeof portfolio = portfolio, rowsBody: typeof rows = rows, summaryBody: typeof summary = summary) {
  server.use(
    http.get(userUrl, () => HttpResponse.json(user)),
    http.get(portfolioUrl, () => HttpResponse.json(portfolioBody)),
    http.get(profitLossUrl, () => HttpResponse.json(rowsBody)),
    http.get(summaryUrl, () => HttpResponse.json(summaryBody)),
  )
}

describe('UserPage', () => {
  beforeEach(() => {
    localStorage.clear()
    localStorage.setItem(TOKEN_KEY, makeJwt(Math.floor(Date.now() / 1000) + 3600, { nameid: '7' }))
  })

  it('shows the header with initials, name, username and email chip, no copy button', async () => {
    useHandlers()
    renderApp('/profile')

    expect(await screen.findByText('Jane Doe')).toBeInTheDocument()
    expect(screen.getByText('JD')).toBeInTheDocument()
    expect(screen.getByText('@jane')).toBeInTheDocument()
    expect(screen.getByText('jane@example.com')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /copy/i })).not.toBeInTheDocument()
  })

  it('shows username and email as disabled inputs in the edit modal', async () => {
    useHandlers()
    renderApp('/profile')
    const ui = userEvent.setup()

    await ui.click(await screen.findByRole('button', { name: 'Edit profile' }))

    expect(await screen.findByLabelText('Username')).toBeDisabled()
    expect(screen.getByLabelText('Username')).toHaveValue('jane')
    expect(screen.getByLabelText('Email')).toBeDisabled()
    expect(screen.getByLabelText('Email')).toHaveValue('jane@example.com')
  })

  it('saves changed names with PUT keeping the username, closes the modal and shows success', async () => {
    let body: unknown = null
    useHandlers()
    server.use(
      http.put(userUrl, async ({ request }) => {
        body = await request.json()
        return HttpResponse.json({ ...user, firstName: 'Janet' })
      }),
    )
    renderApp('/profile')
    const ui = userEvent.setup()

    await ui.click(await screen.findByRole('button', { name: 'Edit profile' }))
    const first = await screen.findByLabelText('First name')
    await ui.clear(first)
    await ui.type(first, 'Janet')
    await ui.click(screen.getByRole('button', { name: 'Save' }))

    expect(await screen.findByText('Profile saved.')).toBeInTheDocument()
    expect(body).toEqual({ username: 'jane', firstName: 'Janet', lastName: 'Doe' })
    await waitFor(() => expect(screen.queryByLabelText('First name')).not.toBeInTheDocument())
  })

  it('resets the form to current values when the modal is reopened', async () => {
    useHandlers()
    renderApp('/profile')
    const ui = userEvent.setup()

    await ui.click(await screen.findByRole('button', { name: 'Edit profile' }))
    await ui.type(await screen.findByLabelText('First name'), 'XYZ')
    await ui.click(screen.getByRole('button', { name: 'Cancel' }))
    await waitFor(() => expect(screen.queryByLabelText('First name')).not.toBeInTheDocument())
    await ui.click(screen.getByRole('button', { name: 'Edit profile' }))

    expect(await screen.findByLabelText('First name')).toHaveValue('Jane')
  })

  it('disables Save when a name is blank', async () => {
    useHandlers()
    renderApp('/profile')
    const ui = userEvent.setup()

    await ui.click(await screen.findByRole('button', { name: 'Edit profile' }))
    await ui.clear(await screen.findByLabelText('Last name'))

    expect(screen.getByRole('button', { name: 'Save' })).toBeDisabled()
  })

  it('shows save errors inside the modal', async () => {
    useHandlers()
    server.use(http.put(userUrl, () => new HttpResponse('Nope', { status: 400 })))
    renderApp('/profile')
    const ui = userEvent.setup()

    await ui.click(await screen.findByRole('button', { name: 'Edit profile' }))
    await ui.click(await screen.findByRole('button', { name: 'Save' }))

    expect(await screen.findByText('Could not save')).toBeInTheDocument()
  })

  it('shows the balance row with fiat, holdings and total worth', async () => {
    useHandlers()
    renderApp('/profile')

    expect(await screen.findByText(money(1234.5))).toBeInTheDocument()
    expect(screen.getByText(money(1434.5))).toBeInTheDocument()
    expect(screen.getByText('Fiat balance')).toBeInTheDocument()
    expect(screen.getByText('Total worth')).toBeInTheDocument()
  })

  it('shows allocation rows with name, amount, value and share, plus the chart', async () => {
    useHandlers()
    renderApp('/profile')

    const card = (await screen.findByRole('heading', { name: 'Allocation' })).closest('.mantine-Card-root') as HTMLElement
    expect(within(card).getByText('Bitcoin')).toBeInTheDocument()
    expect(within(card).queryByRole('columnheader')).toBeNull()
    expect(screen.getAllByText(money(200)).length).toBeGreaterThan(0)
    expect(screen.getByText('100.0%')).toBeInTheDocument()
    expect(screen.getByText('0.0%')).toBeInTheDocument()
    expect(screen.getByText('Ether')).toBeInTheDocument()
    expect(screen.getByText('2 coins')).toBeInTheDocument()
    expect(screen.getByRole('img', { name: 'Holdings chart' })).toBeInTheDocument()
  })

  it('shows an empty state and no chart for an empty portfolio', async () => {
    useHandlers({ ...portfolio, holdings: [], totalCryptoValue: 0 }, [])
    renderApp('/profile')

    expect(await screen.findByText('You do not own any crypto yet.')).toBeInTheDocument()
    expect(screen.queryByRole('img', { name: 'Holdings chart' })).not.toBeInTheDocument()
  })

  it('shows an error when the user fails to load', async () => {
    useHandlers()
    server.use(http.get(userUrl, () => new HttpResponse('boom', { status: 500 })))
    renderApp('/profile')

    expect(await screen.findByText('Could not load profile')).toBeInTheDocument()
  })

  it('shows an error when the portfolio fails to load', async () => {
    useHandlers()
    server.use(http.get(portfolioUrl, () => HttpResponse.error()))
    renderApp('/profile')

    expect(await screen.findByText('Could not load portfolio')).toBeInTheDocument()
  })

  it('shows profit and loss summary with amount, percent and sign state', async () => {
    useHandlers()
    renderApp('/profile')

    const amount = await screen.findByText('+45.5')
    expect(amount).toHaveAttribute('data-sign', 'positive')
    expect(screen.getByText('+29.45%')).toHaveAttribute('data-sign', 'positive')
    expect(screen.getByText('Cost basis')).toBeInTheDocument()
    expect(screen.getAllByText('154.5').length).toBeGreaterThan(0)
  })

  it('marks a negative profit and loss as negative', async () => {
    useHandlers(portfolio, rows, { ...summary, totalProfitLossAmount: -20.5, totalProfitLossPercentage: -11.2 })
    renderApp('/profile')

    expect(await screen.findByText('-20.5')).toHaveAttribute('data-sign', 'negative')
    expect(screen.getByText('-11.2%')).toHaveAttribute('data-sign', 'negative')
  })

  it('shows the average purchase price per coin', async () => {
    useHandlers()
    renderApp('/profile')

    expect(await screen.findByText('77.25')).toBeInTheDocument()
  })

  it('shows an empty profit and loss state without holdings', async () => {
    useHandlers(portfolio, [])
    renderApp('/profile')

    expect(await screen.findByText('No holdings yet.')).toBeInTheDocument()
  })

  it('shows an error when profit and loss fails to load', async () => {
    useHandlers()
    server.use(http.get(summaryUrl, () => HttpResponse.error()))
    renderApp('/profile')

    expect(await screen.findByText('Could not load profit and loss')).toBeInTheDocument()
  })

  it('has a Profile nav link', async () => {
    useHandlers()
    renderApp('/profile')

    expect(await screen.findByRole('link', { name: 'Profile' })).toHaveAttribute('href', '/profile')
  })
})
