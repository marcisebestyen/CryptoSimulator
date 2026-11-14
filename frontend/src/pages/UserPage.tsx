import { Alert, Card, Group, Loader, SimpleGrid, Stack, Text } from '@mantine/core'
import { useQuery } from '@tanstack/react-query'
import { useState } from 'react'
import { fetchUser, fetchWallet } from '../api/users'
import { useAuth } from '../auth/useAuth'
import { EditProfileModal } from './EditProfileModal'
import { errorMessage } from './errorMessage'
import { HoldingsSection } from './HoldingsSection'
import { formatMoney, totalValue } from './holdings'
import { ProfileHeader } from './ProfileHeader'

function Stat({ label, value }: { label: string; value: number }) {
  return (
    <Card withBorder style={{ backgroundColor: 'var(--surface-raised)', borderColor: 'var(--grid-line-color)' }}>
      <Text size="sm" c="dimmed">{label}</Text>
      <Text fz="1.75rem" fw={700}>{formatMoney(value)}</Text>
    </Card>
  )
}

export function UserPage() {
  const { userId } = useAuth()
  const [editing, setEditing] = useState(false)
  const [saved, setSaved] = useState(false)
  const enabled = userId !== null
  const id = userId ?? 0
  const userQuery = useQuery({ queryKey: ['user', id], queryFn: () => fetchUser(id), enabled })
  const walletQuery = useQuery({ queryKey: ['wallet', id], queryFn: () => fetchWallet(id), enabled })

  if (!enabled) return <Alert color="red">Could not determine the signed-in user.</Alert>

  const wallet = walletQuery.data
  const holdingsTotal = wallet ? totalValue(wallet.ownedCryptos) : 0

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

      {walletQuery.isLoading && (
        <Group>
          <Loader size="sm" aria-label="Loading wallet" />
        </Group>
      )}
      {walletQuery.error && (
        <Alert color="red" title="Could not load wallet">
          {errorMessage(walletQuery.error)}
        </Alert>
      )}
      {wallet && (
        <>
          <SimpleGrid cols={{ base: 1, sm: 3 }}>
            <Stat label="Fiat balance" value={wallet.fiatBalance} />
            <Stat label="Holdings value" value={holdingsTotal} />
            <Stat label="Total worth" value={wallet.fiatBalance + holdingsTotal} />
          </SimpleGrid>
          <HoldingsSection holdings={wallet.ownedCryptos} />
        </>
      )}
    </Stack>
  )
}
