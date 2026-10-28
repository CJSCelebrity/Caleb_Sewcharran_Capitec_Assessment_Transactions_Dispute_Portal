import { z } from 'zod'

export const createDisputeSchema = z.object({
  reason: z
    .string()
    .trim()
    .min(1, 'Enter a reason.')
    .max(500, 'Reason must be 500 characters or fewer.'),
})

export type CreateDisputeForm = z.input<typeof createDisputeSchema>

export type CreateDisputeValues = z.output<typeof createDisputeSchema>
