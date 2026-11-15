import { Alert, Anchor, Button, Card, Group, Loader, SimpleGrid, Stack, Text, Title } from '@mantine/core'
import { useQuery } from '@tanstack/react-query'
import { useState } from 'react'
import { Link } from 'react-router'
import { fetchCryptos } from '../api/cryptos'
import { PRICE_REFRESH_MS } from '../api/polling'
import { fetchPortfolio, fetchProfitLoss, fetchProfitLossSummary } from '../api/trades'
import { fetchTransactions } from '../api/transactions'
import { useAuth } from '../auth/useAuth'
import { errorMessage } from './errorMessage'
import { formatMoney } from './holdings'
import { cardStyle, signedMoney } from './marketFormat'
import { HoldingsTable, LiveMarketTable } from './MarketTables'
import { StatCard } from './StatCard'
import { TradeModal } from './TradeModal'
import type { TradeMode } from './tradeMath'

interface TradeTarget {
  mode: TradeMode
  cryptoId: number | null
}

export function MarketPage() {
  const { userId } = useAuth()
  const [trade, setTrade] = useState<TradeTarget | null>(null)
  const enabled = userId !== null
  const id = userId ?? 0
  const portfolioQuery = useQuery({ queryKey: ['portfolio', id], queryFn: () => fetchPortfolio(id), enabled })
  const profitLossQuery = useQuery({ queryKey: ['profit-loss', id], queryFn: () => fetchProfitLoss(id), enabled })
  const summaryQuery = useQuery({
    queryKey: ['profit-loss-summary', id],
    queryFn: () => fetchProfitLossSummary(id),
    enabled,
  })
  const transactionsQuery = useQuery({
    queryKey: ['transactions', id, true],
    queryFn: () => fetchTransactions(id, true),
    enabled,
  })
  const cryptosQuery = useQuery({ queryKey: ['cryptos'], queryFn: fetchCryptos, refetchInterval: PRICE_REFRESH_MS })

  if (!enabled) return <Alert color="red">Could not determine the signed-in user.</Alert>

  const portfolio = portfolioQuery.data
  const cryptos = cryptosQuery.data
  const summary = summaryQuery.data
  const loadError = portfolioQuery.error ?? cryptosQuery.error
  const profitLossError = profitLossQuery.error ?? summaryQuery.error
  const firstHeld = portfolio?.holdings.find((holding) => holding.amountOwned > 0)
  const canTrade = portfolio !== undefined && cryptos !== undefined

  return (
    <Stack>
      <Card withBorder style={cardStyle}>
        <Group justify="space-between" align="center">
          <Stack gap={4}>
            <Title order={2}>Market</Title>
            <Text c="dimmed">Trade crypto at live rates.</Text>
            <Anchor component={Link} to="/market/transactions">View transactions</Anchor>
          </Stack>
          <Group>
            <Button color="teal" disabled={!canTrade} onClick={() => setTrade({ mode: 'buy', cryptoId: null })}>
              Buy Crypto
            </Button>
            <Button
              color="red"
              variant="light"
              disabled={!canTrade || !firstHeld}
              onClick={() => setTrade({ mode: 'sell', cryptoId: firstHeld?.cryptoId ?? null })}
            >
              Sell Crypto
            </Button>
          </Group>
        </Group>
      </Card>

      {(portfolioQuery.isLoading || cryptosQuery.isLoading) && <Loader size="sm" aria-label="Loading market" />}
      {loadError && (
        <Alert color="red" title="Could not load market">
          {errorMessage(loadError)}
        </Alert>
      )}

      {profitLossError && (
        <Alert color="red" title="Could not load profit and loss">
          {errorMessage(profitLossError)}
        </Alert>
      )}
      {transactionsQuery.error && (
        <Alert color="red" title="Could not load transactions">
          {errorMessage(transactionsQuery.error)}
        </Alert>
      )}

      {portfolio && (
        <SimpleGrid cols={{ base: 1, sm: 2, lg: 4 }}>
          <StatCard label="Cash balance">{`$${formatMoney(portfolio.fiatBalance)}`}</StatCard>
          <StatCard label="Holdings value">{`$${formatMoney(portfolio.totalCryptoValue)}`}</StatCard>
          <StatCard
            label="Unrealized P&L"
            sub={
              summary && (
                <Text size="sm" c={summary.totalProfitLossPercentage < 0 ? 'red' : 'teal'}>
                  {`${summary.totalProfitLossPercentage > 0 ? '+' : ''}${formatMoney(summary.totalProfitLossPercentage)}%`}
                </Text>
              )
            }
          >
            {summary ? signedMoney(summary.totalProfitLossAmount) : '-'}
          </StatCard>
          <StatCard label="Executed trades">{transactionsQuery.data ? transactionsQuery.data.length : '-'}</StatCard>
        </SimpleGrid>
      )}

      {portfolio && (
        <HoldingsTable
          holdings={portfolio.holdings}
          profitLoss={profitLossQuery.data ?? []}
          onSell={(cryptoId) => setTrade({ mode: 'sell', cryptoId })}
        />
      )}
      {cryptos && portfolio && (
        <LiveMarketTable
          cryptos={cryptos}
          holdings={portfolio.holdings}
          onBuy={(cryptoId) => setTrade({ mode: 'buy', cryptoId })}
        />
      )}

      {trade && cryptos && portfolio && (
        <TradeModal
          mode={trade.mode}
          cryptoId={trade.cryptoId}
          cryptos={cryptos}
          portfolio={portfolio}
          userId={id}
          onClose={() => setTrade(null)}
        />
      )}
    </Stack>
  )
}
