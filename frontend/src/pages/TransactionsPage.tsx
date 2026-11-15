import { Alert, Anchor, Button, Card, Group, Loader, NativeSelect, SegmentedControl, SimpleGrid, Stack, Text, TextInput, Title } from '@mantine/core'
import { useQuery } from '@tanstack/react-query'
import { useMemo, useState } from 'react'
import { Link } from 'react-router'
import { fetchCryptos } from '../api/cryptos'
import { fetchTransactions } from '../api/transactions'
import { useAuth } from '../auth/useAuth'
import { errorMessage } from './errorMessage'
import { formatMoney } from './holdings'
import { cardStyle } from './marketFormat'
import { StatCard } from './StatCard'
import { TransactionDetailsModal } from './TransactionDetailsModal'
import { toRows } from './transactionRows'
import { TransactionsTable } from './TransactionsTable'

type TypeFilter = 'all' | 'buy' | 'sell'
type SortMode = 'newest' | 'oldest' | 'value'

const SORT_OPTIONS: { value: SortMode; label: string }[] = [
  { value: 'newest', label: 'Newest' },
  { value: 'oldest', label: 'Oldest' },
  { value: 'value', label: 'Highest value' },
]

export function TransactionsPage() {
  const { userId } = useAuth()
  const [typeFilter, setTypeFilter] = useState<TypeFilter>('all')
  const [search, setSearch] = useState('')
  const [sort, setSort] = useState<SortMode>('newest')
  const [selectedId, setSelectedId] = useState<number | null>(null)
  const enabled = userId !== null
  const id = userId ?? 0
  const newestFirst = sort !== 'oldest'

  const transactionsQuery = useQuery({
    queryKey: ['transactions', id, newestFirst],
    queryFn: () => fetchTransactions(id, newestFirst),
    enabled,
  })
  const cryptosQuery = useQuery({ queryKey: ['cryptos'], queryFn: fetchCryptos })
  const transactions = transactionsQuery.data
  const cryptos = cryptosQuery.data

  const allRows = useMemo(() => (transactions && cryptos ? toRows(transactions, cryptos) : []), [transactions, cryptos])
  const visibleRows = useMemo(() => {
    const term = search.trim().toLowerCase()
    const filtered = allRows.filter(
      (row) =>
        (typeFilter === 'all' || row.isPurchase === (typeFilter === 'buy')) &&
        row.cryptoName.toLowerCase().includes(term),
    )
    return sort === 'value' ? [...filtered].sort((a, b) => b.total - a.total) : filtered
  }, [allRows, typeFilter, search, sort])

  if (!enabled) return <Alert color="red">Could not determine the signed-in user.</Alert>

  const loadError = transactionsQuery.error ?? cryptosQuery.error
  const buyCount = allRows.filter((row) => row.isPurchase).length
  const volume = allRows.reduce((sum, row) => sum + row.total, 0)

  return (
    <Stack>
      <Card withBorder style={cardStyle}>
        <Group justify="space-between" align="center">
          <Stack gap={4}>
            <Anchor component={Link} to="/market">Back to Market</Anchor>
            <Title order={2}>Transactions</Title>
            <Text c="dimmed">Your executed buy and sell orders.</Text>
          </Stack>
          <Group>
            <Button component={Link} to="/market" color="teal">New Buy Order</Button>
            <Button component={Link} to="/market" color="red" variant="light">New Sell Order</Button>
          </Group>
        </Group>
      </Card>

      {(transactionsQuery.isLoading || cryptosQuery.isLoading) && <Loader size="sm" aria-label="Loading transactions" />}
      {loadError && (
        <Alert color="red" title="Could not load transactions">
          {errorMessage(loadError)}
        </Alert>
      )}

      {transactions && cryptos && (
        <>
          <SimpleGrid cols={{ base: 1, sm: 3 }}>
            <StatCard label="Total volume">{`$${formatMoney(volume)}`}</StatCard>
            <StatCard label="Buy orders"><span data-testid="buy-count">{buyCount}</span></StatCard>
            <StatCard label="Sell orders"><span data-testid="sell-count">{allRows.length - buyCount}</span></StatCard>
          </SimpleGrid>

          <Group align="end">
            <SegmentedControl
              value={typeFilter}
              onChange={(value) => setTypeFilter(value as TypeFilter)}
              data={[
                { value: 'all', label: 'All' },
                { value: 'buy', label: 'Buy' },
                { value: 'sell', label: 'Sell' },
              ]}
            />
            <TextInput label="Search coin" placeholder="Bitcoin" value={search} onChange={(event) => setSearch(event.currentTarget.value)} />
            <NativeSelect label="Sort by" value={sort} onChange={(event) => setSort(event.currentTarget.value as SortMode)} data={SORT_OPTIONS} />
          </Group>

          <TransactionsTable rows={visibleRows} onSelect={setSelectedId} />
        </>
      )}

      {selectedId !== null && cryptos && (
        <TransactionDetailsModal transactionId={selectedId} cryptos={cryptos} onClose={() => setSelectedId(null)} />
      )}
    </Stack>
  )
}
