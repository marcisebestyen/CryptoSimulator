import { ActionIcon, Alert, Anchor, Button, Checkbox, Group, NumberInput, PasswordInput, Select, Stack, TextInput } from '@mantine/core'
import { IconTrash } from '@tabler/icons-react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useState, type SubmitEvent } from 'react'
import { createDeposit, deleteCard, type SavedCard } from '../api/deposits'
import { formatCardNumber, formatExpiry } from './cardFormat'
import { buildDepositRequest, validateDeposit, type DepositErrors } from './depositRequest'
import { errorMessage } from './errorMessage'
import { formatMoney } from './holdings'

const PRESETS = [100, 500, 1000, 5000, 10000]
const NEW_CARD = 'new'

interface Props {
  userId: number
  cards: SavedCard[]
}

const cardLabel = (card: SavedCard) => `${card.network.toUpperCase()} •••• ${card.last4}`

function demoExpiry(): string {
  return `09/${String((new Date().getFullYear() + 3) % 100).padStart(2, '0')}`
}

export function DepositForm({ userId, cards }: Props) {
  const queryClient = useQueryClient()
  const [amount, setAmount] = useState<number | string>('')
  const [choice, setChoice] = useState<string | null>(null)
  const [holderName, setHolderName] = useState('')
  const [cardNumber, setCardNumber] = useState('')
  const [expiry, setExpiry] = useState('')
  const [cvv, setCvv] = useState('')
  const [saveCard, setSaveCard] = useState(false)
  const [errors, setErrors] = useState<DepositErrors>({})

  const selected = cards.find((card) => String(card.id) === choice) ?? (choice === NEW_CARD ? undefined : cards[0])
  const amountValue = typeof amount === 'number' ? amount : Number.NaN
  const invalidate = (key: string) => queryClient.invalidateQueries({ queryKey: [key, userId] })

  const deposit = useMutation({
    mutationFn: createDeposit,
    onSuccess: async () => {
      setCvv('')
      setCardNumber('')
      await Promise.all(['portfolio', 'deposits', 'cards'].map(invalidate))
    },
  })
  const removeCard = useMutation({
    mutationFn: (cardId: number) => deleteCard(userId, cardId),
    onSuccess: () => {
      setChoice(null)
      return invalidate('cards')
    },
  })

  const fillDemo = () => {
    setChoice(NEW_CARD)
    setHolderName('ALEX MERCER')
    setCardNumber(formatCardNumber('4532710938428896'))
    setExpiry(demoExpiry())
    setCvv('842')
  }

  const submit = (event: SubmitEvent<HTMLFormElement>) => {
    event.preventDefault()
    deposit.reset()
    const input = {
      userId,
      amount: amountValue,
      savedCardId: selected?.id ?? null,
      holderName,
      cardNumber,
      expiry,
      cvv,
      saveCard,
    }
    const found = validateDeposit(input)
    setErrors(found)
    if (Object.keys(found).length === 0) deposit.mutate(buildDepositRequest(input))
  }

  return (
    <form onSubmit={submit}>
      <Stack>
        {deposit.isSuccess && (
          <Alert color="teal">{`Deposit successful. New balance: $${formatMoney(deposit.data.newBalance)}`}</Alert>
        )}
        {deposit.isError && <Alert color="red">{errorMessage(deposit.error)}</Alert>}
        {removeCard.isError && <Alert color="red">{errorMessage(removeCard.error)}</Alert>}
        <Group gap="xs">
          {PRESETS.map((preset) => (
            <Button key={preset} variant="light" size="xs" onClick={() => setAmount(preset)}>
              {`$${preset.toLocaleString('en-US')}`}
            </Button>
          ))}
        </Group>
        <NumberInput
          label="Amount"
          min={10}
          max={100000}
          decimalScale={2}
          allowNegative={false}
          hideControls
          leftSection="$"
          value={amount}
          onChange={setAmount}
          error={errors.amount}
        />
        {cards.length > 0 && (
          <Group align="flex-end" wrap="nowrap">
            <Select
              label="Card"
              allowDeselect={false}
              style={{ flex: 1 }}
              data={[...cards.map((card) => ({ value: String(card.id), label: cardLabel(card) })), { value: NEW_CARD, label: 'New card' }]}
              value={selected ? String(selected.id) : NEW_CARD}
              onChange={setChoice}
            />
            {selected && (
              <ActionIcon
                variant="subtle"
                color="red"
                size="lg"
                aria-label={`Delete card ${cardLabel(selected)}`}
                loading={removeCard.isPending}
                onClick={() => removeCard.mutate(selected.id)}
              >
                <IconTrash size={16} />
              </ActionIcon>
            )}
          </Group>
        )}
        {!selected && (
          <>
            <Group justify="space-between">
              <span />
              <Anchor component="button" type="button" size="sm" onClick={fillDemo}>
                Fill Demo Card
              </Anchor>
            </Group>
            <TextInput
              label="Cardholder name"
              maxLength={100}
              value={holderName}
              error={errors.holderName}
              onChange={(event) => setHolderName(event.currentTarget.value.toUpperCase())}
            />
            <TextInput
              label="Card number"
              inputMode="numeric"
              autoComplete="off"
              value={cardNumber}
              error={errors.cardNumber}
              onChange={(event) => setCardNumber(formatCardNumber(event.currentTarget.value))}
            />
            <TextInput
              label="Expiry (MM/YY)"
              inputMode="numeric"
              autoComplete="off"
              value={expiry}
              error={errors.expiry}
              onChange={(event) => setExpiry(formatExpiry(event.currentTarget.value))}
            />
          </>
        )}
        <PasswordInput
          label="CVV"
          autoComplete="off"
          value={cvv}
          error={errors.cvv}
          onChange={(event) => setCvv(event.currentTarget.value.replace(/\D/g, '').slice(0, 4))}
        />
        {!selected && (
          <Checkbox
            label="Save card for future deposits"
            checked={saveCard}
            onChange={(event) => setSaveCard(event.currentTarget.checked)}
          />
        )}
        <Button type="submit" loading={deposit.isPending} loaderProps={{ type: 'dots' }}>
          {deposit.isPending ? 'Authorizing…' : `Deposit $${(Number.isFinite(amountValue) ? amountValue : 0).toFixed(2)} to Wallet`}
        </Button>
      </Stack>
    </form>
  )
}
