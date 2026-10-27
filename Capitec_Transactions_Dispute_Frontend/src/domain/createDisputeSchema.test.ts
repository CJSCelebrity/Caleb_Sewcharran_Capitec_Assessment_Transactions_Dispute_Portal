import { describe, expect, it } from 'vitest'
import { createDisputeSchema } from './createDisputeSchema'

const valid = {
  transactionId: '0199c0de-1001-7000-8000-000000000009',
  transactionAmount: 250,
  transactionCurrency: 'zar',
  transactionDate: '2026-10-20T12:00',
  merchantName: 'Checkers',
  reason: 'Goods not received.',
}

describe('createDisputeSchema', () => {
  it('uppercases the currency, matching the server', () => {
    const parsed = createDisputeSchema.parse(valid)

    expect(parsed.transactionCurrency).toBe('ZAR')
  })

  it('converts the local datetime input to an iso utc string', () => {
    const parsed = createDisputeSchema.parse(valid)

    expect(parsed.transactionDate).toBe(new Date('2026-10-20T12:00').toISOString())
    expect(parsed.transactionDate.endsWith('Z')).toBe(true)
  })

  it('rejects a transaction id that is not a uuid', () => {
    expect(createDisputeSchema.safeParse({ ...valid, transactionId: 'nope' }).success).toBe(false)
  })

  it('rejects an amount below the server minimum of 0.01', () => {
    expect(createDisputeSchema.safeParse({ ...valid, transactionAmount: 0 }).success).toBe(false)
  })

  it('rejects an amount above the server maximum of 1000000000', () => {
    expect(createDisputeSchema.safeParse({ ...valid, transactionAmount: 1_000_000_001 }).success).toBe(false)
  })

  it('rejects an amount with more than two decimal places', () => {
    expect(createDisputeSchema.safeParse({ ...valid, transactionAmount: 10.999 }).success).toBe(false)
  })

  it('rejects a currency that is not exactly three letters', () => {
    expect(createDisputeSchema.safeParse({ ...valid, transactionCurrency: 'RAND' }).success).toBe(false)
  })

  it('rejects a merchant name longer than 200 characters', () => {
    expect(createDisputeSchema.safeParse({ ...valid, merchantName: 'x'.repeat(201) }).success).toBe(false)
  })

  it('rejects a reason longer than 500 characters', () => {
    expect(createDisputeSchema.safeParse({ ...valid, reason: 'x'.repeat(501) }).success).toBe(false)
  })

  it('rejects an empty merchant name', () => {
    expect(createDisputeSchema.safeParse({ ...valid, merchantName: '   ' }).success).toBe(false)
  })
})
