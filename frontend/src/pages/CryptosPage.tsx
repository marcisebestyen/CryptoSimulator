import { Alert, Anchor, Button, Group, Stack, Text, Title } from '@mantine/core'
import { useQuery } from '@tanstack/react-query'
import { MantineReactTable, useMantineReactTable, type MRT_ColumnDef, type MRT_RowSelectionState } from 'mantine-react-table'
import { useMemo, useState } from 'react'
import { Link } from 'react-router'
import { useAuth } from '../auth/useAuth'
import { ApiError } from '../api/client'
import { fetchCryptos, type Crypto } from '../api/cryptos'
import { CryptoFormModal } from './CryptoFormModal'
import { DeleteCryptoModal } from './DeleteCryptoModal'

type Dialog = 'add' | 'edit' | 'delete' | null

const formatRate = (rate: number) => rate.toLocaleString(undefined, { maximumFractionDigits: 8 })

export function CryptosPage() {
  const { isAdmin } = useAuth()
  const [dialog, setDialog] = useState<Dialog>(null)
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
      {
        id: 'history',
        header: 'History',
        enableSorting: false,
        Cell: ({ row }) => (
          <Anchor
            component={Link}
            to={`/cryptos/${row.original.id}/price-history`}
            onClick={(event) => event.stopPropagation()}
          >
            Price history
          </Anchor>
        ),
      },
    ],
    [],
  )

  const [rowSelection, setRowSelection] = useState<MRT_RowSelectionState>({})
  const selectedCryptos = (data ?? []).filter((crypto) => rowSelection[String(crypto.id)])
  const selected = selectedCryptos.length === 1 ? selectedCryptos[0] : undefined

  const table = useMantineReactTable({
    columns,
    data: data ?? [],
    enablePagination: true,
    enableRowSelection: true,
    state: { isLoading, rowSelection },
    onRowSelectionChange: setRowSelection,
    getRowId: (crypto) => String(crypto.id),
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
    renderTopToolbarCustomActions: isAdmin
      ? () => (
          <Group>
            <Button size="xs" onClick={() => setDialog('add')}>
              Add
            </Button>
            <Button size="xs" disabled={selected === undefined} onClick={() => setDialog('edit')}>
              Edit
            </Button>
            <Button size="xs" color="red" disabled={selected === undefined} onClick={() => setDialog('delete')}>
              Delete
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
      {dialog === 'add' && <CryptoFormModal crypto={null} onClose={() => setDialog(null)} />}
      {dialog === 'edit' && selected && <CryptoFormModal crypto={selected} onClose={() => setDialog(null)} />}
      {dialog === 'delete' && selected && <DeleteCryptoModal crypto={selected} onClose={() => setDialog(null)} />}
    </Stack>
  )
}
