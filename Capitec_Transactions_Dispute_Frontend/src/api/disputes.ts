import type { Dispute, DisputeStatus } from '../domain/dispute'
import type { DisputeStatusChange } from '../domain/disputeHistory'
import { apiFetch } from './client'

export type CreateDisputePayload = {
  transactionId: string
  reason: string
}

export function listDisputes(status?: DisputeStatus): Promise<Dispute[]> {
  const query = status ? `?status=${status}` : ''

  return apiFetch<Dispute[]>(`/disputes${query}`)
}

export function getDispute(id: string): Promise<Dispute> {
  return apiFetch<Dispute>(`/disputes/${id}`)
}

export function getDisputeHistory(id: string): Promise<DisputeStatusChange[]> {
  return apiFetch<DisputeStatusChange[]>(`/disputes/${id}/history`)
}

export function createDispute(payload: CreateDisputePayload): Promise<Dispute> {
  return apiFetch<Dispute>('/disputes', {
    method: 'POST',
    body: JSON.stringify(payload),
  })
}

export function advanceDisputeStatus(id: string, status: DisputeStatus): Promise<Dispute> {
  return apiFetch<Dispute>(`/disputes/${id}/status`, {
    method: 'PATCH',
    body: JSON.stringify({ status }),
  })
}
