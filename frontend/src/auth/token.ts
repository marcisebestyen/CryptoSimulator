export const TOKEN_KEY = 'crypto-token'

function readExpiry(token: string): number | null {
  try {
    const payload = token.split('.')[1]
    if (!payload) return null
    const base64 = payload.replace(/-/g, '+').replace(/_/g, '/')
    const claims: unknown = JSON.parse(atob(base64))
    if (typeof claims === 'object' && claims !== null && 'exp' in claims && typeof claims.exp === 'number') {
      return claims.exp
    }
    return null
  } catch {
    // Not a decodable JWT: documented result is "logged out".
    return null
  }
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
