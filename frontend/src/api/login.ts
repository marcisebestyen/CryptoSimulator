import { apiFetch } from './client'
import { endpoints } from './endpoints'

export interface LoginUser {
  id: number
  username: string
  email: string
}

export interface LoginResponse {
  token: string
  user: LoginUser
}

export function login(email: string, password: string): Promise<LoginResponse> {
  return apiFetch<LoginResponse>(endpoints.login, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password }),
    skipUnauthorizedHandler: true,
  })
}
