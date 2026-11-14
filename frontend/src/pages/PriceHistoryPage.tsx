import { Alert, Anchor, Stack, Text, Title } from '@mantine/core'
import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { MantineReactTable, useMantineReactTable, type MRT_ColumnDef, type MRT_PaginationState } from 'mantine-react-table'
import { useMemo, useState } from 'react'
import { Link, useParams } from 'react-router'
import { ApiError } from '../api/client'
import { fetchCrypto, fetchPriceHistory, type PriceHistoryItem } from '../api/cryptos'

const OPEN_ENDED_YEAR = 9999

const formatValue = (value: number) => value.toLocaleString(undefined, { maximumFractionDigits: 8 })
const formatDate = (iso: string) => new Date(iso).toLocaleString()
const formatTo = (iso: string) => (new Date(iso).getUTCFullYear() === OPEN_ENDED_YEAR ? 'Current' : formatDate(iso))

export function PriceHistoryPage() {
  const cryptoId = Number(useParams().cryptoId)
  const [pagination, setPagination] = useState<MRT_PaginationState>({ pageIndex: 0, pageSize: 25 })

  const crypto = useQuery({ queryKey: ['crypto', cryptoId], queryFn: () => fetchCrypto(cryptoId) })
  const history = useQuery({
    queryKey: ['price-history', cryptoId, pagination.pageIndex, pagination.pageSize],
    queryFn: () => fetchPriceHistory(cryptoId, pagination.pageIndex + 1, pagination.pageSize),
    placeholderData: keepPreviousData,
  })

  const columns = useMemo<MRT_ColumnDef<PriceHistoryItem>[]>(
    () => [
      { accessorKey: 'value', header: 'Value', Cell: ({ cell }) => formatValue(cell.getValue<number>()) },
      { accessorKey: 'from', header: 'From', Cell: ({ cell }) => formatDate(cell.getValue<string>()) },
      { accessorKey: 'to', header: 'To', Cell: ({ cell }) => formatTo(cell.getValue<string>()) },
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
    mantinePaginationProps: { rowsPerPageOptions: ['25', '50', '100'] },
    mantinePaperProps: { withBorder: true, style: { backgroundColor: 'var(--grid-body-bg)' } },
    mantineTableProps: { withTableBorder: true, withColumnBorders: true, withRowBorders: true, striped: false },
    mantineTableHeadCellProps: { className: 'grid-head-cell' },
    renderEmptyRowsFallback: () => <Text ta="center" p="md">No price history yet.</Text>,
  })

  const failure = [crypto.error, history.error].find((error) => error !== null)
  const notFound = failure instanceof ApiError && failure.status === 404

  return (
    <Stack>
      <Anchor component={Link} to="/cryptos">
        Back to cryptos
      </Anchor>
      <Title order={2} fz="2rem">
        {crypto.data ? `${crypto.data.name} price history` : 'Price history'}
      </Title>
      {notFound && <Alert color="red">Crypto not found.</Alert>}
      {failure && !notFound && (
        <Alert color="red" title="Could not load price history">
          {failure instanceof ApiError ? failure.message : 'Cannot reach the server.'}
        </Alert>
      )}
      {!notFound && <MantineReactTable table={table} />}
    </Stack>
  )
}
