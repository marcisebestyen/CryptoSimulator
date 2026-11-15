import { apiFetch } from './client'
import { jsonRequest } from './cryptos'
import { endpoints } from './endpoints'

export type CardNetwork = 'Visa' | 'Mastercard' | 'Amex'

export interface SavedCard {
  id: number
  holderName: string
  last4: string
  network: CardNetwork
  expiryMonth: number
  expiryYear: number
}

export interface DepositHistoryItem {
  id: number
  amount: number
  last4: string
  network: CardNetwork
  date: string
}

export interface DepositResult extends DepositHistoryItem {
  newBalance: number
}

export interface DepositRequest {
  userId: number
  amount: number
  savedCardId?: number
  holderName?: string
  cardNumber?: string
  expiryMonth?: number
  expiryYear?: number
  cvv: string
  saveCard: boolean
}

export function createDeposit(request: DepositRequest): Promise<DepositResult> {
  return apiFetch<DepositResult>(endpoints.deposits, jsonRequest('POST', request))
}

export function fetchDeposits(userId: number): Promise<DepositHistoryItem[]> {
  return apiFetch<DepositHistoryItem[]>(endpoints.userDeposits(userId))
}

export function fetchCards(userId: number): Promise<SavedCard[]> {
  return apiFetch<SavedCard[]>(endpoints.userCards(userId))
}

export function deleteCard(userId: number, cardId: number): Promise<void> {
  return apiFetch<void>(endpoints.userCard(userId, cardId), { method: 'DELETE' })
}
