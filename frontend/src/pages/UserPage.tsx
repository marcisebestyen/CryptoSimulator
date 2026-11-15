import { Alert, Group, Loader, SimpleGrid, Stack } from '@mantine/core'
import { useQuery } from '@tanstack/react-query'
import { useState } from 'react'
import { fetchPortfolio, fetchProfitLoss, fetchProfitLossSummary } from '../api/trades'
import { fetchUser } from '../api/users'
import { useAuth } from '../auth/useAuth'
import { EditProfileModal } from './EditProfileModal'
import { errorMessage } from './errorMessage'
import { HoldingsSection } from './HoldingsSection'
import { formatMoney } from './holdings'
import { ProfileHeader } from './ProfileHeader'
import { ProfitLossSection } from './ProfitLossSection'
import { StatCard } from './StatCard'

export function UserPage() {
  const { userId } = useAuth()
  const [editing, setEditing] = useState(false)
  const [saved, setSaved] = useState(false)
  const enabled = userId !== null
  const id = userId ?? 0
  const userQuery = useQuery({ queryKey: ['user', id], queryFn: () => fetchUser(id), enabled })
  const portfolioQuery = useQuery({ queryKey: ['portfolio', id], queryFn: () => fetchPortfolio(id), enabled })
  const profitLossQuery = useQuery({ queryKey: ['profit-loss', id], queryFn: () => fetchProfitLoss(id), enabled })
  const summaryQuery = useQuery({
    queryKey: ['profit-loss-summary', id],
    queryFn: () => fetchProfitLossSummary(id),
    enabled,
  })

  if (!enabled) return <Alert color="red">Could not determine the signed-in user.</Alert>

  const portfolio = portfolioQuery.data
  const profitLossError = profitLossQuery.error ?? summaryQuery.error

  return (
    <Stack>
      {saved && <Alert color="green">Profile saved.</Alert>}
      {userQuery.isLoading && <Loader size="sm" aria-label="Loading profile" />}
      {userQuery.error && (
        <Alert color="red" title="Could not load profile">
          {errorMessage(userQuery.error)}
        </Alert>
      )}
      {userQuery.data && (
        <>
          <ProfileHeader
            user={userQuery.data}
            onEdit={() => {
              setSaved(false)
              setEditing(true)
            }}
          />
          {editing && (
            <EditProfileModal user={userQuery.data} onClose={() => setEditing(false)} onSaved={() => setSaved(true)} />
          )}
        </>
      )}

      {portfolioQuery.isLoading && (
        <Group>
          <Loader size="sm" aria-label="Loading portfolio" />
        </Group>
      )}
      {portfolioQuery.error && (
        <Alert color="red" title="Could not load portfolio">
          {errorMessage(portfolioQuery.error)}
        </Alert>
      )}
      {portfolio && (
        <>
          <SimpleGrid cols={{ base: 1, sm: 3 }}>
            <StatCard label="Fiat balance">{formatMoney(portfolio.fiatBalance)}</StatCard>
            <StatCard label="Holdings value">{formatMoney(portfolio.totalCryptoValue)}</StatCard>
            <StatCard label="Total worth">{formatMoney(portfolio.totalPortfolioValue)}</StatCard>
          </SimpleGrid>
          <HoldingsSection holdings={portfolio.holdings} />
        </>
      )}

      {(profitLossQuery.isLoading || summaryQuery.isLoading) && (
        <Group>
          <Loader size="sm" aria-label="Loading profit and loss" />
        </Group>
      )}
      {profitLossError && (
        <Alert color="red" title="Could not load profit and loss">
          {errorMessage(profitLossError)}
        </Alert>
      )}
      {profitLossQuery.data && summaryQuery.data && (
        <ProfitLossSection summary={summaryQuery.data} rows={profitLossQuery.data} />
      )}
    </Stack>
  )
}
