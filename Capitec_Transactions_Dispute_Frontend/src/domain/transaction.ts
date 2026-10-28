import type { DisputeStatus } from './dispute'

export type Transaction = {
  id: string
  reference: string
  amount: number
  currency: string
  occurredAt: string
  merchantName: string
  disputeId: string | null
  disputeStatus: DisputeStatus | null
}
