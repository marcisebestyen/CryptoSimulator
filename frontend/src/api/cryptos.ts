import { apiFetch } from './client'
import { endpoints } from './endpoints'

export interface Crypto {
  id: number
  name: string
  currentExchangeRate: number
}

export function fetchCryptos(): Promise<Crypto[]> {
  return apiFetch<Crypto[]>(endpoints.cryptos)
}
