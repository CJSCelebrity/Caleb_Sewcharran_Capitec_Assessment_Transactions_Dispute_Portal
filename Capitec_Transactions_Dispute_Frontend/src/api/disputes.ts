import type { CreateDisputePayload } from '../domain/createDisputeSchema'
import type { Dispute, DisputeStatus } from '../domain/dispute'
import { apiFetch } from './client'

export function listDisputes(status?: DisputeStatus): Promise<Dispute[]> {
  const query = status ? `?status=${status}` : ''

  return apiFetch<Dispute[]>(`/disputes${query}`)
}

export function getDispute(id: string): Promise<Dispute> {
  return apiFetch<Dispute>(`/disputes/${id}`)
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
