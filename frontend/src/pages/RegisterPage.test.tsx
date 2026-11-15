import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { beforeEach, describe, expect, it } from 'vitest'
import { endpoints } from '../api/endpoints'
import { loginAsValidUser, renderApp } from '../test/renderApp'
import { server } from '../test/server'

const base = 'http://localhost:3000'

const created = {
  user: { id: 1, role: 'User', username: 'alice', firstName: '', lastName: '', email: 'a@b.co' },
  recoveryCode: 'ABCD-EFGH-1234',
}

async function fillAndSubmit() {
  const user = userEvent.setup()
  await user.type(screen.getByLabelText(/username/i), 'alice')
  await user.type(screen.getByLabelText(/email/i), 'a@b.co')
  await user.type(screen.getByLabelText(/password/i), 'secret12')
  await user.click(screen.getByRole('button', { name: 'Create account' }))
  return user
}

describe('RegisterPage', () => {
  beforeEach(() => localStorage.clear())

  it('shows the heading', () => {
    renderApp('/register')

    expect(screen.getByRole('heading', { name: 'Create account' })).toBeInTheDocument()
  })

  it('shows the recovery code after registering and stays on the page', async () => {
    server.use(http.post(`${base}${endpoints.register}`, () => HttpResponse.json(created, { status: 201 })))
    renderApp('/register')

    await fillAndSubmit()

    expect(await screen.findByText('ABCD-EFGH-1234')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'I saved it' })).toBeInTheDocument()
    expect(screen.queryByRole('heading', { name: 'Welcome back' })).not.toBeInTheDocument()
  })

  it('goes to the login page after the code is saved', async () => {
    server.use(http.post(`${base}${endpoints.register}`, () => HttpResponse.json(created, { status: 201 })))
    renderApp('/register')

    const user = await fillAndSubmit()
    await user.click(await screen.findByRole('button', { name: 'I saved it' }))

    expect(await screen.findByRole('heading', { name: 'Welcome back' })).toBeInTheDocument()
  })

  it('shows API validation errors in an alert', async () => {
    server.use(
      http.post(`${base}${endpoints.register}`, () =>
        HttpResponse.json({ errors: ['Email is taken.'] }, { status: 400 }),
      ),
    )
    renderApp('/register')

    await fillAndSubmit()

    expect(await screen.findByRole('alert')).toHaveTextContent('Email is taken.')
  })

  it('disables the button while submitting', async () => {
    server.use(
      http.post(`${base}${endpoints.register}`, async () => {
        await new Promise((resolve) => setTimeout(resolve, 100))
        return HttpResponse.json(created, { status: 201 })
      }),
    )
    renderApp('/register')

    await fillAndSubmit()

    await waitFor(() => expect(screen.getByRole('button', { name: 'Create account' })).toBeDisabled())
    expect(await screen.findByText('ABCD-EFGH-1234')).toBeInTheDocument()
  })

  it('redirects a logged in user to the cryptos page', async () => {
    loginAsValidUser()
    server.use(http.get(`${base}${endpoints.cryptos}`, () => HttpResponse.json([])))
    renderApp('/register')

    expect(await screen.findByRole('heading', { name: 'Cryptos' })).toBeInTheDocument()
  })
})
