import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { beforeEach, describe, expect, it } from 'vitest'
import { endpoints } from '../api/endpoints'
import { loginAsValidUser, renderApp } from '../test/renderApp'
import { server } from '../test/server'

const url = `http://localhost:3000${endpoints.cryptos}`

describe('CryptosPage', () => {
  beforeEach(() => {
    localStorage.clear()
    loginAsValidUser()
  })

  it('renders rows with a formatted exchange rate', async () => {
    server.use(http.get(url, () => HttpResponse.json([{ id: 1, name: 'Bitcoin', currentExchangeRate: 43250.5 }])))
    renderApp('/cryptos')

    expect(await screen.findByText('Bitcoin')).toBeInTheDocument()
    expect(screen.getByText((43250.5).toLocaleString(undefined, { maximumFractionDigits: 8 }))).toBeInTheDocument()
  })

  it('selects a row when it is clicked and deselects on a second click', async () => {
    server.use(http.get(url, () => HttpResponse.json([{ id: 1, name: 'Bitcoin', currentExchangeRate: 1 }])))
    renderApp('/cryptos')
    const user = userEvent.setup()

    await user.click(await screen.findByText('Bitcoin'))
    expect(screen.getByRole('checkbox', { name: 'Toggle select row' })).toBeChecked()

    await user.click(screen.getByText('Bitcoin'))
    expect(screen.getByRole('checkbox', { name: 'Toggle select row' })).not.toBeChecked()
  })

  it('shows an empty state for an empty list', async () => {
    server.use(http.get(url, () => HttpResponse.json([])))
    renderApp('/cryptos')

    expect(await screen.findByText('No cryptos yet.')).toBeInTheDocument()
  })

  it('shows an error alert when the server fails and retries on click', async () => {
    server.use(http.get(url, () => new HttpResponse('boom', { status: 500 })))
    renderApp('/cryptos')

    expect(await screen.findByRole('alert')).toBeInTheDocument()

    server.use(http.get(url, () => HttpResponse.json([{ id: 2, name: 'Ether', currentExchangeRate: 1 }])))
    await userEvent.setup().click(screen.getByRole('button', { name: 'Retry' }))

    expect(await screen.findByText('Ether')).toBeInTheDocument()
  })

  it('shows a readable error when the server is unreachable', async () => {
    server.use(http.get(url, () => HttpResponse.error()))
    renderApp('/cryptos')

    expect(await screen.findByText('Cannot reach the server.')).toBeInTheDocument()
  })

  it('sends the user to login on 401', async () => {
    server.use(http.get(url, () => new HttpResponse('no', { status: 401 })))
    renderApp('/cryptos')

    expect(await screen.findByRole('heading', { name: 'Welcome back' })).toBeInTheDocument()
  })
})
