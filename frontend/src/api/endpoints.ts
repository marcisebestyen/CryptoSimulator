export const endpoints = {
  login: '/api/users/login',
  cryptos: '/api/cryptos',
  crypto: (id: number) => `/api/cryptos/${id}`,
  cryptoPrice: (id: number) => `/api/cryptos/${id}/price`,
  cryptoPriceHistory: (id: number) => `/api/cryptos/${id}/price-history`,
} as const
