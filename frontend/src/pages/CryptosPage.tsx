import { Alert, Button, Stack, Text, Title } from '@mantine/core'
import { useQuery } from '@tanstack/react-query'
import { MantineReactTable, useMantineReactTable, type MRT_ColumnDef } from 'mantine-react-table'
import { useMemo } from 'react'
import { ApiError } from '../api/client'
import { fetchCryptos, type Crypto } from '../api/cryptos'

const formatRate = (rate: number) => rate.toLocaleString(undefined, { maximumFractionDigits: 8 })

export function CryptosPage() {
  const { data, isLoading, error, refetch } = useQuery({ queryKey: ['cryptos'], queryFn: fetchCryptos })

  const columns = useMemo<MRT_ColumnDef<Crypto>[]>(
    () => [
      { accessorKey: 'id', header: 'Id', size: 80 },
      { accessorKey: 'name', header: 'Name' },
      {
        accessorKey: 'currentExchangeRate',
        header: 'Exchange rate',
        Cell: ({ cell }) => formatRate(cell.getValue<number>()),
      },
    ],
    [],
  )

  const table = useMantineReactTable({
    columns,
    data: data ?? [],
    enablePagination: true,
    enableRowSelection: true,
    state: { isLoading },
    mantinePaperProps: { withBorder: true, style: { backgroundColor: 'var(--grid-body-bg)' } },
    mantineTableProps: {
      withTableBorder: true,
      withColumnBorders: true,
      withRowBorders: true,
      striped: false,
      highlightOnHover: true,
    },
    mantineTableBodyRowProps: ({ row }) => ({
      onClick: row.getToggleSelectedHandler(),
      className: row.getIsSelected() ? 'row-selected' : undefined,
      style: { cursor: 'pointer' },
    }),
    mantineTopToolbarProps: { className: 'grid-toolbar' },
    mantineTableHeadCellProps: { className: 'grid-head-cell' },
    renderEmptyRowsFallback: () => <Text ta="center" p="md">No cryptos yet.</Text>,
  })

  return (
    <Stack>
      <Title order={2} fz="2rem">Cryptos</Title>
      {error && (
        <Alert color="red" title="Could not load cryptos">
          <Stack align="flex-start" gap="xs">
            {error instanceof ApiError ? error.message : 'Cannot reach the server.'}
            <Button size="xs" onClick={() => void refetch()}>
              Retry
            </Button>
          </Stack>
        </Alert>
      )}
      <MantineReactTable table={table} />
    </Stack>
  )
}
