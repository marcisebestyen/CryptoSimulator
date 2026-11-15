import { Alert, Button, Group, Modal, NativeSelect, SegmentedControl, Stack, Text, TextInput } from '@mantine/core'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useState, type FormEvent } from 'react'
import type { Crypto } from '../api/cryptos'
import { buyCrypto, sellCrypto, type Portfolio } from '../api/trades'
import { errorMessage } from './errorMessage'
import { formatMoney } from './holdings'
import { amountTextError, maxAmount, tradeError, type TradeMode } from './tradeMath'

interface Props {
  mode: TradeMode
  cryptoId: number | null
  cryptos: Crypto[]
  portfolio: Portfolio
  userId: number
  onClose: () => void
}

const INVALIDATED_KEYS = ['portfolio', 'transactions', 'profit-loss', 'profit-loss-summary']

// Mounted only while open, so state starts fresh from the clicked row every time.
export function TradeModal({ mode: initialMode, cryptoId, cryptos, portfolio, userId, onClose }: Props) {
  const queryClient = useQueryClient()
  const [mode, setMode] = useState<TradeMode>(initialMode)
  const [coinId, setCoinId] = useState<number | null>(cryptoId)
  const [amountText, setAmountText] = useState('')

  const owned = (id: number | null) => portfolio.holdings.find((holding) => holding.cryptoId === id)?.amountOwned ?? 0
  const options = mode === 'sell' ? cryptos.filter((crypto) => owned(crypto.id) > 0) : cryptos
  const coin = options.find((crypto) => crypto.id === coinId) ?? options[0]

  const rate = coin?.currentExchangeRate ?? 0
  const ownedAmount = owned(coin?.id ?? null)
  const textError = amountTextError(amountText)
  const amount = textError ? Number.NaN : Number(amountText)
  const error = coin ? (textError ?? tradeError(mode, amount, rate, portfolio.fiatBalance, ownedAmount)) : 'No coin available.'
  const total = Number.isFinite(amount) ? amount * rate : 0

  const trade = useMutation({
    mutationFn: () => {
      const order = { userId, cryptoId: coin?.id ?? 0, amount }
      return mode === 'buy' ? buyCrypto(order) : sellCrypto(order)
    },
    onSuccess: async () => {
      await Promise.all(INVALIDATED_KEYS.map((key) => queryClient.invalidateQueries({ queryKey: [key, userId] })))
      onClose()
    },
  })

  const submit = (event: FormEvent) => {
    event.preventDefault()
    if (!error) trade.mutate()
  }

  return (
    <Modal opened onClose={onClose} title="Trade crypto">
      <form onSubmit={submit}>
        <Stack>
          <SegmentedControl
            fullWidth
            value={mode}
            onChange={(value) => setMode(value as TradeMode)}
            data={[
              { label: 'Buy', value: 'buy' },
              { label: 'Sell', value: 'sell' },
            ]}
          />
          <NativeSelect
            label="Coin"
            value={coin?.id ?? ''}
            onChange={(e) => setCoinId(Number(e.currentTarget.value))}
            data={options.map((crypto) => ({ label: crypto.name, value: String(crypto.id) }))}
          />
          <TextInput
            label="Amount"
            inputMode="decimal"
            value={amountText}
            onChange={(e) => setAmountText(e.currentTarget.value)}
            error={amountText !== '' ? error : null}
            rightSectionWidth={60}
            rightSection={
              <Button
                variant="subtle"
                size="compact-xs"
                onClick={() => setAmountText(String(maxAmount(mode, portfolio.fiatBalance, rate, ownedAmount)))}
              >
                Max
              </Button>
            }
            data-autofocus
          />
          <Group justify="space-between">
            <Text c="dimmed">Total</Text>
            <Text fw={700}>{`$${formatMoney(total)}`}</Text>
          </Group>
          {trade.error && (
            <Alert color="red" title="Trade failed">
              {errorMessage(trade.error)}
            </Alert>
          )}
          <Group justify="flex-end">
            <Button variant="default" onClick={onClose}>
              Cancel
            </Button>
            <Button type="submit" loading={trade.isPending} disabled={error !== null}>
              Confirm
            </Button>
          </Group>
        </Stack>
      </form>
    </Modal>
  )
}
