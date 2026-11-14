import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { describe, expect, it } from 'vitest'
import App from './App'
import { endpoints } from './api/endpoints'
import { server } from './test/server'

const loginUrl = `http://localhost:3000${endpoints.login}`

async function submitLogin() {
  const user = userEvent.setup()
  render(<App />)
  await user.type(screen.getByPlaceholderText('Email'), 'a@b.co')
  await user.type(screen.getByPlaceholderText('Password'), 'secret')
  await user.click(screen.getByRole('button', { name: 'Log in' }))
}

describe('App', () => {
  it('shows no message before submit', () => {
    render(<App />)

    expect(screen.queryByText(/logged in|backend/i)).not.toBeInTheDocument()
  })

  it('shows user email when login succeeds', async () => {
    server.use(http.post(loginUrl, () => HttpResponse.json({ token: 'jwt', user: { email: 'a@b.co' } })))

    await submitLogin()

    expect(await screen.findByText('Logged in as a@b.co')).toBeInTheDocument()
  })

  it('shows status and message when backend rejects login', async () => {
    server.use(http.post(loginUrl, () => new HttpResponse('Invalid email or password.', { status: 401 })))

    await submitLogin()

    expect(await screen.findByText('Backend answered 401: Invalid email or password.')).toBeInTheDocument()
  })

  it('tells backend is unreachable when request fails', async () => {
    server.use(http.post(loginUrl, () => HttpResponse.error()))

    await submitLogin()

    expect(await screen.findByText(/cannot reach backend/i)).toBeInTheDocument()
  })
})
