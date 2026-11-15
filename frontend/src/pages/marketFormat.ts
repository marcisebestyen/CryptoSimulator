import { formatMoney } from './holdings'

export const cardStyle = { backgroundColor: 'var(--surface-raised)', borderColor: 'var(--grid-line-color)' }

export const signedMoney = (value: number, prefix = '$') =>
  `${value > 0 ? '+' : value < 0 ? '-' : ''}${prefix}${formatMoney(Math.abs(value))}`
