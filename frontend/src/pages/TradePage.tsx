import { Alert, Button, Card, Group, Loader, SegmentedControl, SimpleGrid, Stack, Text, Title } from '@mantine/core'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { cancelOffer, fetchMarketOffers, fetchMyOffers, type MarketOffer } from '../api/offers'
import { PRICE_REFRESH_MS } from '../api/polling'
import { fetchPortfolio } from '../api/trades'
import { useAuth } from '../auth/useAuth'
import { BuyOfferModal } from './BuyOfferModal'
import { CreateOfferModal } from './CreateOfferModal'
import { errorMessage } from './errorMessage'
import { formatMoney } from './holdings'
import { cardStyle } from './marketFormat'
import { isActiveOffer, lockedValue } from './offerMath'
import { invalidateOfferQueries } from './offerQueries'
import { MarketOffersTable, MyOffersTable } from './OfferTables'
import { StatCard } from './StatCard'

type View = 'split' | 'my' | 'market'

const isView = (value: string): value is View => value === 'split' || value === 'my' || value === 'market'

export function TradePage() {
  const { userId } = useAuth()
  const queryClient = useQueryClient()
  const [view, setView] = useState<View>('split')
  const [creating, setCreating] = useState(false)
  const [buying, setBuying] = useState<MarketOffer | null>(null)
  const enabled = userId !== null
  const id = userId ?? 0

  const portfolioQuery = useQuery({ queryKey: ['portfolio', id], queryFn: () => fetchPortfolio(id), enabled, refetchInterval: PRICE_REFRESH_MS })
  const marketQuery = useQuery({
    queryKey: ['market-offers'],
    queryFn: fetchMarketOffers,
    enabled,
    refetchInterval: PRICE_REFRESH_MS,
  })
  const myQuery = useQuery({
    queryKey: ['my-offers', id],
    queryFn: fetchMyOffers,
    enabled,
    refetchInterval: PRICE_REFRESH_MS,
  })
  const cancel = useMutation({
    mutationFn: cancelOffer,
    onSuccess: () => invalidateOfferQueries(queryClient, id),
  })

  if (!enabled) return <Alert color="red">Could not determine the signed-in user.</Alert>

  const portfolio = portfolioQuery.data
  const myOffers = myQuery.data
  const ownIds = new Set((myOffers ?? []).map((offer) => offer.id))
  // Own offers are only known once my-offers loaded; until then the market list stays hidden.
  const marketOffers = myOffers && marketQuery.data?.filter((offer) => !ownIds.has(offer.id))
  const sellable = portfolio?.holdings.filter((holding) => holding.amountOwned > 0) ?? []
  const loadError = portfolioQuery.error ?? marketQuery.error ?? myQuery.error

  return (
    <Stack>
      <Card withBorder style={cardStyle}>
        <Group justify="space-between" align="center">
          <Stack gap={4}>
            <Title order={2}>Trade</Title>
            <Text c="dimmed">Sell crypto to other users or buy from their offers.</Text>
          </Stack>
          <Button disabled={sellable.length === 0} onClick={() => setCreating(true)}>
            Create offer
          </Button>
        </Group>
      </Card>

      {(portfolioQuery.isLoading || marketQuery.isLoading || myQuery.isLoading) && (
        <Loader size="sm" aria-label="Loading offers" />
      )}
      {loadError && (
        <Alert color="red" title="Could not load offers">
          {errorMessage(loadError)}
        </Alert>
      )}
      {cancel.error && (
        <Alert color="red" title="Could not cancel offer">
          {errorMessage(cancel.error)}
        </Alert>
      )}

      <SimpleGrid cols={{ base: 1, sm: 2, lg: 4 }}>
        <StatCard label="Cash">{portfolio ? `$${formatMoney(portfolio.fiatBalance)}` : '-'}</StatCard>
        <StatCard label="My active offers">{myOffers ? myOffers.filter(isActiveOffer).length : '-'}</StatCard>
        <StatCard label="Marketplace offers">{marketOffers ? marketOffers.length : '-'}</StatCard>
        <StatCard label="Locked value">{myOffers ? `$${formatMoney(lockedValue(myOffers))}` : '-'}</StatCard>
      </SimpleGrid>

      <SegmentedControl
        value={view}
        onChange={(value) => {
          if (isView(value)) setView(value)
        }}
        data={[
          { label: 'Split view', value: 'split' },
          { label: 'My offers only', value: 'my' },
          { label: 'Market only', value: 'market' },
        ]}
      />

      {view !== 'market' && myOffers && (
        <MyOffersTable
          offers={myOffers}
          cancelling={cancel.isPending ? cancel.variables : null}
          onCancel={(offerId) => cancel.mutate(offerId)}
        />
      )}
      {view !== 'my' && marketOffers && <MarketOffersTable offers={marketOffers} onBuy={setBuying} />}

      {creating && <CreateOfferModal holdings={sellable} userId={id} onClose={() => setCreating(false)} />}
      {buying && portfolio && (
        <BuyOfferModal offer={buying} cash={portfolio.fiatBalance} userId={id} onClose={() => setBuying(null)} />
      )}
    </Stack>
  )
}
