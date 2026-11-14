import { AppShell, Button, NavLink } from '@mantine/core'
import { IconCoin, IconLogout } from '@tabler/icons-react'
import { Link, Outlet, useLocation } from 'react-router'
import { useAuth } from '../auth/useAuth'

export function AppLayout() {
  const { signOut } = useAuth()
  const { pathname } = useLocation()

  return (
    <AppShell navbar={{ width: 240, breakpoint: 'sm' }} padding="md">
      <AppShell.Navbar p="md">
        <AppShell.Section grow>
          <NavLink
            component={Link}
            to="/cryptos"
            label="Cryptos"
            leftSection={<IconCoin size={18} />}
            active={pathname === '/cryptos'}
          />
        </AppShell.Section>
        <AppShell.Section>
          <Button variant="subtle" fullWidth leftSection={<IconLogout size={18} />} onClick={signOut}>
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
