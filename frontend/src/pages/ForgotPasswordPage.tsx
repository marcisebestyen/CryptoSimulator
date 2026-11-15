import { Alert, Anchor, Button, Center, Paper, PasswordInput, Stack, Text, TextInput, Title } from '@mantine/core'
import { IconAt, IconKey, IconLock } from '@tabler/icons-react'
import { useState, type SubmitEvent } from 'react'
import { Link, Navigate, useNavigate } from 'react-router'
import { forgotPassword } from '../api/users'
import { useAuth } from '../auth/useAuth'
import { errorMessage } from './errorMessage'
import { RecoveryCodeNotice } from './RecoveryCodeNotice'

const newCodeDescription =
  'Your password was reset. This is your new recovery code. Your old recovery code no longer works. It is shown only once, so store it somewhere safe.'

export function ForgotPasswordPage() {
  const { isAuthenticated } = useAuth()
  const navigate = useNavigate()
  const [email, setEmail] = useState('')
  const [code, setCode] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)
  const [newRecoveryCode, setNewRecoveryCode] = useState<string | null>(null)

  if (isAuthenticated) {
    return <Navigate to="/cryptos" replace />
  }

  async function handleSubmit(event: SubmitEvent<HTMLFormElement>) {
    event.preventDefault()
    setError(null)
    setSubmitting(true)
    try {
      const response = await forgotPassword(email, code.trim(), newPassword)
      setNewRecoveryCode(response.recoveryCode)
    } catch (caught) {
      setError(errorMessage(caught))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Center mih="100vh" p="md">
      <Paper withBorder p="xl" w={380} maw="100%">
        {newRecoveryCode ? (
          <RecoveryCodeNotice
            code={newRecoveryCode}
            description={newCodeDescription}
            onContinue={() => void navigate('/login', { replace: true })}
          />
        ) : (
          <form onSubmit={handleSubmit}>
            <Stack>
              <Title order={2}>Reset password</Title>
              {error && <Alert color="red">{error}</Alert>}
              <TextInput
                label="Email"
                type="email"
                required
                leftSection={<IconAt size={16} />}
                value={email}
                onChange={(event) => setEmail(event.currentTarget.value)}
              />
              <TextInput
                label="Recovery code"
                required
                autoComplete="off"
                leftSection={<IconKey size={16} />}
                value={code}
                onChange={(event) => setCode(event.currentTarget.value)}
              />
              <PasswordInput
                label="New password"
                required
                leftSection={<IconLock size={16} />}
                value={newPassword}
                onChange={(event) => setNewPassword(event.currentTarget.value)}
              />
              <Button type="submit" loading={submitting}>
                Reset password
              </Button>
              <Text size="sm">
                <Anchor component={Link} to="/login">
                  Back to login
                </Anchor>
              </Text>
            </Stack>
          </form>
        )}
      </Paper>
    </Center>
  )
}
