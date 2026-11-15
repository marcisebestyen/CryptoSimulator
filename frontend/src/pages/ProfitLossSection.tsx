import { Card, SimpleGrid, Table, Text, Title } from '@mantine/core'
import type { CryptoProfitLoss, PortfolioProfitLoss } from '../api/trades'
import { formatAmount, formatMoney } from './holdings'
import { cardStyle, signedMoney } from './marketFormat'
import { StatCard } from './StatCard'

const signOf = (value: number) => (value < 0 ? 'negative' : 'positive')

function Signed({ value, suffix = '', fz, fw }: { value: number; suffix?: string; fz?: string; fw?: number }) {
  const sign = signOf(value)
  return (
    <Text span fz={fz} fw={fw} c={sign === 'negative' ? 'red' : 'teal'} data-sign={sign}>
      {`${signedMoney(value, '')}${suffix}`}
    </Text>
  )
}

export function ProfitLossSection({ summary, rows }: { summary: PortfolioProfitLoss; rows: CryptoProfitLoss[] }) {
  return (
    <>
      <SimpleGrid cols={{ base: 1, sm: 3 }}>
        <StatCard label="Cost basis">{formatMoney(summary.totalCostBasis)}</StatCard>
        <StatCard label="Profit / loss">
          <Signed value={summary.totalProfitLossAmount} fz="1.75rem" fw={700} />
        </StatCard>
        <StatCard label="Profit / loss %">
          <Signed value={summary.totalProfitLossPercentage} suffix="%" fz="1.75rem" fw={700} />
        </StatCard>
      </SimpleGrid>
      <Card withBorder style={cardStyle}>
        <Title order={3} mb="md">Profit and loss</Title>
        {rows.length === 0 ? (
          <Text>No holdings yet.</Text>
        ) : (
          <Table.ScrollContainer minWidth={560}>
            <Table>
              <Table.Thead>
                <Table.Tr>
                  <Table.Th>Coin</Table.Th>
                  <Table.Th ta="right">Amount</Table.Th>
                  <Table.Th ta="right">Avg buy price</Table.Th>
                  <Table.Th ta="right">Current rate</Table.Th>
                  <Table.Th ta="right">P&amp;L</Table.Th>
                  <Table.Th ta="right">P&amp;L %</Table.Th>
                </Table.Tr>
              </Table.Thead>
              <Table.Tbody>
                {rows.map((row) => (
                  <Table.Tr key={row.cryptoId}>
                    <Table.Td>{row.cryptoName}</Table.Td>
                    <Table.Td ta="right">{formatAmount(row.amountOwned)}</Table.Td>
                    <Table.Td ta="right">{formatMoney(row.avgPurchasePrice)}</Table.Td>
                    <Table.Td ta="right">{formatMoney(row.currentExchangeRate)}</Table.Td>
                    <Table.Td ta="right"><Signed value={row.profitLossAmount} /></Table.Td>
                    <Table.Td ta="right"><Signed value={row.profitLossPercentage} suffix="%" /></Table.Td>
                  </Table.Tr>
                ))}
              </Table.Tbody>
            </Table>
          </Table.ScrollContainer>
        )}
      </Card>
    </>
  )
}
