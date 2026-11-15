import { Alert, Anchor, Button, Center, Group, Paper, PasswordInput, Stack, TextInput, Title } from '@mantine/core'
import { IconAt, IconLock } from '@tabler/icons-react'
import { useState, type SubmitEvent } from 'react'
import { Link, Navigate, useNavigate } from 'react-router'
import { ApiError } from '../api/client'
import { useAuth } from '../auth/useAuth'

export function LoginPage() {
  const { isAuthenticated, signIn } = useAuth()
  const navigate = useNavigate()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  if (isAuthenticated) {
    return <Navigate to="/cryptos" replace />
  }

  async function handleSubmit(event: SubmitEvent<HTMLFormElement>) {
    event.preventDefault()
    setError(null)
    setSubmitting(true)
    try {
      await signIn(email, password)
      await navigate('/cryptos', { replace: true })
    } catch (caught) {
      setError(caught instanceof ApiError ? caught.message : 'Cannot reach the server.')
      setSubmitting(false)
    }
  }

  return (
    <Center mih="100vh" p="md">
      <Paper withBorder p="xl" w={380} maw="100%">
        <form onSubmit={handleSubmit}>
          <Stack>
            <Title order={2}>Welcome back</Title>
            {error && <Alert color="red">{error}</Alert>}
            <TextInput
              label="Email"
              type="email"
              required
              leftSection={<IconAt size={16} />}
              value={email}
              onChange={(event) => setEmail(event.currentTarget.value)}
            />
            <PasswordInput
              label="Password"
              required
              leftSection={<IconLock size={16} />}
              value={password}
              onChange={(event) => setPassword(event.currentTarget.value)}
            />
            <Button type="submit" loading={submitting}>
              Log in
            </Button>
            <Group justify="space-between">
              <Anchor component={Link} to="/register" size="sm">
                Create an account
              </Anchor>
              <Anchor component={Link} to="/forgot-password" size="sm">
                Forgot password?
              </Anchor>
            </Group>
          </Stack>
        </form>
      </Paper>
    </Center>
  )
}
