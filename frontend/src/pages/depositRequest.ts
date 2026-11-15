import type { DepositRequest } from '../api/deposits'
import { isLuhnValid, parseExpiry } from './cardFormat'

export const MIN_AMOUNT = 10
export const MAX_AMOUNT = 100000

export interface DepositInput {
  userId: number
  amount: number
  savedCardId: number | null
  holderName: string
  cardNumber: string
  expiry: string
  cvv: string
  saveCard: boolean
}

export type DepositErrors = Partial<Record<'amount' | 'holderName' | 'cardNumber' | 'expiry' | 'cvv', string>>

const hasTwoDecimals = (value: number) => Math.abs(value * 100 - Math.round(value * 100)) < 1e-6

function isExpired(month: number, year: number, now: Date): boolean {
  return year < now.getFullYear() || (year === now.getFullYear() && month < now.getMonth() + 1)
}

export function validateDeposit(input: DepositInput, now = new Date()): DepositErrors {
  const errors: DepositErrors = {}
  if (!Number.isFinite(input.amount) || input.amount < MIN_AMOUNT || input.amount > MAX_AMOUNT || !hasTwoDecimals(input.amount)) {
    errors.amount = `Enter an amount between $${MIN_AMOUNT} and $${MAX_AMOUNT.toLocaleString()} with at most 2 decimals.`
  }
  if (!/^\d{3,4}$/.test(input.cvv)) errors.cvv = 'CVV must be 3 or 4 digits.'
  if (input.savedCardId !== null) return errors

  if (input.holderName.trim() === '') errors.holderName = 'Enter the cardholder name.'
  else if (input.holderName.trim().length > 100) errors.holderName = 'Cardholder name must be at most 100 characters.'
  const digits = input.cardNumber.replace(/\D/g, '')
  if (digits.length < 15 || digits.length > 16 || !isLuhnValid(digits)) errors.cardNumber = 'Enter a valid card number.'
  const expiry = parseExpiry(input.expiry)
  if (!expiry || isExpired(expiry.month, expiry.year, now)) errors.expiry = 'Enter a valid expiry date (MM/YY).'
  return errors
}

export function buildDepositRequest(input: DepositInput): DepositRequest {
  const base = { userId: input.userId, amount: input.amount, cvv: input.cvv }
  if (input.savedCardId !== null) return { ...base, savedCardId: input.savedCardId, saveCard: false }
  const expiry = parseExpiry(input.expiry)
  return {
    ...base,
    holderName: input.holderName.trim(),
    cardNumber: input.cardNumber.replace(/\D/g, ''),
    expiryMonth: expiry?.month,
    expiryYear: expiry?.year,
    saveCard: input.saveCard,
  }
}
