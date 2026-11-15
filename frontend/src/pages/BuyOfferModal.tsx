import { Alert, Button, Group, Modal, Stack, Text, TextInput } from '@mantine/core'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useState, type FormEvent } from 'react'
import { buyOffer, type MarketOffer } from '../api/offers'
import { errorMessage } from './errorMessage'
import { formatAmount, formatMoney, formatPrice } from './holdings'
import { buyQuantityError, offerTotal } from './offerMath'
import { invalidateOfferQueries } from './offerQueries'

interface Props {
  offer: MarketOffer
  cash: number
  userId: number
  onClose: () => void
}

export function BuyOfferModal({ offer, cash, userId, onClose }: Props) {
  const queryClient = useQueryClient()
  const [quantityText, setQuantityText] = useState(String(offer.remainingAmount))

  const error = buyQuantityError(quantityText, offer.remainingAmount, offer.askingPrice, cash)
  const total = error ? 0 : offerTotal(Number(quantityText), offer.askingPrice)

  const buy = useMutation({
    mutationFn: () => buyOffer(offer.id, Number(quantityText)),
    onSuccess: async () => {
      await invalidateOfferQueries(queryClient, userId)
      onClose()
    },
    // The offer may be gone or smaller now, so the list must not stay stale.
    onError: () => queryClient.invalidateQueries({ queryKey: ['market-offers'] }),
  })

  const submit = (event: FormEvent) => {
    event.preventDefault()
    if (!error) buy.mutate()
  }

  return (
    <Modal opened onClose={onClose} title={`Buy ${offer.cryptoName} from ${offer.sellerUsername}`}>
      <form onSubmit={submit}>
        <Stack>
          <Group justify="space-between">
            <Text c="dimmed">Price per unit</Text>
            <Text>{`$${formatPrice(offer.askingPrice)}`}</Text>
          </Group>
          <TextInput
            label="Quantity"
            description={`Available: ${formatAmount(offer.remainingAmount)}`}
            inputMode="decimal"
            value={quantityText}
            onChange={(e) => setQuantityText(e.currentTarget.value)}
            error={quantityText !== '' ? error : null}
            data-autofocus
          />
          <Group justify="space-between">
            <Text c="dimmed">Total cost</Text>
            <Text fw={700}>{`$${formatMoney(total)}`}</Text>
          </Group>
          {buy.error && (
            <Alert color="red" title="Purchase failed">
              {errorMessage(buy.error)}
            </Alert>
          )}
          <Group justify="flex-end">
            <Button variant="default" onClick={onClose}>
              Close
            </Button>
            <Button type="submit" color="teal" loading={buy.isPending} disabled={error !== null}>
              Confirm purchase
            </Button>
          </Group>
        </Stack>
      </form>
    </Modal>
  )
}
