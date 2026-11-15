import { formatAmount, formatMoney } from './holdings'

export type TradeMode = 'buy' | 'sell'

const MIN_AMOUNT = 1e-8

const AMOUNT_PATTERN = /^\d+(\.\d{1,8})?$/

// Checks the raw text first: Number() would also accept '0x10', '1e-9' and more than 8 decimals.
export function amountTextError(text: string): string | null {
  return AMOUNT_PATTERN.test(text.trim()) ? null : 'Enter a valid amount with at most 8 decimals.'
}

export function tradeError(mode: TradeMode, amount: number, rate: number, balance: number, owned: number): string | null {
  if (!Number.isFinite(amount) || amount <= 0) return 'Enter an amount greater than 0.'
  if (amount < MIN_AMOUNT) return 'Minimum amount is 0.00000001.'
  if (mode === 'buy') {
    const shortfall = amount * rate - balance
    // Tiny epsilon so float noise on an exact-balance buy is not flagged.
    return shortfall > 1e-9 ? `Insufficient balance: you need $${formatMoney(shortfall)} more.` : null
  }
  return amount > owned ? `You only own ${formatAmount(owned)}.` : null
}

export function maxAmount(mode: TradeMode, balance: number, rate: number, owned: number): number {
  if (mode === 'sell') return owned
  return rate > 0 ? Math.floor((balance / rate) * 1e8) / 1e8 : 0
}
