import { Button, Card, Table, Text, Title } from '@mantine/core'
import type { Crypto } from '../api/cryptos'
import type { CryptoProfitLoss, PortfolioItem } from '../api/trades'
import { Coin } from './Coin'
import { formatAmount, formatMoney } from './holdings'
import { cardStyle, signedMoney } from './marketFormat'

interface HoldingsProps {
  holdings: PortfolioItem[]
  profitLoss: CryptoProfitLoss[]
  onSell: (cryptoId: number) => void
}

export function HoldingsTable({ holdings, profitLoss, onSell }: HoldingsProps) {
  const owned = holdings.filter((holding) => holding.amountOwned > 0)
  return (
    <Card withBorder style={cardStyle}>
      <Title order={3} mb="md">Your Holdings</Title>
      {owned.length === 0 ? (
        <Text>No holdings yet.</Text>
      ) : (
        <Table.ScrollContainer minWidth={640}>
          <Table>
            <Table.Thead>
              <Table.Tr>
                <Table.Th>Asset</Table.Th>
                <Table.Th ta="right">Amount</Table.Th>
                <Table.Th ta="right">Avg buy price</Table.Th>
                <Table.Th ta="right">Current value</Table.Th>
                <Table.Th ta="right">P&amp;L</Table.Th>
                <Table.Th />
              </Table.Tr>
            </Table.Thead>
            <Table.Tbody>
              {owned.map((holding) => {
                const row = profitLoss.find((item) => item.cryptoId === holding.cryptoId)
                return (
                  <Table.Tr key={holding.cryptoId}>
                    <Table.Td><Coin name={holding.cryptoName} /></Table.Td>
                    <Table.Td ta="right">{formatAmount(holding.amountOwned)}</Table.Td>
                    <Table.Td ta="right">{row ? `$${formatMoney(row.avgPurchasePrice)}` : '-'}</Table.Td>
                    <Table.Td ta="right">{`$${formatMoney(holding.currentValue)}`}</Table.Td>
                    <Table.Td ta="right">
                      {row ? (
                        <Text span c={row.profitLossAmount < 0 ? 'red' : 'teal'}>{signedMoney(row.profitLossAmount)}</Text>
                      ) : (
                        '-'
                      )}
                    </Table.Td>
                    <Table.Td ta="right">
                      <Button
                        size="compact-sm"
                        color="red"
                        variant="light"
                        aria-label={`Sell ${holding.cryptoName}`}
                        onClick={() => onSell(holding.cryptoId)}
                      >
                        Sell
                      </Button>
                    </Table.Td>
                  </Table.Tr>
                )
              })}
            </Table.Tbody>
          </Table>
        </Table.ScrollContainer>
      )}
    </Card>
  )
}

interface MarketProps {
  cryptos: Crypto[]
  holdings: PortfolioItem[]
  onBuy: (cryptoId: number) => void
}

export function LiveMarketTable({ cryptos, holdings, onBuy }: MarketProps) {
  return (
    <Card withBorder style={cardStyle}>
      <Title order={3} mb="md">Live Market</Title>
      {cryptos.length === 0 ? (
        <Text>No coins available.</Text>
      ) : (
        <Table.ScrollContainer minWidth={480}>
          <Table>
            <Table.Thead>
              <Table.Tr>
                <Table.Th>Coin</Table.Th>
                <Table.Th ta="right">Current rate</Table.Th>
                <Table.Th ta="right">You own</Table.Th>
                <Table.Th />
              </Table.Tr>
            </Table.Thead>
            <Table.Tbody>
              {cryptos.map((crypto) => {
                const amount = holdings.find((holding) => holding.cryptoId === crypto.id)?.amountOwned ?? 0
                return (
                  <Table.Tr key={crypto.id}>
                    <Table.Td><Coin name={crypto.name} /></Table.Td>
                    <Table.Td ta="right">{`$${formatMoney(crypto.currentExchangeRate)}`}</Table.Td>
                    <Table.Td ta="right">{formatAmount(amount)}</Table.Td>
                    <Table.Td ta="right">
                      <Button
                        size="compact-sm"
                        color="teal"
                        variant="light"
                        aria-label={`Buy ${crypto.name}`}
                        onClick={() => onBuy(crypto.id)}
                      >
                        Buy
                      </Button>
                    </Table.Td>
                  </Table.Tr>
                )
              })}
            </Table.Tbody>
          </Table>
        </Table.ScrollContainer>
      )}
    </Card>
  )
}
