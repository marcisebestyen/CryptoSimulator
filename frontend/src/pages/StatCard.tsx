import { Card, Text } from '@mantine/core'
import type { ReactNode } from 'react'
import { cardStyle } from './marketFormat'

export function StatCard({ label, children, sub }: { label: string; children: ReactNode; sub?: ReactNode }) {
  return (
    <Card withBorder style={cardStyle}>
      <Text size="sm" c="dimmed">{label}</Text>
      <Text fz="1.75rem" fw={700}>{children}</Text>
      {sub}
    </Card>
  )
}
