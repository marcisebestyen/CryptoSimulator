import { Alert, Anchor, SegmentedControl, Stack, Text, Title } from '@mantine/core'
import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { MantineReactTable, useMantineReactTable, type MRT_ColumnDef, type MRT_PaginationState } from 'mantine-react-table'
import { useMemo, useState } from 'react'
import { Link, useParams } from 'react-router'
import { ApiError } from '../api/client'
import { PRICE_REFRESH_MS } from '../api/polling'
import { fetchCrypto, fetchPriceHistory, type PriceHistoryItem } from '../api/cryptos'

import { PriceHistoryChart } from './PriceHistoryChart'
import { PriceHistoryHeader } from './PriceHistoryHeader'
import { marketTableProps } from './marketTableProps'
import { lastN, sessionStats, stepDeltaPercent } from './priceStats'

const CHART_POINTS = 100
const OPEN_ENDED_YEAR = 9999

const formatValue = (value: number) => value.toLocaleString(undefined, { maximumFractionDigits: 8 })
const formatDate = (iso: string) => new Date(iso).toLocaleString()
const isOpenEnded = (iso: string) => new Date(iso).getUTCFullYear() === OPEN_ENDED_YEAR
const formatTo = (iso: string) => (isOpenEnded(iso) ? 'Current' : formatDate(iso))

type ChartRange = 'all' | '20' | '10'

const RANGE_OPTIONS = [
  { value: 'all', label: 'All' },
  { value: '20', label: 'Last 20' },
  { value: '10', label: 'Last 10' },
]

function StepDelta({ percent }: { percent: number | null }) {
  if (percent === null) return null
  if (percent === 0) return <Text c="dimmed" span>0.00%</Text>
  return (
    <Text c={percent > 0 ? 'green' : 'red'} span>
      {`${percent > 0 ? '+' : ''}${percent.toFixed(2)}%`}
    </Text>
  )
}

export function PriceHistoryPage() {
  const cryptoId = Number(useParams().cryptoId)
  const [pagination, setPagination] = useState<MRT_PaginationState>({ pageIndex: 0, pageSize: 10 })

  const crypto = useQuery({ queryKey: ['crypto', cryptoId], queryFn: () => fetchCrypto(cryptoId) })
  const history = useQuery({
    queryKey: ['price-history', cryptoId, pagination.pageIndex, pagination.pageSize],
    queryFn: () => fetchPriceHistory(cryptoId, pagination.pageIndex + 1, pagination.pageSize),
    placeholderData: keepPreviousData,
    refetchInterval: PRICE_REFRESH_MS,
  })
  const chartHistory = useQuery({
    queryKey: ['price-history-chart', cryptoId],
    queryFn: () => fetchPriceHistory(cryptoId, 1, CHART_POINTS),
    refetchInterval: PRICE_REFRESH_MS,
  })
  // The API returns newest first; the chart needs oldest first.
  const chartItems = useMemo(() => [...(chartHistory.data?.items ?? [])].reverse(), [chartHistory.data])
  const [range, setRange] = useState<ChartRange>('all')
  const stats = useMemo(() => sessionStats(chartItems.map((item) => item.value)), [chartItems])
  const visibleItems = lastN(chartItems, range === 'all' ? 'all' : Number(range))

  // History is newest first, so the open-ended record is on page 1; keep the last seen rate on later pages.
  const [currentRate, setCurrentRate] = useState<number>()
  const loadedRate = history.data?.items.find((item) => isOpenEnded(item.to))?.value
  if (loadedRate !== undefined && loadedRate !== currentRate) setCurrentRate(loadedRate)

  const columns = useMemo<MRT_ColumnDef<PriceHistoryItem>[]>(
    () => [
      {
        accessorKey: 'value',
        header: 'Value',
        mantineTableHeadCellProps: { align: 'right' },
        mantineTableBodyCellProps: { align: 'right' },
        Cell: ({ cell }) => `$${formatValue(cell.getValue<number>())}`,
      },
      { accessorKey: 'from', header: 'From', Cell: ({ cell }) => formatDate(cell.getValue<string>()) },
      { accessorKey: 'to', header: 'To', Cell: ({ cell }) => formatTo(cell.getValue<string>()) },
      {
        id: 'stepDelta',
        header: 'Step delta',
        enableSorting: false,
        // Rows are newest first, so the next row is the older value.
        Cell: ({ row, table }) => (
          <StepDelta
            percent={stepDeltaPercent(
              row.original.value,
              table.options.data[row.index + 1]?.value,
            )}
          />
        ),
      },
    ],
    [],
  )

  const table = useMantineReactTable({
    columns,
    data: history.data?.items ?? [],
    manualPagination: true,
    enablePagination: true,
    enableSorting: false,
    rowCount: history.data?.totalCount ?? 0,
    onPaginationChange: setPagination,
    state: { isLoading: history.isLoading, pagination },
    mantinePaginationProps: { rowsPerPageOptions: ['10', '25', '50', '100'] },
    ...marketTableProps,
    renderTopToolbarCustomActions: () => <Title order={3}>Price History</Title>,
    renderEmptyRowsFallback: () => <Text ta="center" p="md">No price history yet.</Text>,
  })

  const failure = [crypto.error, history.error].find((error) => error !== null)
  const notFound = failure instanceof ApiError && failure.status === 404

  return (
    <Stack>
      <Anchor component={Link} to="/cryptos">
        Back to cryptos
      </Anchor>
      <PriceHistoryHeader name={crypto.data?.name} currentRate={currentRate} stats={stats} />
      {notFound && <Alert color="red">Crypto not found.</Alert>}
      {failure && !notFound && (
        <Alert color="red" title="Could not load price history">
          {failure instanceof ApiError ? failure.message : 'Cannot reach the server.'}
        </Alert>
      )}
      {!notFound && chartItems.length > 0 && (
        <SegmentedControl
          aria-label="Chart range"
          value={range}
          onChange={(value) => setRange(value as ChartRange)}
          data={RANGE_OPTIONS}
        />
      )}
      {!notFound && <PriceHistoryChart items={visibleItems} />}
      {!notFound && <MantineReactTable table={table} />}
    </Stack>
  )
}
