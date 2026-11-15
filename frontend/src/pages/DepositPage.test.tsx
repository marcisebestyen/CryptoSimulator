import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { beforeEach, describe, expect, it } from 'vitest'
import { endpoints } from '../api/endpoints'
import { TOKEN_KEY } from '../auth/token'
import { makeJwt } from '../test/jwt'
import { renderApp } from '../test/renderApp'
import { server } from '../test/server'

const url = (path: string) => `http://localhost:3000${path}`
const portfolio = (fiatBalance: number) => ({
  userId: 7,
  fiatBalance,
  holdings: [],
  totalCryptoValue: 0,
  totalPortfolioValue: fiatBalance,
})
const savedCard = {
  id: 3,
  holderName: 'ALEX MERCER',
  last4: '8896',
  network: 'Visa',
  expiryMonth: 9,
  expiryYear: 2099,
}
const deposits = [
  { id: 1, amount: 100, last4: '1111', network: 'Visa', date: '2026-01-01T12:00:00Z' },
  { id: 2, amount: 2500, last4: '2222', network: 'Visa', date: '2026-03-01T12:00:00Z' },
]

interface Options {
  cards?: unknown[]
  balance?: { current: number }
}

function useHandlers({ cards = [], balance = { current: 1234 } }: Options = {}) {
  server.use(
    http.get(url(endpoints.portfolio(7)), () => HttpResponse.json(portfolio(balance.current))),
    http.get(url(endpoints.userCards(7)), () => HttpResponse.json(cards)),
    http.get(url(endpoints.userDeposits(7)), () => HttpResponse.json(deposits)),
  )
}

