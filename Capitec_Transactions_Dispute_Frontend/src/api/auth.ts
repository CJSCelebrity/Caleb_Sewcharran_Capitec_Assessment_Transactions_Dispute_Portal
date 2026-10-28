import { apiFetch } from './client'

export type Role = 'Customer' | 'ReadOnly' | 'DisputeAdmin'

export type LoginResponse = {
  accessToken: string
  expiresAtUtc: string
  email: string
  role: Role
  customerId: string | null
}

export function login(email: string, password: string): Promise<LoginResponse> {
  return apiFetch<LoginResponse>('/auth/login', {
    method: 'POST',
    body: JSON.stringify({ email, password }),
  })
}
