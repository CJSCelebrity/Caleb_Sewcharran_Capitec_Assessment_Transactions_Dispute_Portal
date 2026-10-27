export const DISPUTE_STATUSES = ['Initiated', 'UnderInvestigation', 'Resolved'] as const

export type DisputeStatus = (typeof DISPUTE_STATUSES)[number]

export type Dispute = {
  id: string
  transactionId: string
  transactionAmount: number
  transactionCurrency: string
  transactionDate: string
  merchantName: string
  reason: string
  status: DisputeStatus
  createdAt: string
  resolvedAt: string | null
}

export const STATUS_LABELS: Record<DisputeStatus, string> = {
  Initiated: 'Initiated',
  UnderInvestigation: 'Under investigation',
  Resolved: 'Resolved',
}

export const STATUS_CLASS: Record<DisputeStatus, string> = {
  Initiated: 'badge-initiated',
  UnderInvestigation: 'badge-under-investigation',
  Resolved: 'badge-resolved',
}

export function nextStatus(status: DisputeStatus): DisputeStatus | null {
  switch (status) {
    case 'Initiated':
      return 'UnderInvestigation'
    case 'UnderInvestigation':
      return 'Resolved'
    case 'Resolved':
      return null
  }
}

export function formatAmount(amount: number, currency: string): string {
  return new Intl.NumberFormat('en-ZA', {
    style: 'currency',
    currency,
    minimumFractionDigits: 2,
  }).format(amount)
}

export function formatDateTime(iso: string): string {
  return new Intl.DateTimeFormat('en-ZA', {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(new Date(iso))
}
