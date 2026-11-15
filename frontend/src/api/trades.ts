import { apiFetch } from './client'
import { jsonRequest } from './cryptos'
import { endpoints } from './endpoints'

export interface PortfolioItem {
  cryptoId: number
  cryptoName: string
  amountOwned: number
  currentExchangeRate: number
  currentValue: number
}

export interface Portfolio {
  userId: number
  fiatBalance: number
  holdings: PortfolioItem[]
  totalCryptoValue: number
  totalPortfolioValue: number
}

export interface CryptoProfitLoss {
  cryptoId: number
  cryptoName: string
  amountOwned: number
  avgPurchasePrice: number
  currentExchangeRate: number
  totalCostBasis: number
  currentValue: number
  profitLossAmount: number
  profitLossPercentage: number
}

export interface PortfolioProfitLoss {
  userId: number
  totalCurrentCryptoValue: number
  totalCostBasis: number
  totalProfitLossAmount: number
  totalProfitLossPercentage: number
}

export interface TradeOrder {
  userId: number
  cryptoId: number
  amount: number
}

export function fetchPortfolio(userId: number): Promise<Portfolio> {
  return apiFetch<Portfolio>(endpoints.portfolio(userId))
}

export function fetchProfitLoss(userId: number): Promise<CryptoProfitLoss[]> {
  return apiFetch<CryptoProfitLoss[]>(endpoints.profitLoss(userId))
}

export function fetchProfitLossSummary(userId: number): Promise<PortfolioProfitLoss> {
  return apiFetch<PortfolioProfitLoss>(endpoints.profitLossSummary(userId))
}

export function buyCrypto(order: TradeOrder): Promise<void> {
  return apiFetch<void>(endpoints.tradeBuy, jsonRequest('POST', order))
}

export function sellCrypto(order: TradeOrder): Promise<void> {
  return apiFetch<void>(endpoints.tradeSell, jsonRequest('POST', order))
}