describe('DepositPage', () => {
  beforeEach(() => {
    localStorage.clear()
    localStorage.setItem(TOKEN_KEY, makeJwt(Math.floor(Date.now() / 1000) + 3600, { nameid: '7' }))
  })

  it('fills the amount from a preset button and shows the balance', async () => {
    useHandlers()
    const ui = userEvent.setup()
    renderApp('/deposit')

    expect(await screen.findByText('$1,234')).toBeInTheDocument()
    await ui.click(screen.getByRole('button', { name: '$500' }))

    expect(screen.getByLabelText('Amount')).toHaveValue('500')
    expect(screen.getByRole('button', { name: 'Deposit $500.00 to Wallet' })).toBeInTheDocument()
  })

  it('lists recent deposits newest first', async () => {
    useHandlers()
    renderApp('/deposit')

    const amounts = await screen.findAllByText(/^\+\$/)
    expect(amounts.map((node) => node.textContent)).toEqual(['+$2,500', '+$100'])
    expect(screen.getByText(/•••• 2222/)).toBeInTheDocument()
  })

  it('lists saved cards and hides the new-card fields', async () => {
    useHandlers({ cards: [savedCard] })
    renderApp('/deposit')

    expect(await screen.findByDisplayValue('VISA •••• 8896')).toBeInTheDocument()
    expect(screen.queryByLabelText('Card number')).not.toBeInTheDocument()
    expect(screen.queryByLabelText('Cardholder name')).not.toBeInTheDocument()
    expect(screen.queryByRole('checkbox', { name: /save card/i })).not.toBeInTheDocument()
    expect(screen.getByLabelText('CVV')).toBeInTheDocument()
  })

  it('deposits with a saved card, sends no card number and refreshes the balance', async () => {
    const balance = { current: 1234 }
    useHandlers({ cards: [savedCard], balance })
    let posted: Record<string, unknown> | null = null
    server.use(
      http.post(url(endpoints.deposits), async ({ request }) => {
        posted = (await request.json()) as Record<string, unknown>
        balance.current = 1500
        return HttpResponse.json({
          id: 9,
          amount: 500,
          last4: '8896',
          network: 'Visa',
          date: '2026-05-01T00:00:00Z',
          newBalance: 1500,
        })
      }),
    )
    const ui = userEvent.setup()
    renderApp('/deposit')

    await screen.findByDisplayValue('VISA •••• 8896')
    await ui.click(screen.getByRole('button', { name: '$500' }))
    await ui.type(screen.getByLabelText('CVV'), '842')
    await ui.click(screen.getByRole('button', { name: 'Deposit $500.00 to Wallet' }))

    expect(await screen.findByText(/new balance/i)).toHaveTextContent('$1,500')
    expect(posted).toEqual({ userId: 7, amount: 500, savedCardId: 3, cvv: '842', saveCard: false })
    expect(await screen.findAllByText('$1,500')).not.toHaveLength(0)
    expect(screen.getByLabelText('CVV')).toHaveValue('')
  })

  it('sends a new card with digits only and fills the demo card', async () => {
    useHandlers()
    let posted: Record<string, unknown> | null = null
    server.use(
      http.post(url(endpoints.deposits), async ({ request }) => {
        posted = (await request.json()) as Record<string, unknown>
        return HttpResponse.json({ id: 9, amount: 100, last4: '8896', network: 'Visa', date: '2026-05-01T00:00:00Z', newBalance: 1100 })
      }),
    )
    const ui = userEvent.setup()
    renderApp('/deposit')

    await ui.click(await screen.findByRole('button', { name: 'Fill Demo Card' }))
    expect(screen.getByLabelText('Card number')).toHaveValue('4532 7109 3842 8896')
    await ui.click(screen.getByRole('button', { name: '$100' }))
    await ui.click(screen.getByRole('checkbox', { name: /save card/i }))
    await ui.click(screen.getByRole('button', { name: 'Deposit $100.00 to Wallet' }))

    await waitFor(() => expect(posted).not.toBeNull())
    expect(posted).toMatchObject({
      userId: 7,
      amount: 100,
      holderName: 'ALEX MERCER',
      cardNumber: '4532710938428896',
      expiryMonth: 9,
      cvv: '842',
      saveCard: true,
    })
    expect(posted).not.toHaveProperty('savedCardId')
    expect(await screen.findByLabelText('Card number')).toHaveValue('')
  })

  it('shows an inline error and sends no request for an invalid Luhn number', async () => {
    useHandlers()
    let requests = 0
    server.use(
      http.post(url(endpoints.deposits), () => {
        requests += 1
        return HttpResponse.json({})
      }),
    )
    const ui = userEvent.setup()
    renderApp('/deposit')

    await ui.click(await screen.findByRole('button', { name: 'Fill Demo Card' }))
    await ui.click(screen.getByRole('button', { name: '$100' }))
    const number = screen.getByLabelText('Card number')
    await ui.clear(number)
    await ui.type(number, '4532710938428890')
    await ui.click(screen.getByRole('button', { name: 'Deposit $100.00 to Wallet' }))

    expect(await screen.findByText('Enter a valid card number.')).toBeInTheDocument()
    expect(requests).toBe(0)
  })

  it('shows the server error message on a 400', async () => {
    useHandlers({ cards: [savedCard] })
    server.use(http.post(url(endpoints.deposits), () => new HttpResponse('Card was declined', { status: 400 })))
    const ui = userEvent.setup()
    renderApp('/deposit')

    await screen.findByDisplayValue('VISA •••• 8896')
    await ui.click(screen.getByRole('button', { name: '$100' }))
    await ui.type(screen.getByLabelText('CVV'), '842')
    await ui.click(screen.getByRole('button', { name: 'Deposit $100.00 to Wallet' }))

    expect(await screen.findByText('Card was declined')).toBeInTheDocument()
  })

  it('disables the submit button while the request is pending', async () => {
    useHandlers({ cards: [savedCard] })
    let release: () => void = () => undefined
    const gate = new Promise<void>((resolve) => {
      release = resolve
    })
    server.use(
      http.post(url(endpoints.deposits), async () => {
        await gate
        return HttpResponse.json({ id: 9, amount: 100, last4: '8896', network: 'Visa', date: '2026-05-01T00:00:00Z', newBalance: 1100 })
      }),
    )
    const ui = userEvent.setup()
    renderApp('/deposit')

    await screen.findByDisplayValue('VISA •••• 8896')
    await ui.click(screen.getByRole('button', { name: '$100' }))
    await ui.type(screen.getByLabelText('CVV'), '842')
    await ui.click(screen.getByRole('button', { name: 'Deposit $100.00 to Wallet' }))

    expect(await screen.findByRole('button', { name: /authorizing/i })).toBeDisabled()
    release()
    expect(await screen.findByText(/new balance/i)).toBeInTheDocument()
  })

  it('deletes a saved card and refreshes the list', async () => {
    let cards: unknown[] = [savedCard]
    server.use(
      http.get(url(endpoints.portfolio(7)), () => HttpResponse.json(portfolio(1000))),
      http.get(url(endpoints.userCards(7)), () => HttpResponse.json(cards)),
      http.get(url(endpoints.userDeposits(7)), () => HttpResponse.json([])),
      http.delete(url(endpoints.userCard(7, 3)), () => {
        cards = []
        return new HttpResponse(null, { status: 204 })
      }),
    )
    const ui = userEvent.setup()
    renderApp('/deposit')

    await ui.click(await screen.findByRole('button', { name: 'Delete card VISA •••• 8896' }))

    expect(await screen.findByLabelText('Card number')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /delete card/i })).not.toBeInTheDocument()
  })
})
