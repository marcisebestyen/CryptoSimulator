import { Alert, Button, Group, Modal, NumberInput, Stack, TextInput } from '@mantine/core'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useState, type FormEvent } from 'react'
import { ApiError } from '../api/client'
import { createCrypto, renameCrypto, setCryptoPrice, type Crypto } from '../api/cryptos'

// Backend rejects prices below this (ManualPriceUpdateDto / CryptoPostDto range).
const MIN_PRICE = 0.01

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
        if (typeof price !== 'number') return
        await createCrypto(trimmed, price)
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
          <NumberInput label="Price" value={price} onChange={setPrice} min={0} decimalScale={8} hideControls />
          <Group justify="flex-end">
            <Button variant="default" onClick={onClose}>
              Cancel
            </Button>
            <Button type="submit" loading={save.isPending} disabled={name.trim() === '' || (crypto === null && !(typeof price === 'number' && price >= MIN_PRICE))}>
              Save
            </Button>
          </Group>
        </Stack>
      </form>
    </Modal>
  )
}
