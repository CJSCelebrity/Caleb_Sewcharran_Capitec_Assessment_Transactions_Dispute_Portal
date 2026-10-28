import { describe, expect, it } from 'vitest'
import { createDisputeSchema } from './createDisputeSchema'

describe('createDisputeSchema', () => {
  it('accepts a reason and trims it', () => {
    const result = createDisputeSchema.safeParse({ reason: '  Charged twice.  ' })

    expect(result.success).toBe(true)
    expect(result.success && result.data.reason).toBe('Charged twice.')
  })

  it('rejects an empty reason', () => {
    const result = createDisputeSchema.safeParse({ reason: '' })

    expect(result.success).toBe(false)
    expect(result.success === false && result.error.issues[0].message).toBe('Enter a reason.')
  })

  it('rejects a whitespace only reason', () => {
    expect(createDisputeSchema.safeParse({ reason: '   ' }).success).toBe(false)
  })

  it('rejects a reason longer than 500 characters', () => {
    const result = createDisputeSchema.safeParse({ reason: 'a'.repeat(501) })

    expect(result.success).toBe(false)
    expect(result.success === false && result.error.issues[0].message).toBe(
      'Reason must be 500 characters or fewer.',
    )
  })

  it('accepts a reason of exactly 500 characters', () => {
    expect(createDisputeSchema.safeParse({ reason: 'a'.repeat(500) }).success).toBe(true)
  })

  it('describes only the reason, so no transaction field can be entered', () => {
    expect(Object.keys(createDisputeSchema.shape)).toEqual(['reason'])
  })
})
