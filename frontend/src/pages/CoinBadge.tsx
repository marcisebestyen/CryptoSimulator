import { Avatar } from '@mantine/core'

const PALETTE = ['blue', 'teal', 'grape', 'orange', 'cyan', 'pink', 'lime', 'indigo']

function initials(name: string): string {
  const words = name.trim().split(/\s+/).filter(Boolean)
  const letters = words.length > 1 ? words[0][0] + words[1][0] : (words[0] ?? '').slice(0, 2)
  return letters.toUpperCase()
}

function colorFor(name: string): string {
  let hash = 0
  for (const char of name) hash = (hash * 31 + char.charCodeAt(0)) >>> 0
  return PALETTE[hash % PALETTE.length]
}

export function CoinBadge({ name, size = 32 }: { name: string; size?: number }) {
  return (
    <Avatar radius="xl" variant="light" size={size} color={colorFor(name)} aria-hidden>
      {initials(name)}
    </Avatar>
  )
}
