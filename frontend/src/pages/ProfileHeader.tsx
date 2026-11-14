import { Avatar, Badge, Box, Button, Card, Group, Stack, Text, Title } from '@mantine/core'
import { IconMail, IconPencil } from '@tabler/icons-react'
import type { User } from '../api/users'
import { initials } from './holdings'

interface Props {
  user: User
  onEdit: () => void
}

export function ProfileHeader({ user, onEdit }: Props) {
  return (
    <Card withBorder padding={0} style={{ backgroundColor: 'var(--surface-raised)', borderColor: 'var(--grid-line-color)' }}>
      <Box h={90} style={{ background: 'linear-gradient(120deg, var(--mantine-color-accent-9), var(--mantine-color-accent-6))' }} />
      <Group justify="space-between" align="flex-end" p="lg" mt={-50} wrap="wrap">
        <Group align="flex-end" wrap="nowrap">
          <Avatar size={88} radius="lg" color="accent" variant="filled" style={{ border: '4px solid var(--surface-raised)' }}>
            {initials(user.firstName, user.lastName)}
          </Avatar>
          <Stack gap={4}>
            <Title order={2}>{`${user.firstName} ${user.lastName}`}</Title>
            <Group gap="xs">
              <Text c="dimmed">{`@${user.username}`}</Text>
              <Badge variant="light" tt="none" leftSection={<IconMail size={12} />}>
                {user.email}
              </Badge>
            </Group>
          </Stack>
        </Group>
        <Button leftSection={<IconPencil size={16} />} onClick={onEdit}>
          Edit profile
        </Button>
      </Group>
    </Card>
  )
}
