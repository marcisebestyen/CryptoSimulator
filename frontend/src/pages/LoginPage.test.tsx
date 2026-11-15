import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { beforeEach, describe, expect, it } from 'vitest'
import { endpoints } from '../api/endpoints'
import { TOKEN_KEY } from '../auth/token'
import { makeJwt } from '../test/jwt'
import { loginAsValidUser, renderApp } from '../test/renderApp'
import { server } from '../test/server'

const base = 'http://localhost:3000'

async function fillAndSubmit() {
  const user = userEvent.setup()
  await user.type(screen.getByLabelText(/email/i), 'a@b.co')
  await user.type(screen.getByLabelText(/password/i), 'secret')
  await user.click(screen.getByRole('button', { name: 'Log in' }))
}

describe('LoginPage', () => {
  beforeEach(() => localStorage.clear())

  it('goes to the cryptos page when login succeeds', async () => {
    const token = makeJwt(Math.floor(Date.now() / 1000) + 3600)
    server.use(
      http.post(`${base}${endpoints.login}`, () =>
        HttpResponse.json({ token, user: { id: 1, username: 'a', email: 'a@b.co' } }),
      ),
      http.get(`${base}${endpoints.cryptos}`, () => HttpResponse.json([])),
    )
    renderApp('/login')

    await fillAndSubmit()

    expect(await screen.findByRole('heading', { name: 'Cryptos' })).toBeInTheDocument()
  })

  it('shows the greeting heading', () => {
    renderApp('/login')

    expect(screen.getByRole('heading', { name: 'Welcome back' })).toBeInTheDocument()
  })

  it('shows an error and stores no token when the password is wrong', async () => {
    server.use(
      http.post(`${base}${endpoints.login}`, () => new HttpResponse('Invalid email or password.', { status: 401 })),
    )
    renderApp('/login')

    await fillAndSubmit()

    expect(await screen.findByText('Invalid email or password.')).toBeInTheDocument()
    expect(localStorage.getItem(TOKEN_KEY)).toBeNull()
  })

  it('shows a readable error when the server is unreachable', async () => {
    server.use(http.post(`${base}${endpoints.login}`, () => HttpResponse.error()))
    renderApp('/login')

    await fillAndSubmit()

    expect(await screen.findByText('Cannot reach the server.')).toBeInTheDocument()
  })

  it('makes no request when fields are empty', async () => {
    let requests = 0
    server.use(
      http.post(`${base}${endpoints.login}`, () => {
        requests += 1
        return HttpResponse.json({})
      }),
    )
    renderApp('/login')

    await userEvent.setup().click(screen.getByRole('button', { name: 'Log in' }))

    expect(requests).toBe(0)
    expect(screen.getByRole('heading', { name: 'Welcome back' })).toBeInTheDocument()
  })

  it('redirects a logged in user to the cryptos page', async () => {
    loginAsValidUser()
    server.use(http.get(`${base}${endpoints.cryptos}`, () => HttpResponse.json([])))
    renderApp('/login')

    expect(await screen.findByRole('heading', { name: 'Cryptos' })).toBeInTheDocument()
  })

  it('links to registration and password recovery', () => {
    renderApp('/login')

    expect(screen.getByRole('link', { name: 'Create an account' })).toHaveAttribute('href', '/register')
    expect(screen.getByRole('link', { name: 'Forgot password?' })).toHaveAttribute('href', '/forgot-password')
  })
})
