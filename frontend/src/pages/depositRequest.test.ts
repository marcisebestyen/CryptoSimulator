import { describe, expect, it } from 'vitest'
import { buildDepositRequest, validateDeposit, type DepositInput } from './depositRequest'

const now = new Date(2026, 10, 15)

const validNew: DepositInput = {
  userId: 7,
  amount: 100,
  savedCardId: null,
  holderName: 'ALEX MERCER',
  cardNumber: '4532 7109 3842 8896',
  expiry: '11/26',
  cvv: '842',
  saveCard: true,
}
const validSaved: DepositInput = { ...validNew, savedCardId: 3, holderName: '', cardNumber: '', expiry: '' }

describe('validateDeposit amount', () => {
  it.each([10, 10.5, 100000, 99.99])('accepts %s', (amount) => {
    expect(validateDeposit({ ...validNew, amount }, now)).toEqual({})
  })

  it.each([10.001, 9.99, 100000.01, Number.NaN, 0, -5])('rejects %s', (amount) => {
    expect(validateDeposit({ ...validNew, amount }, now).amount).toBeDefined()
  })

  it('rejects an empty amount (NumberInput gives NaN for \'\')', () => {
    expect(validateDeposit({ ...validNew, amount: Number('') || Number.NaN }, now).amount).toBeDefined()
  })
})

describe('validateDeposit expiry', () => {
  it.each([
    ['11/26', true],
    ['12/26', true],
    ['10/26', false],
    ['12/25', false],
    ['01/27', true],
    ['13/27', false],
    ['00/27', false],
    ['1/27', false],
  ])('expiry %s valid=%s', (expiry, valid) => {
    expect(validateDeposit({ ...validNew, expiry }, now).expiry === undefined).toBe(valid)
  })

  it('treats YY as 20YY across the year rollover', () => {
    const january = new Date(2027, 0, 10)
    expect(validateDeposit({ ...validNew, expiry: '01/27' }, january).expiry).toBeUndefined()
    expect(validateDeposit({ ...validNew, expiry: '12/26' }, january).expiry).toBeDefined()
  })
})

describe('validateDeposit holder name', () => {
  it.each([
    [100, true],
    [101, false],
  ])('holder of %s chars valid=%s', (length, valid) => {
    expect(validateDeposit({ ...validNew, holderName: 'A'.repeat(length) }, now).holderName === undefined).toBe(valid)
  })
})

describe('validateDeposit cvv', () => {
  it.each([
    ['123', true],
    ['1234', true],
    ['12', false],
    ['12345', false],
    ['12a', false],
    ['', false],
  ])('cvv "%s" valid=%s', (cvv, valid) => {
    expect(validateDeposit({ ...validNew, cvv }, now).cvv === undefined).toBe(valid)
  })
})

describe('validateDeposit card fields', () => {
  it('rejects bad Luhn, short number and blank holder', () => {
    expect(validateDeposit({ ...validNew, cardNumber: '4532710938428890' }, now).cardNumber).toBeDefined()
    expect(validateDeposit({ ...validNew, cardNumber: '4532 7109' }, now).cardNumber).toBeDefined()
    expect(validateDeposit({ ...validNew, holderName: '  ' }, now).holderName).toBeDefined()
  })

  it('ignores new-card fields for a saved card but still checks cvv and amount', () => {
    expect(validateDeposit(validSaved, now)).toEqual({})
    expect(validateDeposit({ ...validSaved, cvv: '1' }, now)).toEqual({ cvv: expect.any(String) })
  })
})

describe('buildDepositRequest', () => {
  it('sends savedCardId and cvv without card details for a saved card', () => {
    const request = buildDepositRequest(validSaved)
    expect(request).toEqual({ userId: 7, amount: 100, cvv: '842', savedCardId: 3, saveCard: false })
    expect(request).not.toHaveProperty('cardNumber')
  })

  it('sends digits-only number and 20YY expiry for a new card', () => {
    expect(buildDepositRequest(validNew)).toEqual({
      userId: 7,
      amount: 100,
      cvv: '842',
      holderName: 'ALEX MERCER',
      cardNumber: '4532710938428896',
      expiryMonth: 11,
      expiryYear: 2026,
      saveCard: true,
    })
  })
})
