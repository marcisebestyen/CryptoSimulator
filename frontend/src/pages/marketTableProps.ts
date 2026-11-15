import { cardStyle } from './marketFormat'

// Shared Market look for MantineReactTable (styles live under `.market-table` in index.css).
export const marketTableProps = {
  mantinePaperProps: { withBorder: true, radius: 'md', style: cardStyle },
  mantineTableProps: {
    withTableBorder: true,
    withColumnBorders: false,
    withRowBorders: true,
    striped: false,
    highlightOnHover: true,
    className: 'market-table',
  },
  mantineTopToolbarProps: { className: 'grid-toolbar' },
  mantineTableHeadCellProps: { className: 'grid-head-cell' },
} as const
