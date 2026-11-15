import { Card, Group, Stack, Text, Title } from '@mantine/core'
import type { DepositHistoryItem } from '../api/deposits'
import { formatMoney } from './holdings'

interface Props {
  deposits: DepositHistoryItem[]
}

export function DepositHistory({ deposits }: Props) {
  const newestFirst = [...deposits].sort((a, b) => Date.parse(b.date) - Date.parse(a.date))
  return (
    <Card withBorder>
      <Stack>
        <Title order={4}>Recent Wallet Deposits</Title>
        {newestFirst.length === 0 && <Text c="dimmed">No deposits yet.</Text>}
        {newestFirst.map((deposit) => (
          <Group key={deposit.id} justify="space-between">
            <Text size="sm">{`${new Date(deposit.date).toLocaleDateString()} (•••• ${deposit.last4})`}</Text>
            <Text size="sm" fw={600} c="teal">{`+$${formatMoney(deposit.amount)}`}</Text>
          </Group>
        ))}
      </Stack>
    </Card>
  )
}
