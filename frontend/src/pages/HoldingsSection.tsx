import { DonutChart } from '@mantine/charts'
import { Badge, Box, Card, Group, Progress, Stack, Text, Title } from '@mantine/core'
import type { PortfolioItem } from '../api/trades'
import { formatAmount, formatMoney, sharePercent, sortByValue, toSlices, totalValue } from './holdings'
import { cardStyle } from './marketFormat'

const CHART_SIZE = 220

export function HoldingsSection({ holdings }: { holdings: PortfolioItem[] }) {
  const sorted = sortByValue(holdings)
  const slices = toSlices(sorted)
  const total = totalValue(holdings)
  const coins = `${holdings.length} ${holdings.length === 1 ? 'coin' : 'coins'}`

  return (
    <Card withBorder style={cardStyle}>
      <Title order={3} mb="md">Allocation</Title>
      {holdings.length === 0 ? (
        <Text>You do not own any crypto yet.</Text>
      ) : (
        <Group align="center" gap="xl" wrap="wrap">
          <Box aria-label="Holdings chart" role="img" pos="relative" w={CHART_SIZE} h={CHART_SIZE}>
            <DonutChart data={slices} size={CHART_SIZE} withTooltip tooltipDataSource="segment" valueFormatter={formatMoney} thickness={22} />
            <Stack gap={0} align="center" justify="center" pos="absolute" inset={0} style={{ pointerEvents: 'none' }}>
              <Text fw={700}>{coins}</Text>
              <Text size="sm" c="dimmed">{formatMoney(total)}</Text>
            </Stack>
          </Box>
          <Stack style={{ flex: 1, minWidth: 260 }}>
            {sorted.map((holding, index) => {
              const share = sharePercent(holding.currentValue, total)
              const color = index < slices.length ? slices[index].color : 'gray.6'
              return (
                <Stack key={holding.cryptoId} gap={4}>
                  <Group justify="space-between" wrap="nowrap">
                    <Group gap="xs" wrap="nowrap">
                      <Badge circle size="xs" color={color} />
                      <Text fw={600}>{holding.cryptoName}</Text>
                    </Group>
                    <Text size="sm" c="dimmed">{formatAmount(holding.amountOwned)}</Text>
                    <Text fw={600}>{formatMoney(holding.currentValue)}</Text>
                  </Group>
                  <Group gap="sm" wrap="nowrap">
                    <Progress value={share} color={color} size="sm" style={{ flex: 1 }} />
                    <Text size="xs" w={48} ta="right">{`${share.toFixed(1)}%`}</Text>
                  </Group>
                </Stack>
              )
            })}
          </Stack>
        </Group>
      )}
    </Card>
  )
}
