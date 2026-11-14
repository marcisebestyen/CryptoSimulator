import { ActionIcon, Alert, Anchor, Button, Flex, Group, Stack, Text, Title, Tooltip } from '@mantine/core'
import { IconEdit, IconTrash } from '@tabler/icons-react'
import { useQuery } from '@tanstack/react-query'
import { MantineReactTable, useMantineReactTable, type MRT_ColumnDef } from 'mantine-react-table'
import { useMemo, useState } from 'react'
import { Link } from 'react-router'
import { useAuth } from '../auth/useAuth'
import { ApiError } from '../api/client'
import { fetchCryptos, type Crypto } from '../api/cryptos'
import { PRICE_REFRESH_MS } from '../api/polling'
import { CryptoFormModal } from './CryptoFormModal'
import { DeleteCryptoModal } from './DeleteCryptoModal'

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

  const columns = useMemo<MRT_ColumnDef<Crypto>[]>(
    () => [
      { accessorKey: 'name', header: 'Name' },
      {
        accessorKey: 'currentExchangeRate',
        header: 'Exchange rate',
        Cell: ({ cell }) => formatRate(cell.getValue<number>()),
      },
      {
        id: 'history',
        header: 'History',
        enableSorting: false,
        Cell: ({ row }) => (
          <Anchor component={Link} to={`/cryptos/${row.original.id}/price-history`}>
            Price history
          </Anchor>
        ),
      },
    ],
    [],
  )

  const table = useMantineReactTable({
    columns,
    data: data ?? [],
    enablePagination: true,
    state: { isLoading },
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
    mantinePaperProps: { withBorder: true, style: { backgroundColor: 'var(--grid-body-bg)' } },
    mantineTableProps: {
      withTableBorder: true,
      withColumnBorders: true,
      withRowBorders: true,
      striped: false,
      highlightOnHover: true,
    },
    mantineTopToolbarProps: { className: 'grid-toolbar' },
    renderTopToolbarCustomActions: isAdmin
      ? () => (
          <Group>
            <Button size="xs" onClick={() => setDialog({ type: 'add' })}>
              Add
            </Button>
          </Group>
        )
      : undefined,
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
      {dialog?.type === 'add' && <CryptoFormModal crypto={null} onClose={() => setDialog(null)} />}
      {dialog?.type === 'edit' && <CryptoFormModal crypto={dialog.crypto} onClose={() => setDialog(null)} />}
      {dialog?.type === 'delete' && <DeleteCryptoModal crypto={dialog.crypto} onClose={() => setDialog(null)} />}
    </Stack>
  )
}
