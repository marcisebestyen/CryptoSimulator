export type Network = 'VISA' | 'MASTERCARD' | 'AMEX'

const onlyDigits = (raw: string) => raw.replace(/\D/g, '')

export function formatCardNumber(raw: string): string {
  const digits = onlyDigits(raw).slice(0, 16)
  return digits.replace(/(\d{4})(?=\d)/g, '$1 ')
}

export function formatExpiry(raw: string): string {
  const digits = onlyDigits(raw).slice(0, 4)
  return digits.length > 2 ? `${digits.slice(0, 2)}/${digits.slice(2)}` : digits
}

export function parseExpiry(value: string): { month: number; year: number } | null {
  const match = /^(\d{2})\/(\d{2})$/.exec(value)
  if (!match) return null
  const month = Number(match[1])
  if (month < 1 || month > 12) return null
  return { month, year: 2000 + Number(match[2]) }
}

// Same prefix rules as the backend CardRules.DetectNetwork.
export function detectNetwork(digits: string): Network {
  if (/^3[47]/.test(digits)) return 'AMEX'
  if (/^5[1-5]/.test(digits)) return 'MASTERCARD'
  return 'VISA'
}

export function isLuhnValid(digits: string): boolean {
  if (!/^\d+$/.test(digits)) return false
  let sum = 0
  for (let index = 0; index < digits.length; index++) {
    let digit = Number(digits[digits.length - 1 - index])
    if (index % 2 === 1) {
      digit *= 2
      if (digit > 9) digit -= 9
    }
    sum += digit
  }
  return sum % 10 === 0
}
