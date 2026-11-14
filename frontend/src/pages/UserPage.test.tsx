import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { beforeEach, describe, expect, it } from 'vitest'
import { endpoints } from '../api/endpoints'
import { TOKEN_KEY } from '../auth/token'
import { makeJwt } from '../test/jwt'
import { renderApp } from '../test/renderApp'
import { server } from '../test/server'

const userUrl = `http://localhost:3000${endpoints.user(7)}`
const walletUrl = `http://localhost:3000${endpoints.userWallet(7)}`

const user = { id: 7, role: 'User', username: 'jane', firstName: 'Jane', lastName: 'Doe', email: 'jane@example.com' }
const wallet = {
  userId: 7,
  fiatBalance: 1234.5,
  ownedCryptos: [
    { cryptoId: 1, cryptoName: 'Bitcoin', amount: 2, currentPrice: 100, currentValue: 200 },
    { cryptoId: 2, cryptoName: 'Ether', amount: 0, currentPrice: 5, currentValue: 0 },
  ],
}

const money = (value: number) => value.toLocaleString(undefined, { maximumFractionDigits: 2 })

function useHandlers(walletBody: typeof wallet = wallet) {
  server.use(
    http.get(userUrl, () => HttpResponse.json(user)),
    http.get(walletUrl, () => HttpResponse.json(walletBody)),
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

    expect(await screen.findByText('Bitcoin')).toBeInTheDocument()
    expect(screen.getAllByText(money(200)).length).toBeGreaterThan(0)
    expect(screen.getByText('100.0%')).toBeInTheDocument()
    expect(screen.getByText('0.0%')).toBeInTheDocument()
    expect(screen.getByText('Ether')).toBeInTheDocument()
    expect(screen.getByText('2 coins')).toBeInTheDocument()
    expect(screen.getByRole('img', { name: 'Holdings chart' })).toBeInTheDocument()
    expect(screen.queryByRole('columnheader')).not.toBeInTheDocument()
  })

  it('shows an empty state and no chart for an empty wallet', async () => {
    useHandlers({ ...wallet, ownedCryptos: [] })
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

  it('shows an error when the wallet fails to load', async () => {
    useHandlers()
    server.use(http.get(walletUrl, () => HttpResponse.error()))
    renderApp('/profile')

    expect(await screen.findByText('Could not load wallet')).toBeInTheDocument()
  })

  it('has a Profile nav link', async () => {
    useHandlers()
    renderApp('/profile')

    expect(await screen.findByRole('link', { name: 'Profile' })).toHaveAttribute('href', '/profile')
  })
})
