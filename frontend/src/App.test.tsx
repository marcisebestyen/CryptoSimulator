import { screen } from '@testing-library/react'
import { http, HttpResponse } from 'msw'
import { beforeEach, describe, expect, it } from 'vitest'
import { endpoints } from './api/endpoints'
import { loginAsValidUser, renderApp } from './test/renderApp'
import { server } from './test/server'

describe('App routing', () => {
  beforeEach(() => {
    localStorage.clear()
    server.use(http.get(`http://localhost:3000${endpoints.cryptos}`, () => HttpResponse.json([])))
  })

  it('sends an unauthenticated user from /cryptos to login', () => {
    renderApp('/cryptos')

    expect(screen.getByRole('heading', { name: 'Welcome back' })).toBeInTheDocument()
  })

  it('sends an authenticated user from / to cryptos', async () => {
    loginAsValidUser()
    renderApp('/')

    expect(await screen.findByRole('heading', { name: 'Cryptos' })).toBeInTheDocument()
  })

  it('sends an unknown path to cryptos', async () => {
    loginAsValidUser()
    renderApp('/nope/nothing')

    expect(await screen.findByRole('heading', { name: 'Cryptos' })).toBeInTheDocument()
  })
})
