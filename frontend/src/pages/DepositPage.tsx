import { Alert, Card, Group, Loader, SimpleGrid, Stack, Text, Title } from '@mantine/core'
import { useQuery } from '@tanstack/react-query'
import { fetchCards, fetchDeposits } from '../api/deposits'
import { fetchPortfolio } from '../api/trades'
import { useAuth } from '../auth/useAuth'
import { DepositForm } from './DepositForm'
import { DepositHistory } from './DepositHistory'
import { errorMessage } from './errorMessage'
import { formatMoney } from './holdings'

export function DepositPage() {
  const { userId } = useAuth()
  const enabled = userId !== null
  const id = userId ?? 0
  const portfolioQuery = useQuery({ queryKey: ['portfolio', id], queryFn: () => fetchPortfolio(id), enabled })
  const cardsQuery = useQuery({ queryKey: ['cards', id], queryFn: () => fetchCards(id), enabled })
  const depositsQuery = useQuery({ queryKey: ['deposits', id], queryFn: () => fetchDeposits(id), enabled })

  if (!enabled) return <Alert color="red">Could not determine the signed-in user.</Alert>

  const loadError = portfolioQuery.error ?? cardsQuery.error ?? depositsQuery.error

  return (
    <Stack>
      <Title order={2}>Deposit</Title>
      {loadError && (
        <Alert color="red" title="Could not load deposit data">
          {errorMessage(loadError)}
        </Alert>
      )}
      <SimpleGrid cols={{ base: 1, md: 2 }} spacing="md">
        <Card withBorder>
          <Stack>
            <Group justify="space-between">
              <Text c="dimmed">Wallet balance</Text>
              {portfolioQuery.data ? (
                <Text fw={700} size="lg">{`$${formatMoney(portfolioQuery.data.fiatBalance)}`}</Text>
              ) : (
                <Loader size="xs" aria-label="Loading balance" />
              )}
            </Group>
            {cardsQuery.data ? <DepositForm userId={id} cards={cardsQuery.data} /> : <Loader size="sm" aria-label="Loading cards" />}
          </Stack>
        </Card>
        <DepositHistory deposits={depositsQuery.data ?? []} />
      </SimpleGrid>
    </Stack>
  )
}
