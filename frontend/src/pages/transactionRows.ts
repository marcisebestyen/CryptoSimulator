import type { Crypto } from '../api/cryptos'
import type { Transaction } from '../api/transactions'

export type TransactionRow = Transaction & { cryptoName: string; total: number }

export function toRows(transactions: Transaction[], cryptos: Crypto[]): TransactionRow[] {
  const names = new Map(cryptos.map((crypto) => [crypto.id, crypto.name]))
  return transactions.map((tx) => ({
    ...tx,
    cryptoName: names.get(tx.cryptoId) ?? `Crypto #${tx.cryptoId}`,
    total: tx.amount * tx.exchangeRate,
  }))
}
