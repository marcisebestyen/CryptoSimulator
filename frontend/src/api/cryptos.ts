import { apiFetch } from './client'
import { endpoints } from './endpoints'

export interface Crypto {
  id: number
  name: string
  currentExchangeRate: number
}

export interface PriceHistoryItem {
  value: number
  from: string
  to: string
}

export interface PagedResult<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
}

function jsonRequest(method: string, body: unknown): RequestInit {
  return {
    method,
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  }
}

export function fetchCryptos(): Promise<Crypto[]> {
  return apiFetch<Crypto[]>(endpoints.cryptos)
}

export function fetchCrypto(id: number): Promise<{ id: number; name: string }> {
  return apiFetch<{ id: number; name: string }>(endpoints.crypto(id))
}

export function createCrypto(name: string, initialPrice: number): Promise<Crypto> {
  return apiFetch<Crypto>(endpoints.cryptos, jsonRequest('POST', { name, initialPrice }))
}

export function renameCrypto(id: number, name: string): Promise<void> {
  return apiFetch<void>(endpoints.crypto(id), jsonRequest('PUT', { name }))
}

export function setCryptoPrice(id: number, newPrice: number): Promise<void> {
  return apiFetch<void>(endpoints.cryptoPrice(id), jsonRequest('PUT', { newPrice }))
}

export function deleteCrypto(id: number): Promise<void> {
  return apiFetch<void>(endpoints.crypto(id), { method: 'DELETE' })
}

export function fetchPriceHistory(
  id: number,
  page: number,
  pageSize: number,
): Promise<PagedResult<PriceHistoryItem>> {
  const query = new URLSearchParams({ page: String(page), pageSize: String(pageSize) })
  return apiFetch<PagedResult<PriceHistoryItem>>(`${endpoints.cryptoPriceHistory(id)}?${query}`)
}
