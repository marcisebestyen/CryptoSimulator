import { createContext, useContext } from 'react'
import type { LoginUser } from '../api/login'

export interface AuthContextValue {
  token: string | null
  user: LoginUser | null
  isAuthenticated: boolean
  userId: number | null
  isAdmin: boolean
  signIn(email: string, password: string): Promise<void>
  signOut(): void
}

export const AuthContext = createContext<AuthContextValue | null>(null)

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext)
  if (context === null) {
    throw new Error('useAuth must be used inside AuthProvider')
  }
  return context
}
