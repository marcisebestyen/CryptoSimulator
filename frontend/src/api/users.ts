import { apiFetch } from './client'
import { jsonRequest } from './cryptos'
import { endpoints } from './endpoints'

export interface User {
  id: number
  role: string
  username: string
  firstName: string
  lastName: string
  email: string
}

export interface UserUpdate {
  username: string
  firstName: string
  lastName: string
}

export function fetchUser(id: number): Promise<User> {
  return apiFetch<User>(endpoints.user(id))
}

export function updateUser(id: number, body: UserUpdate): Promise<User> {
  return apiFetch<User>(endpoints.user(id), jsonRequest('PUT', body))
}

export interface RegisterResponse {
  user: User
  recoveryCode: string
}

export interface RecoveryCodeResponse {
  recoveryCode: string
}

export function register(username: string, email: string, password: string): Promise<RegisterResponse> {
  return apiFetch<RegisterResponse>(endpoints.register, {
    ...jsonRequest('POST', { username, email, password }),
    skipUnauthorizedHandler: true,
  })
}

export function forgotPassword(
  email: string,
  recoveryCode: string,
  newPassword: string,
): Promise<RecoveryCodeResponse> {
  return apiFetch<RecoveryCodeResponse>(endpoints.forgotPassword, {
    ...jsonRequest('POST', { email, recoveryCode, newPassword }),
    skipUnauthorizedHandler: true,
  })
}
