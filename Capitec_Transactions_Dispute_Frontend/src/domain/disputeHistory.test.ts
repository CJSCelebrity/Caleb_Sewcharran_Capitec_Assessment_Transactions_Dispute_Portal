import { describe, expect, it } from 'vitest'
import { historyEntryLabel } from './disputeHistory'

describe('historyEntryLabel', () => {
  it('reads as Raised for the first entry', () => {
    expect(
      historyEntryLabel({
        fromStatus: null,
        toStatus: 'Initiated',
        changedBy: 'thandi@example.test',
        changedAt: '2026-10-21T08:00:00Z',
      }),
    ).toBe('Raised')
  })

  it('names the status a later entry moved to', () => {
    expect(
      historyEntryLabel({
        fromStatus: 'Initiated',
        toStatus: 'UnderInvestigation',
        changedBy: 'admin@capitec.test',
        changedAt: '2026-10-22T09:00:00Z',
      }),
    ).toBe('Moved to under investigation')
  })

  it('names a move to resolved', () => {
    expect(
      historyEntryLabel({
        fromStatus: 'UnderInvestigation',
        toStatus: 'Resolved',
        changedBy: 'admin@capitec.test',
        changedAt: '2026-10-23T09:00:00Z',
      }),
    ).toBe('Moved to resolved')
  })
})
