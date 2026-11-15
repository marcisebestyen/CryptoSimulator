import { Alert, Badge, Divider, Group, Loader, Modal, Stack, Text } from '@mantine/core'
import { useQuery } from '@tanstack/react-query'
import type { Crypto } from '../api/cryptos'
import { fetchTransaction } from '../api/transactions'
import { errorMessage } from './errorMessage'
import { formatAmount, formatMoney } from './holdings'
import { toRows } from './transactionRows'

function Line({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <Group justify="space-between">
      <Text c="dimmed">{label}</Text>
      <Text fw={500} component="div">{children}</Text>
    </Group>
  )
}

export function TypeBadge({ isPurchase }: { isPurchase: boolean }) {
  return (
    <Badge color={isPurchase ? 'teal' : 'red'} variant="light">
      {isPurchase ? 'BUY' : 'SELL'}
    </Badge>
  )
}

interface Props {
  transactionId: number
  cryptos: Crypto[]
  onClose: () => void
}

export function TransactionDetailsModal({ transactionId, cryptos, onClose }: Props) {
  const query = useQuery({ queryKey: ['transaction', transactionId], queryFn: () => fetchTransaction(transactionId) })
  const row = query.data ? toRows([query.data], cryptos)[0] : null

  return (
    <Modal opened onClose={onClose} title={`Transaction #${transactionId}`}>
      {query.isLoading && <Loader size="sm" aria-label="Loading transaction" />}
      {query.error && <Alert color="red">{errorMessage(query.error)}</Alert>}
      {row && (
        <Stack>
          <Line label="Type"><TypeBadge isPurchase={row.isPurchase} /></Line>
          <Line label="Date">{new Date(row.date).toLocaleString()}</Line>
          <Line label="Asset">{row.cryptoName}</Line>
          <Line label="Wallet">{row.walletId}</Line>
          <Divider variant="dashed" />
          <Line label="Amount">{formatAmount(row.amount)}</Line>
          <Line label="Execution price">{`$${formatMoney(row.exchangeRate)}`}</Line>
          <Divider variant="dashed" />
          <Line label="Total value">{`$${formatMoney(row.total)}`}</Line>
        </Stack>
      )}
    </Modal>
  )
}
