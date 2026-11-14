import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react'
import { setTokenGetter, setUnauthorizedHandler } from '../api/client'
import { login, type LoginUser } from '../api/login'
import { AuthContext, type AuthContextValue } from './useAuth'
import { clearToken, getRoleFromToken, getUserIdFromToken, readToken, saveToken } from './token'

export function AuthProvider({ children }: { children: ReactNode }) {
  const [token, setToken] = useState<string | null>(readToken)
  const [user, setUser] = useState<LoginUser | null>(null)

  const signOut = useCallback(() => {
    clearToken()
    setToken(null)
    setUser(null)
  }, [])

  const signIn = useCallback(async (email: string, password: string) => {
    const response = await login(email, password)
    saveToken(response.token)
    setToken(response.token)
    setUser(response.user)
  }, [])

  // Set during render so child effects (first API calls) already see them.
  setTokenGetter(readToken)

  useEffect(() => {
    setUnauthorizedHandler(signOut)
    return () => setUnauthorizedHandler(null)
  }, [signOut])

  const value = useMemo<AuthContextValue>(
    () => ({
      token,
      user,
      isAuthenticated: token !== null,
      userId: getUserIdFromToken(token),
      isAdmin: token !== null && getRoleFromToken(token) === 'Admin',
      signIn,
      signOut,
    }),
    [token, user, signIn, signOut],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
