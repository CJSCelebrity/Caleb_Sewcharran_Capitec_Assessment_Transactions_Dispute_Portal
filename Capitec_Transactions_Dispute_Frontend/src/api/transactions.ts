import type { Transaction } from '../domain/transaction'
import { apiFetch } from './client'

export function listTransactions(): Promise<Transaction[]> {
  return apiFetch<Transaction[]>('/transactions')
}
