import { beforeEach, describe, expect, it } from 'vitest'
import { makeJwt } from '../test/jwt'
import { TOKEN_KEY, clearToken, getRoleFromToken, getUserIdFromToken, readToken, saveToken } from './token'

const USER_ID_CLAIM = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'
const inFuture = () => Math.floor(Date.now() / 1000) + 3600
const inPast = () => Math.floor(Date.now() / 1000) - 3600

describe('token storage', () => {
  beforeEach(() => localStorage.clear())

  it('returns a valid token', () => {
    const token = makeJwt(inFuture())
    saveToken(token)

    expect(readToken()).toBe(token)
  })

  it('returns null and clears storage when expired', () => {
    saveToken(makeJwt(inPast()))

    expect(readToken()).toBeNull()
    expect(localStorage.getItem(TOKEN_KEY)).toBeNull()
  })

  it('returns null and clears storage when malformed', () => {
    saveToken('garbage')

    expect(readToken()).toBeNull()
    expect(localStorage.getItem(TOKEN_KEY)).toBeNull()
  })

  it('returns null and clears storage when exp is missing', () => {
    const noExp = `${btoa('{}')}.${btoa('{"sub":"1"}')}.sig`
    saveToken(noExp)

    expect(readToken()).toBeNull()
    expect(localStorage.getItem(TOKEN_KEY)).toBeNull()
  })

  it('returns null when no token is stored', () => {
    expect(readToken()).toBeNull()
  })

  it('clearToken removes the token', () => {
    saveToken(makeJwt(inFuture()))
    clearToken()

    expect(localStorage.getItem(TOKEN_KEY)).toBeNull()
  })
})

const ROLE_CLAIM = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'

describe('getRoleFromToken', () => {
  it('returns Admin from long claim', () => {
    expect(getRoleFromToken(makeJwt(inFuture(), { [ROLE_CLAIM]: 'Admin' }))).toBe('Admin')
  })

  it('returns Admin from short role claim', () => {
    expect(getRoleFromToken(makeJwt(inFuture(), { role: 'Admin' }))).toBe('Admin')
  })

  it('returns null when no role claim', () => {
    expect(getRoleFromToken(makeJwt(inFuture()))).toBeNull()
  })

  it('returns null for a malformed token', () => {
    expect(getRoleFromToken('garbage')).toBeNull()
  })
})

describe('getUserIdFromToken', () => {
  it('returns the id from the long nameidentifier claim', () => {
    expect(getUserIdFromToken(makeJwt(inFuture(), { [USER_ID_CLAIM]: '42' }))).toBe(42)
  })

  it('returns the id from the short nameid claim', () => {
    expect(getUserIdFromToken(makeJwt(inFuture(), { nameid: '7' }))).toBe(7)
  })

  it('accepts a numeric claim value', () => {
    expect(getUserIdFromToken(makeJwt(inFuture(), { nameid: 5 }))).toBe(5)
  })

  it('returns null when the claim is missing', () => {
    expect(getUserIdFromToken(makeJwt(inFuture()))).toBeNull()
  })

  it('returns null when the claim is not a number', () => {
    expect(getUserIdFromToken(makeJwt(inFuture(), { nameid: 'abc' }))).toBeNull()
  })

  it('returns null for a null or malformed token', () => {
    expect(getUserIdFromToken(null)).toBeNull()
    expect(getUserIdFromToken('garbage')).toBeNull()
  })
})
