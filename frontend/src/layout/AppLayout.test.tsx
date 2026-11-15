import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { beforeEach, describe, expect, it } from 'vitest'
import { endpoints } from '../api/endpoints'
import { TOKEN_KEY } from '../auth/token'
import { loginAsValidUser, renderApp } from '../test/renderApp'
import { server } from '../test/server'

describe('AppLayout', () => {
  beforeEach(() => {
    localStorage.clear()
    loginAsValidUser()
    server.use(http.get(`http://localhost:3000${endpoints.cryptos}`, () => HttpResponse.json([])))
  })

  it('shows a Cryptos link in the sidebar', async () => {
    renderApp('/cryptos')

    expect(await screen.findByRole('link', { name: 'Cryptos' })).toBeInTheDocument()
  })

  it('shows Market and Transactions links with the right hrefs', async () => {
    renderApp('/cryptos')

    expect(await screen.findByRole('link', { name: 'Market' })).toHaveAttribute('href', '/market')
    expect(screen.getByRole('link', { name: 'Transactions' })).toHaveAttribute(
      'href',
      '/market/transactions',
    )
  })

  it('marks only the Transactions link active on the transactions page', async () => {
    renderApp('/market/transactions')

    const transactions = await screen.findByRole('link', { name: 'Transactions' })
    expect(transactions).toHaveAttribute('data-active', 'true')
    expect(screen.getByRole('link', { name: 'Market' })).not.toHaveAttribute('data-active')
  })

  it('navigates to the market and transactions pages from the sidebar', async () => {
    renderApp('/cryptos')
    const user = userEvent.setup()

    await user.click(await screen.findByRole('link', { name: 'Market' }))
    expect(await screen.findByRole('link', { name: 'Market' })).toHaveAttribute('data-active', 'true')

    await user.click(screen.getByRole('link', { name: 'Transactions' }))
    expect(await screen.findByRole('link', { name: 'Transactions' })).toHaveAttribute(
      'data-active',
      'true',
    )
  })

  it('shows the app name in the sidebar', async () => {
    renderApp('/cryptos')

    expect(await screen.findByText('Crypto Simulator')).toBeInTheDocument()
  })

  it('clears the token and shows login when logging out', async () => {
    renderApp('/cryptos')

    await userEvent.setup().click(await screen.findByRole('button', { name: 'Log out' }))

    expect(await screen.findByRole('heading', { name: 'Welcome back' })).toBeInTheDocument()
    expect(localStorage.getItem(TOKEN_KEY)).toBeNull()
  })
})
