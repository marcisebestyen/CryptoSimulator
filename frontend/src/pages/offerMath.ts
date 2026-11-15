import type { MarketOffer, MyOffer } from '../api/offers'
import { formatAmount, formatMoney } from './holdings'
import { amountTextError, MIN_AMOUNT } from './tradeMath'

export type OfferSort = 'best-deal' | 'lowest-price' | 'highest-total'

export const isOfferSort = (value: string): value is OfferSort =>
  value === 'best-deal' || value === 'lowest-price' || value === 'highest-total'

export interface OfferFilter {
  search: string
  coin: string
  sort: OfferSort
}

const PRICE_PATTERN = /^\d+(\.\d{1,8})?$/
const MAX_PRICE = 1e10

export const vsMarketPct = (askingPrice: number, rate: number) =>
  rate > 0 ? ((askingPrice - rate) / rate) * 100 : 0

// Same rule as the backend: 2 decimals, half away from zero. The epsilon absorbs float noise like 1.005 * 100.
export const offerTotal = (amount: number, price: number) => Math.round(amount * price * 100 + 1e-9) / 100

export function priceTextError(text: string): string | null {
  const trimmed = text.trim()
  if (!PRICE_PATTERN.test(trimmed)) return 'Enter a valid price with at most 8 decimals.'
  const price = Number(trimmed)
  if (price <= 0) return 'Price must be greater than 0.'
  return price >= MAX_PRICE ? 'Price must be below 10,000,000,000.' : null
}

function quantityError(text: string, limit: number, limitLabel: string): string | null {
  const textError = amountTextError(text)
  if (textError) return textError
  const amount = Number(text)
  if (amount < MIN_AMOUNT) return 'Minimum amount is 0.0001.'
  return amount > limit ? `${limitLabel} ${formatAmount(limit)}.` : null
}

export const sellQuantityError = (text: string, owned: number) => quantityError(text, owned, 'You only own')

export function buyQuantityError(text: string, remaining: number, price: number, cash: number): string | null {
  const error = quantityError(text, remaining, 'Only')
  if (error) return error
  const total = offerTotal(Number(text), price)
  if (total === 0) return 'Total is too small. Increase the quantity.'
  return total > cash ? `Insufficient balance: you need $${formatMoney(total - cash)} more.` : null
}

const dealRank = (offer: MarketOffer) => vsMarketPct(offer.askingPrice, offer.currentRate)
const totalOf = (offer: MarketOffer) => offerTotal(offer.remainingAmount, offer.askingPrice)

const COMPARATORS: Record<OfferSort, (a: MarketOffer, b: MarketOffer) => number> = {
  'best-deal': (a, b) => dealRank(a) - dealRank(b),
  'lowest-price': (a, b) => a.askingPrice - b.askingPrice,
  'highest-total': (a, b) => totalOf(b) - totalOf(a),
}

export function filterAndSortOffers(offers: MarketOffer[], { search, coin, sort }: OfferFilter): MarketOffer[] {
  const query = search.trim().toLowerCase()
  return offers
    .filter((offer) => coin === 'all' || String(offer.cryptoId) === coin)
    .filter(
      (offer) =>
        query === '' ||
        offer.cryptoName.toLowerCase().includes(query) ||
        offer.sellerUsername.toLowerCase().includes(query),
    )
    .sort(COMPARATORS[sort])
}

export const isActiveOffer = (offer: MyOffer) => offer.status === 'Open' || offer.status === 'PartlySold'

export const lockedValue = (offers: MyOffer[]) =>
  offers.filter(isActiveOffer).reduce((sum, offer) => sum + offer.remainingAmount * offer.currentRate, 0)
