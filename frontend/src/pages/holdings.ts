import type { PortfolioItem } from '../api/trades'

const SLICE_COLORS = ['blue.6', 'teal.6', 'grape.6', 'orange.6', 'cyan.6', 'pink.6', 'lime.6', 'yellow.6']

export const formatMoney = (value: number) => value.toLocaleString(undefined, { maximumFractionDigits: 2 })
export const formatPrice = (value: number) => value.toLocaleString(undefined, { maximumFractionDigits: 8 })
export const formatAmount = (value: number) => value.toLocaleString(undefined, { maximumFractionDigits: 4 })

export function toSlices(holdings: PortfolioItem[]) {
  return holdings
    .filter((holding) => holding.currentValue > 0)
    .map((holding, index) => ({
      name: holding.cryptoName,
      value: holding.currentValue,
      color: SLICE_COLORS[index % SLICE_COLORS.length],
    }))
}

export const totalValue = (holdings: PortfolioItem[]) => holdings.reduce((sum, holding) => sum + holding.currentValue, 0)

export const sharePercent = (value: number, total: number) => (total > 0 ? (value / total) * 100 : 0)

export const sortByValue = (holdings: PortfolioItem[]) =>
  [...holdings].sort((a, b) => b.currentValue - a.currentValue)

export const initials = (firstName: string, lastName: string) =>
  `${firstName.trim().charAt(0)}${lastName.trim().charAt(0)}`.toUpperCase() || 'U'
