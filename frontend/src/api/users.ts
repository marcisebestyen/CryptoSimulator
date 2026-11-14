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

export interface OwnedCrypto {
  cryptoId: number
  cryptoName: string
  amount: number
  currentPrice: number
  currentValue: number
}

export interface UserWallet {
  userId: number
  fiatBalance: number
  ownedCryptos: OwnedCrypto[]
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

export function fetchWallet(id: number): Promise<UserWallet> {
  return apiFetch<UserWallet>(endpoints.userWallet(id))
}
