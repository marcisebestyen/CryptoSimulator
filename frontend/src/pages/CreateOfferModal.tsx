import { Alert, Button, Group, Modal, NativeSelect, Stack, Text, TextInput } from '@mantine/core'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useState, type FormEvent } from 'react'
import { createOffer } from '../api/offers'
import type { PortfolioItem } from '../api/trades'
import { errorMessage } from './errorMessage'
import { formatMoney, formatPrice } from './holdings'
import { invalidateOfferQueries } from './offerQueries'
import { floorAmount } from './tradeMath'
import { offerTotal, priceTextError, sellQuantityError } from './offerMath'

interface Props {
  holdings: PortfolioItem[]
  userId: number
  onClose: () => void
}

// Mounted only while open, so state starts fresh every time.
export function CreateOfferModal({ holdings, userId, onClose }: Props) {
  const queryClient = useQueryClient()
  const [coinId, setCoinId] = useState<number | null>(null)
  const [quantityText, setQuantityText] = useState('')
  const [priceText, setPriceText] = useState('')

  const coin = holdings.find((holding) => holding.cryptoId === coinId) ?? holdings[0]
  const quantityError = coin ? sellQuantityError(quantityText, coin.amountOwned) : 'No coin available.'
  const priceError = priceTextError(priceText)
  const error = quantityError ?? priceError
  const payout = error ? 0 : offerTotal(Number(quantityText), Number(priceText))

  const create = useMutation({
    mutationFn: (cryptoId: number) => createOffer({ cryptoId, amount: Number(quantityText), askingPrice: Number(priceText) }),
    onSuccess: async () => {
      await invalidateOfferQueries(queryClient, userId)
      onClose()
    },
  })

  const submit = (event: FormEvent) => {
    event.preventDefault()
    if (!error && coin) create.mutate(coin.cryptoId)
  }

  return (
    <Modal opened onClose={onClose} title="Create sell offer">
      <form onSubmit={submit}>
        <Stack>
          <NativeSelect
            label="Coin"
            value={coin?.cryptoId ?? ''}
            onChange={(e) => setCoinId(Number(e.currentTarget.value))}
            data={holdings.map((holding) => ({ label: holding.cryptoName, value: String(holding.cryptoId) }))}
          />
          <TextInput
            label="Quantity"
            inputMode="decimal"
            value={quantityText}
            onChange={(e) => setQuantityText(e.currentTarget.value)}
            error={quantityText !== '' ? quantityError : null}
            rightSectionWidth={60}
            rightSection={
              <Button variant="subtle" size="compact-xs" onClick={() => setQuantityText(String(floorAmount(coin?.amountOwned ?? 0)))}>
                Max
              </Button>
            }
            data-autofocus
          />
          <TextInput
            label="Asking price"
            description={coin ? `Market rate $${formatPrice(coin.currentExchangeRate)}` : undefined}
            inputMode="decimal"
            value={priceText}
            onChange={(e) => setPriceText(e.currentTarget.value)}
            error={priceText !== '' ? priceError : null}
          />
          <Group justify="space-between">
            <Text c="dimmed">You receive when sold</Text>
            <Text fw={700}>{`$${formatMoney(payout)}`}</Text>
          </Group>
          {create.error && (
            <Alert color="red" title="Could not create offer">
              {errorMessage(create.error)}
            </Alert>
          )}
          <Group justify="flex-end">
            <Button variant="default" onClick={onClose}>
              Close
            </Button>
            <Button type="submit" loading={create.isPending} disabled={error !== null}>
              List offer
            </Button>
          </Group>
        </Stack>
      </form>
    </Modal>
  )
}
