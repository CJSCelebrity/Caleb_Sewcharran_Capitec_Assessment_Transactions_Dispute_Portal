import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import type { CreateDisputePayload } from '../domain/createDisputeSchema'
import type { DisputeStatus } from '../domain/dispute'
import {
  advanceDisputeStatus,
  createDispute,
  getDispute,
  listDisputes,
} from '../api/disputes'

export type StatusFilter = DisputeStatus | 'All'

export const disputeKeys = {
  lists: ['disputes', 'list'] as const,
  list: (filter: StatusFilter) => ['disputes', 'list', filter] as const,
  detail: (id: string) => ['disputes', 'detail', id] as const,
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

export function useCreateDisputeMutation() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (payload: CreateDisputePayload) => createDispute(payload),
    onSuccess: (dispute) => {
      queryClient.setQueryData(disputeKeys.detail(dispute.id), dispute)
      void queryClient.invalidateQueries({ queryKey: disputeKeys.lists })
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
    },
  })
}
