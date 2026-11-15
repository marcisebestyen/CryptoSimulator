import { Button, Card, Group, Table, Text } from '@mantine/core'
import { CoinBadge } from './CoinBadge'
import { formatAmount, formatMoney } from './holdings'
import { cardStyle } from './marketFormat'
import { TypeBadge } from './TransactionDetailsModal'
import type { TransactionRow } from './transactionRows'

interface Props {
  rows: TransactionRow[]
  onSelect: (id: number) => void
}

export function TransactionsTable({ rows, onSelect }: Props) {
  if (rows.length === 0) {
    return (
      <Card withBorder style={cardStyle}>
        <Text>No transactions found.</Text>
      </Card>
    )
  }
  return (
    <Card withBorder style={cardStyle}>
      <Table.ScrollContainer minWidth={720}>
        <Table highlightOnHover>
          <Table.Thead>
            <Table.Tr>
              <Table.Th>Date &amp; time</Table.Th>
              <Table.Th>Type</Table.Th>
              <Table.Th>Asset</Table.Th>
              <Table.Th ta="right">Amount</Table.Th>
              <Table.Th ta="right">Execution price</Table.Th>
              <Table.Th ta="right">Total value</Table.Th>
              <Table.Th />
            </Table.Tr>
          </Table.Thead>
          <Table.Tbody>
            {rows.map((row) => (
              <Table.Tr key={row.id} style={{ cursor: 'pointer' }} onClick={() => onSelect(row.id)}>
                <Table.Td>
                  <Text span>{new Date(row.date).toLocaleString()}</Text>
                  <Text span c="dimmed" ml="xs">{`#${row.id}`}</Text>
                </Table.Td>
                <Table.Td><TypeBadge isPurchase={row.isPurchase} /></Table.Td>
                <Table.Td>
                  <Group gap="sm" wrap="nowrap">
                    <CoinBadge name={row.cryptoName} size={28} />
                    <Text fw={500}>{row.cryptoName}</Text>
                  </Group>
                </Table.Td>
                <Table.Td ta="right">{formatAmount(row.amount)}</Table.Td>
                <Table.Td ta="right">{`$${formatMoney(row.exchangeRate)}`}</Table.Td>
                <Table.Td ta="right">{`$${formatMoney(row.total)}`}</Table.Td>
                <Table.Td ta="right">
                  <Button size="compact-sm" variant="light" aria-label={`Details #${row.id}`}>Details</Button>
                </Table.Td>
              </Table.Tr>
            ))}
          </Table.Tbody>
        </Table>
      </Table.ScrollContainer>
    </Card>
  )
}
