export const TOKEN_KEY = 'crypto-token'

const ROLE_CLAIM = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'
const USER_ID_CLAIM = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'

function readClaims(token: string): Record<string, unknown> | null {
  try {
    const payload = token.split('.')[1]
    if (!payload) return null
    const base64 = payload.replace(/-/g, '+').replace(/_/g, '/')
    const claims: unknown = JSON.parse(atob(base64))
    if (typeof claims === 'object' && claims !== null) {
      return Object.fromEntries(Object.entries(claims))
    }
    return null
  } catch {
    // Not a decodable JWT: documented result is "logged out".
    return null
  }
}

function readExpiry(token: string): number | null {
  const exp = readClaims(token)?.exp
  return typeof exp === 'number' ? exp : null
}

export function getRoleFromToken(token: string): string | null {
  const claims = readClaims(token)
  if (claims === null) return null
  const role = claims[ROLE_CLAIM] ?? claims.role
  return typeof role === 'string' ? role : null
}

export function getUserIdFromToken(token: string | null): number | null {
  if (token === null) return null
  const claims = readClaims(token)
  if (claims === null) return null
  const raw = claims[USER_ID_CLAIM] ?? claims.nameid
  if (typeof raw !== 'string' && typeof raw !== 'number') return null
  const id = Number(raw)
  return raw !== '' && Number.isInteger(id) ? id : null
}

export function readToken(): string | null {
  const token = localStorage.getItem(TOKEN_KEY)
  if (token === null) return null

  const exp = readExpiry(token)
  if (exp === null || exp * 1000 <= Date.now()) {
    clearToken()
    return null
  }
  return token
}

export function saveToken(token: string): void {
  localStorage.setItem(TOKEN_KEY, token)
}

export function clearToken(): void {
  localStorage.removeItem(TOKEN_KEY)
}
