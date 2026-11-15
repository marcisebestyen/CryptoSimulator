import { apiFetch } from './client'
import { endpoints } from './endpoints'

export interface Transaction {
  id: number
  walletId: number
  cryptoId: number
  amount: number
  exchangeRate: number
  date: string
  isPurchase: boolean
  transactionType: string
}

export function fetchTransactions(userId: number, newestFirst = true): Promise<Transaction[]> {
  const query = new URLSearchParams({ newestFirst: String(newestFirst) })
  return apiFetch<Transaction[]>(`${endpoints.transactions(userId)}?${query}`)
}

export function fetchTransaction(id: number): Promise<Transaction> {
  return apiFetch<Transaction>(endpoints.transaction(id))
}
