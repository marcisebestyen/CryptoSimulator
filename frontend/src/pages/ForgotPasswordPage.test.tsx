import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { beforeEach, describe, expect, it } from 'vitest'
import { endpoints } from '../api/endpoints'
import { loginAsValidUser, renderApp } from '../test/renderApp'
import { server } from '../test/server'

const base = 'http://localhost:3000'

async function fillAndSubmit(code = 'AbCd-EfGh-1234') {
  const user = userEvent.setup()
  await user.type(screen.getByLabelText(/email/i), 'a@b.co')
  await user.type(screen.getByLabelText(/recovery code/i), code)
  await user.type(screen.getByLabelText(/new password/i), 'newsecret12')
  await user.click(screen.getByRole('button', { name: 'Reset password' }))
  return user
}

describe('ForgotPasswordPage', () => {
  beforeEach(() => localStorage.clear())

  it('shows the heading and fields', () => {
    renderApp('/forgot-password')

    expect(screen.getByRole('heading', { name: 'Reset password' })).toBeInTheDocument()
    expect(screen.getByLabelText(/email/i)).toHaveAttribute('type', 'email')
    expect(screen.getByLabelText(/recovery code/i)).toBeInTheDocument()
    expect(screen.getByLabelText(/new password/i)).toBeInTheDocument()
  })

  it('shows the new code, says the old one is void, and goes to login', async () => {
    server.use(
      http.post(`${base}${endpoints.forgotPassword}`, () => HttpResponse.json({ recoveryCode: 'NEW-CODE-9999' })),
    )
    renderApp('/forgot-password')

    const user = await fillAndSubmit()

    expect(await screen.findByText('NEW-CODE-9999')).toBeInTheDocument()
    expect(screen.getByText(/old recovery code no longer works/i)).toBeInTheDocument()
    await user.click(screen.getByRole('button', { name: 'I saved it' }))

    expect(await screen.findByRole('heading', { name: 'Welcome back' })).toBeInTheDocument()
    expect(screen.queryByText('NEW-CODE-9999')).not.toBeInTheDocument()
  })

  it('sends the code with case kept and surrounding whitespace trimmed', async () => {
    let body: unknown
    server.use(
      http.post(`${base}${endpoints.forgotPassword}`, async ({ request }) => {
        body = await request.json()
        return HttpResponse.json({ recoveryCode: 'NEW-CODE-9999' })
      }),
    )
    renderApp('/forgot-password')

    await fillAndSubmit('  AbCd-EfGh-1234  ')

    await screen.findByText('NEW-CODE-9999')
    expect(body).toEqual({ email: 'a@b.co', recoveryCode: 'AbCd-EfGh-1234', newPassword: 'newsecret12' })
  })

  it('shows the server message and stays on the form for a wrong code', async () => {
    server.use(
      http.post(`${base}${endpoints.forgotPassword}`, () =>
        new HttpResponse('Invalid email or recovery code.', { status: 400 }),
      ),
    )
    renderApp('/forgot-password')

    await fillAndSubmit()

    expect(await screen.findByRole('alert')).toHaveTextContent('Invalid email or recovery code.')
    expect(screen.getByRole('heading', { name: 'Reset password' })).toBeInTheDocument()
  })

  it('links back to login', async () => {
    renderApp('/forgot-password')

    await userEvent.setup().click(screen.getByRole('link', { name: 'Back to login' }))

    expect(await screen.findByRole('heading', { name: 'Welcome back' })).toBeInTheDocument()
  })

  it('redirects a logged in user to the cryptos page', async () => {
    loginAsValidUser()
    server.use(http.get(`${base}${endpoints.cryptos}`, () => HttpResponse.json([])))
    renderApp('/forgot-password')

    expect(await screen.findByRole('heading', { name: 'Cryptos' })).toBeInTheDocument()
  })
})
