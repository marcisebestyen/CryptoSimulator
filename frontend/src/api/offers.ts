import { apiFetch } from './client'
import { jsonRequest } from './cryptos'
import { endpoints } from './endpoints'

export type OfferStatus = 'Open' | 'PartlySold' | 'SoldOut' | 'Cancelled'

export interface MarketOffer {
  id: number
  sellerUsername: string
  sellerCompletionRate: number | null
  sellerTrades: number
  cryptoId: number
  cryptoName: string
  remainingAmount: number
  askingPrice: number
  currentRate: number
}

export interface MyOffer {
  id: number
  cryptoId: number
  cryptoName: string
  amount: number
  remainingAmount: number
  soldAmount: number
  askingPrice: number
  currentRate: number
  status: OfferStatus
  createdAt: string
}

export interface NewOffer {
  cryptoId: number
  amount: number
  askingPrice: number
}

export function fetchMarketOffers(): Promise<MarketOffer[]> {
  return apiFetch<MarketOffer[]>(endpoints.tradeOffers)
}

export function fetchMyOffers(): Promise<MyOffer[]> {
  return apiFetch<MyOffer[]>(endpoints.tradeMyOffers)
}

export function createOffer(offer: NewOffer): Promise<MyOffer> {
  return apiFetch<MyOffer>(endpoints.tradeOffers, jsonRequest('POST', offer))
}

export function cancelOffer(id: number): Promise<void> {
  return apiFetch<void>(endpoints.tradeOffer(id), { method: 'DELETE' })
}

export function buyOffer(id: number, amount: number): Promise<void> {
  return apiFetch<void>(endpoints.tradeOfferBuy(id), jsonRequest('POST', { amount }))
}
