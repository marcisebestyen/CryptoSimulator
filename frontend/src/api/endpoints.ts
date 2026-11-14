export const endpoints = {
  login: '/api/users/login',
  user: (id: number) => `/api/users/${id}`,
  userWallet: (id: number) => `/api/users/${id}/wallet`,
  cryptos: '/api/cryptos',
  crypto: (id: number) => `/api/cryptos/${id}`,
  cryptoPrice: (id: number) => `/api/cryptos/${id}/price`,
  cryptoPriceHistory: (id: number) => `/api/cryptos/${id}/price-history`,
} as const
