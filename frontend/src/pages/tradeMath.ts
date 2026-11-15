import { formatAmount, formatMoney } from './holdings'

export type TradeMode = 'buy' | 'sell'

export const MIN_AMOUNT = 1e-4

const AMOUNT_PATTERN = /^\d+(\.\d{1,4})?$/

// Checks the raw text first: Number() would also accept '0x10', '1e-9' and more than 4 decimals.
export function amountTextError(text: string): string | null {
  return AMOUNT_PATTERN.test(text.trim()) ? null : 'Enter a valid amount with at most 4 decimals.'
}

export function tradeError(mode: TradeMode, amount: number, rate: number, balance: number, owned: number): string | null {
  if (!Number.isFinite(amount) || amount <= 0) return 'Enter an amount greater than 0.'
  if (amount < MIN_AMOUNT) return 'Minimum amount is 0.0001.'
  if (mode === 'buy') {
    const shortfall = amount * rate - balance
    // Tiny epsilon so float noise on an exact-balance buy is not flagged.
    return shortfall > 1e-9 ? `Insufficient balance: you need $${formatMoney(shortfall)} more.` : null
  }
  return amount > owned ? `You only own ${formatAmount(owned)}.` : null
}

// The epsilon absorbs float noise like 0.0003 * 1e4 = 2.9999999999999996.
export const floorAmount = (value: number) => Math.floor(value * 1e4 + 1e-6) / 1e4

export function maxAmount(mode: TradeMode, balance: number, rate: number, owned: number): number {
  if (mode === 'sell') return floorAmount(owned)
  return rate > 0 ? Math.floor((balance / rate) * 1e4) / 1e4 : 0
}
