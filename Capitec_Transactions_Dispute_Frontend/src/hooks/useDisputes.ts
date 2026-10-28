import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import type { CreateDisputePayload } from '../api/disputes'
import type { DisputeStatus } from '../domain/dispute'
import {
  advanceDisputeStatus,
  createDispute,
  getDispute,
  getDisputeHistory,
  listDisputes,
} from '../api/disputes'
import { transactionKeys } from './useTransactions'

export type StatusFilter = DisputeStatus | 'All'

export const disputeKeys = {
  lists: ['disputes', 'list'] as const,
  list: (filter: StatusFilter) => ['disputes', 'list', filter] as const,
  detail: (id: string) => ['disputes', 'detail', id] as const,
  history: (id: string) => ['disputes', 'history', id] as const,
}

export function useDisputesQuery(filter: StatusFilter) {
  return useQuery({
    queryKey: disputeKeys.list(filter),
    queryFn: () => listDisputes(filter === 'All' ? undefined : filter),
  })
}

export function useDisputeQuery(id: string) {
  return useQuery({
    queryKey: disputeKeys.detail(id),
    queryFn: () => getDispute(id),
  })
}

export function useDisputeHistoryQuery(id: string) {
  return useQuery({
    queryKey: disputeKeys.history(id),
    queryFn: () => getDisputeHistory(id),
  })
}

export function useCreateDisputeMutation() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (payload: CreateDisputePayload) => createDispute(payload),
    onSuccess: (dispute) => {
      queryClient.setQueryData(disputeKeys.detail(dispute.id), dispute)
      void queryClient.invalidateQueries({ queryKey: disputeKeys.lists })
      void queryClient.invalidateQueries({ queryKey: transactionKeys.lists })
    },
  })
}

export function useAdvanceStatusMutation(id: string) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (status: DisputeStatus) => advanceDisputeStatus(id, status),
    onSuccess: (dispute) => {
      queryClient.setQueryData(disputeKeys.detail(id), dispute)
      void queryClient.invalidateQueries({ queryKey: disputeKeys.lists })
      void queryClient.invalidateQueries({ queryKey: disputeKeys.history(id) })
      void queryClient.invalidateQueries({ queryKey: transactionKeys.lists })
    },
  })
}
