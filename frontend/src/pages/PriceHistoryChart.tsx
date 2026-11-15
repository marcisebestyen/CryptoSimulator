import { LineChart } from '@mantine/charts'
import { Box } from '@mantine/core'
import type { PriceHistoryItem } from '../api/cryptos'

const CHART_HEIGHT = 300
const MAX_POINTS_WITH_DOTS = 30

const formatValue = (value: number) => value.toLocaleString(undefined, { maximumFractionDigits: 8 })
const formatTick = (iso: string) => new Date(iso).toLocaleString(undefined, { dateStyle: 'short', timeStyle: 'short' })

interface PriceHistoryChartProps {
  // Chronological order, oldest first.
  items: PriceHistoryItem[]
}

export function PriceHistoryChart({ items }: PriceHistoryChartProps) {
  if (items.length === 0) return null

  return (
    <Box aria-label="Price chart" role="img">
      <LineChart
        h={CHART_HEIGHT}
        data={items}
        dataKey="from"
        series={[{ name: 'value', label: 'Price', color: 'blue.6' }]}
        curveType="monotone"
        withDots={items.length <= MAX_POINTS_WITH_DOTS}
        xAxisProps={{ tickFormatter: formatTick }}
        valueFormatter={formatValue}
        tooltipProps={{ labelFormatter: formatTick }}
      />
    </Box>
  )
}
