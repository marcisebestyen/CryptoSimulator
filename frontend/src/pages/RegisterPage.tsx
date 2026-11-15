import { Alert, Anchor, Button, Center, Paper, PasswordInput, Stack, Text, TextInput, Title } from '@mantine/core'
import { IconAt, IconLock, IconUser } from '@tabler/icons-react'
import { useState, type SubmitEvent } from 'react'
import { Link, Navigate, useNavigate } from 'react-router'
import { register } from '../api/users'
import { useAuth } from '../auth/useAuth'
import { errorMessage } from './errorMessage'
import { RecoveryCodeNotice } from './RecoveryCodeNotice'

export function RegisterPage() {
  const { isAuthenticated } = useAuth()
  const navigate = useNavigate()
  const [username, setUsername] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)
  const [recoveryCode, setRecoveryCode] = useState<string | null>(null)

  if (isAuthenticated) {
    return <Navigate to="/cryptos" replace />
  }

  async function handleSubmit(event: SubmitEvent<HTMLFormElement>) {
    event.preventDefault()
    setError(null)
    setSubmitting(true)
    try {
      const response = await register(username, email, password)
      setRecoveryCode(response.recoveryCode)
    } catch (caught) {
      setError(errorMessage(caught))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Center mih="100vh" p="md">
      <Paper withBorder p="xl" w={380} maw="100%">
        {recoveryCode ? (
          <RecoveryCodeNotice code={recoveryCode} onContinue={() => void navigate('/login', { replace: true })} />
        ) : (
          <form onSubmit={handleSubmit}>
            <Stack>
              <Title order={2}>Create account</Title>
              {error && <Alert color="red">{error}</Alert>}
              <TextInput
                label="Username"
                required
                leftSection={<IconUser size={16} />}
                value={username}
                onChange={(event) => setUsername(event.currentTarget.value)}
              />
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
                Create account
              </Button>
              <Text size="sm">
                Already registered?{' '}
                <Anchor component={Link} to="/login">
                  Log in
                </Anchor>
              </Text>
            </Stack>
          </form>
        )}
      </Paper>
    </Center>
  )
}
