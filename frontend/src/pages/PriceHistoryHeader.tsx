import { Badge, Group, Paper, Stack, Text, Title } from '@mantine/core'
import { CoinBadge } from './CoinBadge'
import type { SessionStats } from './priceStats'

interface PriceHistoryHeaderProps {
  name: string | undefined
  currentRate: number | undefined
  stats: SessionStats | null
}

const formatValue = (value: number) => value.toLocaleString(undefined, { maximumFractionDigits: 8 })

function StatValue({ label, value }: { label: string; value: number }) {
  return (
    <div>
      <Text size="xs" c="dimmed">{label}</Text>
      <Text fw={600}>{formatValue(value)}</Text>
    </div>
  )
}

export function PriceHistoryHeader({ name, currentRate, stats }: PriceHistoryHeaderProps) {
  return (
    <Paper withBorder p="md" style={{ backgroundColor: 'var(--grid-body-bg)' }}>
      <Group justify="space-between" align="center" wrap="wrap">
        <Group>
          {name && <CoinBadge name={name} size={48} />}
          <Stack gap={2}>
            <Title order={2} fz="2rem">
              {name ? `${name} price history` : 'Price history'}
            </Title>
            {currentRate !== undefined && <Text>{`Current rate: ${formatValue(currentRate)}`}</Text>}
          </Stack>
        </Group>
        {stats && (
          <Group gap="xl">
            <Badge size="lg" color={stats.changePercent >= 0 ? 'green' : 'red'}>
              {`${stats.changePercent >= 0 ? '+' : ''}${stats.changePercent.toFixed(2)}% (Session)`}
            </Badge>
            <StatValue label="Session high" value={stats.high} />
            <StatValue label="Session low" value={stats.low} />
          </Group>
        )}
      </Group>
    </Paper>
  )
}
