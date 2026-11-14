import { AppShell, Button, Divider, NavLink, Text } from '@mantine/core'
import { IconCoin, IconLogout, IconUser } from '@tabler/icons-react'
import { Link, Outlet, useLocation } from 'react-router'
import { useAuth } from '../auth/useAuth'

export function AppLayout() {
  const { signOut } = useAuth()
  const { pathname } = useLocation()

  return (
    <AppShell navbar={{ width: 240, breakpoint: 'sm' }} padding="md">
      <AppShell.Navbar p="md" className="app-navbar">
        <AppShell.Section>
          <Text fw={700} size="lg" pb="md">
            Crypto Simulator
          </Text>
          <Divider mb="sm" />
        </AppShell.Section>
        <AppShell.Section grow>
          <NavLink
            component={Link}
            to="/cryptos"
            label="Cryptos"
            leftSection={<IconCoin size={18} />}
            active={pathname === '/cryptos'}
            style={{ borderRadius: 'var(--mantine-radius-default)' }}
          />
        </AppShell.Section>
        <AppShell.Section>
          <NavLink
            component={Link}
            to="/profile"
            label="Profile"
            leftSection={<IconUser size={18} />}
            active={pathname === '/profile'}
            style={{ borderRadius: 'var(--mantine-radius-default)' }}
          />
          <Button variant="subtle" fullWidth className="logout-button" leftSection={<IconLogout size={18} />} onClick={signOut}
          >
            Log out
          </Button>
        </AppShell.Section>
      </AppShell.Navbar>
      <AppShell.Main>
        <Outlet />
      </AppShell.Main>
    </AppShell>
  )
}
