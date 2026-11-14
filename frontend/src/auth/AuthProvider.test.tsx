import { act, renderHook } from '@testing-library/react'
import { http, HttpResponse } from 'msw'
import { afterEach, beforeEach, describe, expect, it } from 'vitest'
import { apiFetch } from '../api/client'
import { endpoints } from '../api/endpoints'
import { server } from '../test/server'
import { AuthProvider } from './AuthProvider'
import { makeJwt } from '../test/jwt'
import { TOKEN_KEY } from './token'
import { useAuth } from './useAuth'

const base = 'http://localhost:3000'
const user = { id: 1, username: 'a', email: 'a@b.co' }
const future = () => Math.floor(Date.now() / 1000) + 3600

function renderProvider() {
  return renderHook(() => useAuth(), { wrapper: AuthProvider }).result
}

describe('AuthProvider', () => {
  beforeEach(() => localStorage.clear())
  afterEach(() => localStorage.clear())

  it('signIn stores the token and marks the user authenticated', async () => {
    const token = makeJwt(future())
    server.use(http.post(`${base}${endpoints.login}`, () => HttpResponse.json({ token, user })))
    const auth = renderProvider()

    await act(() => auth.current.signIn('a@b.co', 'pw'))

    expect(auth.current.isAuthenticated).toBe(true)
    expect(auth.current.token).toBe(token)
    expect(auth.current.user).toEqual(user)
    expect(localStorage.getItem(TOKEN_KEY)).toBe(token)
  })

  it('signIn rejects on 401 and stores nothing', async () => {
    server.use(http.post(`${base}${endpoints.login}`, () => new HttpResponse('bad', { status: 401 })))
    const auth = renderProvider()

    await expect(act(() => auth.current.signIn('a@b.co', 'nope'))).rejects.toMatchObject({ status: 401 })

    expect(auth.current.isAuthenticated).toBe(false)
    expect(localStorage.getItem(TOKEN_KEY)).toBeNull()
  })

  it('signOut clears the session', async () => {
    const token = makeJwt(future())
    server.use(http.post(`${base}${endpoints.login}`, () => HttpResponse.json({ token, user })))
    const auth = renderProvider()
    await act(() => auth.current.signIn('a@b.co', 'pw'))

    act(() => auth.current.signOut())

    expect(auth.current.isAuthenticated).toBe(false)
    expect(auth.current.user).toBeNull()
    expect(localStorage.getItem(TOKEN_KEY)).toBeNull()
  })

  it('signs out when any api call returns 401', async () => {
    const token = makeJwt(future())
    server.use(
      http.post(`${base}${endpoints.login}`, () => HttpResponse.json({ token, user })),
      http.get(`${base}/api/ping`, () => new HttpResponse('expired', { status: 401 })),
    )
    const auth = renderProvider()
    await act(() => auth.current.signIn('a@b.co', 'pw'))

    await act(() => apiFetch('/api/ping').catch(() => undefined))

    expect(auth.current.isAuthenticated).toBe(false)
    expect(localStorage.getItem(TOKEN_KEY)).toBeNull()
  })

  it('starts authenticated from a stored valid token', () => {
    localStorage.setItem(TOKEN_KEY, makeJwt(future()))
    const auth = renderProvider()

    expect(auth.current.isAuthenticated).toBe(true)
  })

  it('starts logged out when the stored token is malformed', () => {
    localStorage.setItem(TOKEN_KEY, 'garbage')
    const auth = renderProvider()

    expect(auth.current.isAuthenticated).toBe(false)
  })

  it('isAdmin is true for an Admin token', () => {
    localStorage.setItem(TOKEN_KEY, makeJwt(future(), { role: 'Admin' }))
    const auth = renderProvider()

    expect(auth.current.isAdmin).toBe(true)
  })

  it('isAdmin is false for a User token and when signed out', () => {
    localStorage.setItem(TOKEN_KEY, makeJwt(future(), { role: 'User' }))
    const userAuth = renderProvider()
    expect(userAuth.current.isAdmin).toBe(false)

    localStorage.clear()
    const signedOut = renderProvider()
    expect(signedOut.current.isAdmin).toBe(false)
  })

  it('isAdmin is false when the token has no role claim', () => {
    localStorage.setItem(TOKEN_KEY, makeJwt(future()))
    const auth = renderProvider()

    expect(auth.current.isAdmin).toBe(false)
  })
})

describe('useAuth', () => {
  it('throws outside the provider', () => {
    expect(() => renderHook(() => useAuth())).toThrow()
  })
})
