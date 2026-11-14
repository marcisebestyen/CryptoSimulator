import { Alert, Button, Group, Modal, Stack, Text } from '@mantine/core'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { ApiError } from '../api/client'
import { deleteCrypto, type Crypto } from '../api/cryptos'

interface Props {
  crypto: Crypto
  onClose: () => void
}

export function DeleteCryptoModal({ crypto, onClose }: Props) {
  const queryClient = useQueryClient()
  const remove = useMutation({
    mutationFn: () => deleteCrypto(crypto.id),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['cryptos'] })
      onClose()
    },
  })

  return (
    <Modal opened onClose={onClose} title="Delete crypto">
      <Stack>
        {remove.error && (
          <Alert color="red" title="Could not delete">
            {remove.error instanceof ApiError ? remove.error.message : 'Cannot reach the server.'}
          </Alert>
        )}
        <Text>Delete {crypto.name}? This cannot be undone.</Text>
        <Group justify="flex-end">
          <Button variant="default" onClick={onClose}>
            Cancel
          </Button>
          <Button color="red" loading={remove.isPending} onClick={() => remove.mutate()}>
            Delete
          </Button>
        </Group>
      </Stack>
    </Modal>
  )
}
