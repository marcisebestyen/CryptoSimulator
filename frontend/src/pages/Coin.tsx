import { Group, Text } from '@mantine/core'
import { CoinBadge } from './CoinBadge'

export function Coin({ name }: { name: string }) {
  return (
    <Group gap="sm" wrap="nowrap">
      <CoinBadge name={name} size={28} />
      <Text fw={500}>{name}</Text>
    </Group>
  )
}
