import { describe, expect, it } from 'vitest'
import { detectNetwork, formatCardNumber, formatExpiry, isLuhnValid, parseExpiry } from './cardFormat'

describe('formatCardNumber', () => {
  it('groups digits by four', () => {
    expect(formatCardNumber('4532710938428896')).toBe('4532 7109 3842 8896')
  })

  it('strips letters and spaces', () => {
    expect(formatCardNumber('45a32 71')).toBe('4532 71')
  })

  it('truncates at 16 digits', () => {
    expect(formatCardNumber('45327109384288961234')).toBe('4532 7109 3842 8896')
  })
})

describe('formatExpiry', () => {
  it('inserts a slash after the month', () => {
    expect(formatExpiry('0929')).toBe('09/29')
    expect(formatExpiry('092')).toBe('09/2')
    expect(formatExpiry('09')).toBe('09')
  })

  it('keeps at most four digits', () => {
    expect(formatExpiry('092999')).toBe('09/29')
  })
})

describe('parseExpiry', () => {
  it('maps YY to 20YY', () => {
    expect(parseExpiry('09/29')).toEqual({ month: 9, year: 2029 })
  })

  it.each(['13/29', '00/29', '09/2', '0929', ''])('rejects %s', (value) => {
    expect(parseExpiry(value)).toBeNull()
  })
})

describe('detectNetwork', () => {
  it.each([
    ['4111111111111111', 'VISA'],
    ['5105105105105100', 'MASTERCARD'],
    ['371449635398431', 'AMEX'],
    ['9999', 'VISA'],
  ] as const)('%s is %s', (digits, network) => {
    expect(detectNetwork(digits)).toBe(network)
  })
})

describe('isLuhnValid', () => {
  it('accepts a valid number', () => {
    expect(isLuhnValid('4532710938428896')).toBe(true)
  })

  it('rejects a changed last digit', () => {
    expect(isLuhnValid('4532710938428890')).toBe(false)
  })

  it('rejects non-digits and empty input', () => {
    expect(isLuhnValid('4532 7109')).toBe(false)
    expect(isLuhnValid('')).toBe(false)
  })
})
