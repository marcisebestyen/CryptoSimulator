import { Badge, Button, Card, Group, NativeSelect, Table, Text, TextInput, Title } from '@mantine/core'
import { useState } from 'react'
import type { MarketOffer, MyOffer, OfferStatus } from '../api/offers'
import { Coin } from './Coin'
import { formatAmount, formatMoney, formatPrice } from './holdings'
import { cardStyle } from './marketFormat'
import { filterAndSortOffers, isActiveOffer, isOfferSort, offerTotal, vsMarketPct, type OfferSort } from './offerMath'

const STATUS_LABEL: Record<OfferStatus, string> = {
  Open: 'Open',
  PartlySold: 'Partly sold',
  SoldOut: 'Sold out',
  Cancelled: 'Cancelled',
}
const STATUS_COLOR: Record<OfferStatus, string> = { Open: 'blue', PartlySold: 'yellow', SoldOut: 'teal', Cancelled: 'gray' }

const SORT_OPTIONS: { label: string; value: OfferSort }[] = [
  { label: 'Best deal vs market', value: 'best-deal' },
  { label: 'Lowest price', value: 'lowest-price' },
  { label: 'Highest total', value: 'highest-total' },
]

interface MyOffersProps {
  offers: MyOffer[]
  cancelling: number | null
  onCancel: (id: number) => void
}

export function MyOffersTable({ offers, cancelling, onCancel }: MyOffersProps) {
  return (
    <Card withBorder style={cardStyle}>
      <Title order={3} mb="md">My Offers</Title>
      {offers.length === 0 ? (
        <Text>You have no offers yet.</Text>
      ) : (
        <Table.ScrollContainer minWidth={700}>
          <Table aria-label="My offers">
            <Table.Thead>
              <Table.Tr>
                <Table.Th>Asset</Table.Th>
                <Table.Th ta="right">Amount</Table.Th>
                <Table.Th ta="right">Asking price</Table.Th>
                <Table.Th ta="right">Total value</Table.Th>
                <Table.Th>Status</Table.Th>
                <Table.Th />
              </Table.Tr>
            </Table.Thead>
            <Table.Tbody>
              {offers.map((offer) => (
                <Table.Tr key={offer.id}>
                  <Table.Td><Coin name={offer.cryptoName} /></Table.Td>
                  <Table.Td ta="right">{formatAmount(offer.amount)}</Table.Td>
                  <Table.Td ta="right">{`$${formatPrice(offer.askingPrice)}`}</Table.Td>
                  <Table.Td ta="right">{`$${formatMoney(offerTotal(offer.amount, offer.askingPrice))}`}</Table.Td>
                  <Table.Td>
                    <Badge variant="light" color={STATUS_COLOR[offer.status]}>{STATUS_LABEL[offer.status]}</Badge>
                    {offer.status === 'PartlySold' && (
                      <Text size="xs" c="dimmed">{`${formatAmount(offer.remainingAmount)} left of ${formatAmount(offer.amount)}`}</Text>
                    )}
                  </Table.Td>
                  <Table.Td ta="right">
                    {isActiveOffer(offer) && (
                      <Button
                        size="compact-sm"
                        color="red"
                        variant="light"
                        aria-label={`Cancel ${formatAmount(offer.remainingAmount)} ${offer.cryptoName} offer at $${formatPrice(offer.askingPrice)}`}
                        loading={cancelling === offer.id}
                        onClick={() => onCancel(offer.id)}
                      >
                        Cancel
                      </Button>
                    )}
                  </Table.Td>
                </Table.Tr>
              ))}
            </Table.Tbody>
          </Table>
        </Table.ScrollContainer>
      )}
    </Card>
  )
}

function MarketBadge({ offer }: { offer: MarketOffer }) {
  const pct = vsMarketPct(offer.askingPrice, offer.currentRate)
  if (offer.currentRate <= 0) return null
  if (Math.abs(pct) < 0.005) return <Badge variant="light" color="gray">At market</Badge>
  return (
    <Badge variant="light" color={pct < 0 ? 'teal' : 'orange'}>
      {`${formatMoney(Math.abs(pct))}% ${pct < 0 ? 'below' : 'above'} market`}
    </Badge>
  )
}

interface MarketProps {
  offers: MarketOffer[]
  onBuy: (offer: MarketOffer) => void
}

export function MarketOffersTable({ offers, onBuy }: MarketProps) {
  const [search, setSearch] = useState('')
  const [coin, setCoin] = useState('all')
  const [sort, setSort] = useState<OfferSort>('best-deal')

  const coins = [...new Map(offers.map((offer) => [offer.cryptoId, offer.cryptoName])).entries()]
  const visible = filterAndSortOffers(offers, { search, coin, sort })

  return (
    <Card withBorder style={cardStyle}>
      <Group justify="space-between" mb="md">
        <Title order={3}>Offers</Title>
        <Group>
          <TextInput
            aria-label="Search offers"
            placeholder="Search coin or seller"
            value={search}
            onChange={(e) => setSearch(e.currentTarget.value)}
          />
          <NativeSelect
            aria-label="Filter by coin"
            value={coin}
            onChange={(e) => setCoin(e.currentTarget.value)}
            data={[{ label: 'All coins', value: 'all' }, ...coins.map(([id, name]) => ({ label: name, value: String(id) }))]}
          />
          <NativeSelect
            aria-label="Sort offers"
            value={sort}
            onChange={(e) => {
              if (isOfferSort(e.currentTarget.value)) setSort(e.currentTarget.value)
            }}
            data={SORT_OPTIONS}
          />
        </Group>
      </Group>
      {visible.length === 0 ? (
        <Text>No offers available.</Text>
      ) : (
        <Table.ScrollContainer minWidth={760}>
          <Table aria-label="Offers">
            <Table.Thead>
              <Table.Tr>
                <Table.Th>Seller</Table.Th>
                <Table.Th>Asset</Table.Th>
                <Table.Th ta="right">Available</Table.Th>
                <Table.Th ta="right">Asking price</Table.Th>
                <Table.Th>Vs market</Table.Th>
                <Table.Th />
              </Table.Tr>
            </Table.Thead>
            <Table.Tbody>
              {visible.map((offer) => (
                <Table.Tr key={offer.id}>
                  <Table.Td>
                    <Text fw={500}>{offer.sellerUsername}</Text>
                    <Text size="xs" c="dimmed">
                      {`${offer.sellerCompletionRate === null ? 'n/a' : `${formatMoney(offer.sellerCompletionRate * 100)}%`} completion, ${offer.sellerTrades} trades`}
                    </Text>
                  </Table.Td>
                  <Table.Td><Coin name={offer.cryptoName} /></Table.Td>
                  <Table.Td ta="right">{formatAmount(offer.remainingAmount)}</Table.Td>
                  <Table.Td ta="right">{`$${formatPrice(offer.askingPrice)}`}</Table.Td>
                  <Table.Td><MarketBadge offer={offer} /></Table.Td>
                  <Table.Td ta="right">
                    <Button
                      size="compact-sm"
                      color="teal"
                      variant="light"
                      aria-label={`Buy ${offer.cryptoName} from ${offer.sellerUsername}`}
                      onClick={() => onBuy(offer)}
                    >
                      Buy
                    </Button>
                  </Table.Td>
                </Table.Tr>
              ))}
            </Table.Tbody>
          </Table>
        </Table.ScrollContainer>
      )}
    </Card>
  )
}
