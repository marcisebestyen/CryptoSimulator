import type { QueryClient } from '@tanstack/react-query'

const USER_KEYS = ['my-offers', 'portfolio', 'transactions', 'profit-loss', 'profit-loss-summary']

export function invalidateOfferQueries(queryClient: QueryClient, userId: number): Promise<unknown> {
  return Promise.all([
    queryClient.invalidateQueries({ queryKey: ['market-offers'] }),
    ...USER_KEYS.map((key) => queryClient.invalidateQueries({ queryKey: [key, userId] })),
  ])
}
