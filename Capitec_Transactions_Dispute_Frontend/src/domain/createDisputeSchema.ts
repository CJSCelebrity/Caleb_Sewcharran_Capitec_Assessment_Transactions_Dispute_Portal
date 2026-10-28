import { z } from 'zod'

export const createDisputeSchema = z.object({
  transactionAmount: z
    .number('Enter an amount.')
    .min(0.01, 'Amount must be at least 0.01.')
    .max(1_000_000_000, 'Amount must not exceed 1,000,000,000.')
    .refine(
      (value) => Math.abs(value * 100 - Math.round(value * 100)) < 1e-6,
      'Amount supports at most 2 decimal places.',
    ),
  transactionCurrency: z
    .string()
    .trim()
    .regex(/^[A-Za-z]{3}$/, 'Currency must be exactly 3 letters.')
    .transform((value) => value.toUpperCase()),
  transactionDate: z
    .string()
    .min(1, 'Enter the transaction date and time.')
    .refine((value) => !Number.isNaN(Date.parse(value)), 'Enter a valid date and time.')
    .transform((value) => new Date(value).toISOString()),
  merchantName: z
    .string()
    .trim()
    .min(1, 'Enter the merchant name.')
    .max(200, 'Merchant name must be 200 characters or fewer.'),
  reason: z
    .string()
    .trim()
    .min(1, 'Enter a reason.')
    .max(500, 'Reason must be 500 characters or fewer.'),
})

export type CreateDisputeForm = z.input<typeof createDisputeSchema>

export type CreateDisputeValues = z.output<typeof createDisputeSchema>

export type CreateDisputePayload = CreateDisputeValues & { transactionId: string }
