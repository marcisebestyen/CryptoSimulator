import { MantineProvider } from '@mantine/core'
import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import type { CryptoProfitLoss, PortfolioProfitLoss } from '../api/trades'
import { theme } from '../theme'
import { ProfitLossSection } from './ProfitLossSection'

const summary: PortfolioProfitLoss = {
  userId: 1,
  totalCurrentCryptoValue: 200,
  totalCostBasis: 154.5,
  totalProfitLossAmount: 45.5,
  totalProfitLossPercentage: 29.45,
}
const row: CryptoProfitLoss = {
  cryptoId: 1,
  cryptoName: 'Bitcoin',
  amountOwned: 2,
  avgPurchasePrice: 77.25,
  currentExchangeRate: 100,
  totalCostBasis: 154.5,
  currentValue: 200,
  profitLossAmount: -3,
  profitLossPercentage: -1.5,
}

const renderWithProviders = (ui: React.ReactNode) => render(<MantineProvider theme={theme}>{ui}</MantineProvider>)

describe('ProfitLossSection', () => {
  it('renders summary and a row with sign states', () => {
    renderWithProviders(<ProfitLossSection summary={summary} rows={[row]} />)

    expect(screen.getByText('+45.5')).toHaveAttribute('data-sign', 'positive')
    expect(screen.getByText('Bitcoin')).toBeInTheDocument()
    expect(screen.getByText('77.25')).toBeInTheDocument()
    expect(screen.getByText('-3')).toHaveAttribute('data-sign', 'negative')
    expect(screen.getByText('-1.5%')).toHaveAttribute('data-sign', 'negative')
  })

  it('shows an empty message without rows', () => {
    renderWithProviders(<ProfitLossSection summary={summary} rows={[]} />)

    expect(screen.getByText('No holdings yet.')).toBeInTheDocument()
    expect(screen.queryByRole('table')).not.toBeInTheDocument()
  })
})
