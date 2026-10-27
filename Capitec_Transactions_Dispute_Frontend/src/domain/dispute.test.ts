import { describe, expect, it } from 'vitest'
import { formatAmount, nextStatus, STATUS_LABELS } from './dispute'

describe('nextStatus', () => {
  it('advances Initiated to UnderInvestigation', () => {
    expect(nextStatus('Initiated')).toBe('UnderInvestigation')
  })

  it('advances UnderInvestigation to Resolved', () => {
    expect(nextStatus('UnderInvestigation')).toBe('Resolved')
  })

  it('offers nothing after Resolved', () => {
    expect(nextStatus('Resolved')).toBeNull()
  })
})

describe('STATUS_LABELS', () => {
  it('spells UnderInvestigation as readable text', () => {
    expect(STATUS_LABELS.UnderInvestigation).toBe('Under investigation')
  })
})

describe('formatAmount', () => {
  it('formats a ZAR amount with two decimals', () => {
    expect(formatAmount(1499.99, 'ZAR')).toMatch(/1\s?499[,.]99/)
  })
})
