import { Button, Code, CopyButton, Group, Stack, Text, Title } from '@mantine/core'

interface RecoveryCodeNoticeProps {
  code: string
  onContinue: () => void
  description?: string
}

const defaultDescription =
  'This code resets your password if you forget it. It is shown only once, so store it somewhere safe.'

export function RecoveryCodeNotice({ code, onContinue, description = defaultDescription }: RecoveryCodeNoticeProps) {
  return (
    <Stack>
      <Title order={2}>Save your recovery code</Title>
      <Text size="sm">{description}</Text>
      <Code block fz="lg" ta="center">
        {code}
      </Code>
      <Group grow>
        <CopyButton value={code}>
          {({ copied, copy }) => (
            <Button variant="default" onClick={copy}>
              {copied ? 'Copied' : 'Copy code'}
            </Button>
          )}
        </CopyButton>
        <Button onClick={onContinue}>I saved it</Button>
      </Group>
    </Stack>
  )
}
