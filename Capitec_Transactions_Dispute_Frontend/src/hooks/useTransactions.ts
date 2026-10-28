import { useQuery } from '@tanstack/react-query'
import { listTransactions } from '../api/transactions'

export const transactionKeys = {
  lists: ['transactions', 'list'] as const,
  list: () => ['transactions', 'list'] as const,
}

export function useTransactionsQuery() {
  return useQuery({
    queryKey: transactionKeys.list(),
    queryFn: () => listTransactions(),
  })
}
