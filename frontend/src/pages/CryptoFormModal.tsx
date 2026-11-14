import { Alert, Button, Group, Modal, NumberInput, Stack, TextInput } from '@mantine/core'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useState, type FormEvent } from 'react'
import { ApiError } from '../api/client'
import { createCrypto, renameCrypto, setCryptoPrice, type Crypto } from '../api/cryptos'

interface Props {
  // null means "add a new crypto".
  crypto: Crypto | null
  onClose: () => void
}

export function CryptoFormModal({ crypto, onClose }: Props) {
  const queryClient = useQueryClient()
  const [name, setName] = useState(crypto?.name ?? '')
  const [price, setPrice] = useState<number | string>(crypto?.currentExchangeRate ?? '')

  const save = useMutation({
    mutationFn: async () => {
      const trimmed = name.trim()
      if (crypto === null) {
        await createCrypto(trimmed)
        return
      }
      if (trimmed !== crypto.name) await renameCrypto(crypto.id, trimmed)
      if (typeof price === 'number' && price !== crypto.currentExchangeRate) {
        await setCryptoPrice(crypto.id, price)
      }
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['cryptos'] })
      onClose()
    },
  })

  const submit = (event: FormEvent) => {
    event.preventDefault()
    save.mutate()
  }

  return (
    <Modal opened onClose={onClose} title={crypto === null ? 'Add crypto' : 'Edit crypto'}>
      <form onSubmit={submit}>
        <Stack>
          {save.error && (
            <Alert color="red" title="Could not save">
              {save.error instanceof ApiError ? save.error.message : 'Cannot reach the server.'}
            </Alert>
          )}
          <TextInput label="Name" value={name} onChange={(e) => setName(e.currentTarget.value)} data-autofocus />
          {crypto !== null && (
            <NumberInput label="Price" value={price} onChange={setPrice} min={0} decimalScale={8} hideControls />
          )}
          <Group justify="flex-end">
            <Button variant="default" onClick={onClose}>
              Cancel
            </Button>
            <Button type="submit" loading={save.isPending} disabled={name.trim() === ''}>
              Save
            </Button>
          </Group>
        </Stack>
      </form>
    </Modal>
  )
}
