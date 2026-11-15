import { ActionIcon, Alert, Button, Flex, Group, Stack, Text, Title, Tooltip } from '@mantine/core'
import { IconArrowRight, IconEdit, IconTrash } from '@tabler/icons-react'
import { useQuery } from '@tanstack/react-query'
import { MantineReactTable, useMantineReactTable, type MRT_ColumnDef } from 'mantine-react-table'
import { useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router'
import { useAuth } from '../auth/useAuth'
import { ApiError } from '../api/client'
import { fetchCryptos, type Crypto } from '../api/cryptos'
import { PRICE_REFRESH_MS } from '../api/polling'
import { Coin } from './Coin'
import { CryptoFormModal } from './CryptoFormModal'
import { DeleteCryptoModal } from './DeleteCryptoModal'
import { marketTableProps } from './marketTableProps'
import { usePersistedTableState } from './usePersistedTableState'

type Dialog = { type: 'add' } | { type: 'edit'; crypto: Crypto } | { type: 'delete'; crypto: Crypto } | null

const formatRate = (rate: number) => rate.toLocaleString(undefined, { maximumFractionDigits: 8 })

export function CryptosPage() {
  const { isAdmin } = useAuth()
  const [dialog, setDialog] = useState<Dialog>(null)
  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ['cryptos'],
    queryFn: fetchCryptos,
    refetchInterval: PRICE_REFRESH_MS,
  })

  const { pagination, globalFilter, onPaginationChange, onGlobalFilterChange } =
    usePersistedTableState('cryptos-table')

  const columns = useMemo<MRT_ColumnDef<Crypto>[]>(
    () => [
      {
        accessorKey: 'name',
        header: 'Name',
        Cell: ({ row }) => <Coin name={row.original.name} />,
      },
      {
        accessorKey: 'currentExchangeRate',
        header: 'Exchange rate',
        mantineTableHeadCellProps: { align: 'right' },
        mantineTableBodyCellProps: { align: 'right' },
        Cell: ({ cell }) => `$${formatRate(cell.getValue<number>())}`,
      },
      {
        id: 'history',
        header: 'History',
        enableSorting: false,
        Cell: ({ row }) => (
          <Button
            component={Link}
            to={`/cryptos/${row.original.id}/price-history`}
            variant="light"
            size="xs"
            rightSection={<IconArrowRight size={14} />}
          >
            Price history
          </Button>
        ),
      },
    ],
    [],
  )

  const table = useMantineReactTable({
    columns,
    data: data ?? [],
    enablePagination: true,
    enableGlobalFilter: true,
    autoResetPageIndex: false,
    positionGlobalFilter: 'right',
    onPaginationChange,
    onGlobalFilterChange,
    initialState: { showGlobalFilter: true },
    mantineSearchTextInputProps: { placeholder: 'Search assets...', 'aria-label': 'Search' },
    state: { isLoading, pagination, globalFilter },
    enableRowActions: isAdmin,
    positionActionsColumn: 'first',
    displayColumnDefOptions: { 'mrt-row-actions': { header: 'Actions' } },
    renderRowActions: ({ row }) => (
      <Flex gap="xs">
        <Tooltip label="Edit">
          <ActionIcon
            variant="subtle"
            aria-label={`Edit ${row.original.name}`}
            onClick={() => setDialog({ type: 'edit', crypto: row.original })}
          >
            <IconEdit size={18} />
          </ActionIcon>
        </Tooltip>
        <Tooltip label="Delete">
          <ActionIcon
            variant="subtle"
            color="red"
            aria-label={`Delete ${row.original.name}`}
            onClick={() => setDialog({ type: 'delete', crypto: row.original })}
          >
            <IconTrash size={18} />
          </ActionIcon>
        </Tooltip>
      </Flex>
    ),
    getRowId: (crypto) => String(crypto.id),
    ...marketTableProps,
    renderTopToolbarCustomActions: () => (
      <Group>
        <Title order={3}>All Cryptos</Title>
        {isAdmin && (
          <Button size="xs" color="teal" variant="light" onClick={() => setDialog({ type: 'add' })}>
            Add
          </Button>
        )}
      </Group>
    ),
    renderEmptyRowsFallback: () => <Text ta="center" p="md">No cryptos yet.</Text>,
  })

  const rowCount = table.getFilteredRowModel().rows.length
  const lastPageIndex = Math.max(0, Math.ceil(rowCount / pagination.pageSize) - 1)
  const isLoaded = data !== undefined
  useEffect(() => {
    // A saved page can vanish when rows are removed; wait for data so we do not reset it while loading.
    if (isLoaded && pagination.pageIndex > lastPageIndex) {
      onPaginationChange({ ...pagination, pageIndex: lastPageIndex })
    }
  }, [isLoaded, lastPageIndex, pagination, onPaginationChange])

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
      {dialog?.type === 'add' && <CryptoFormModal crypto={null} onClose={() => setDialog(null)} />}
      {dialog?.type === 'edit' && <CryptoFormModal crypto={dialog.crypto} onClose={() => setDialog(null)} />}
      {dialog?.type === 'delete' && <DeleteCryptoModal crypto={dialog.crypto} onClose={() => setDialog(null)} />}
    </Stack>
  )
}
