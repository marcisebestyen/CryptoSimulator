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

  it('clears the token and shows login when logging out', async () => {
    renderApp('/cryptos')

    await userEvent.setup().click(await screen.findByRole('button', { name: 'Log out' }))

    expect(await screen.findByRole('heading', { name: 'Welcome back' })).toBeInTheDocument()
    expect(localStorage.getItem(TOKEN_KEY)).toBeNull()
  })
})
