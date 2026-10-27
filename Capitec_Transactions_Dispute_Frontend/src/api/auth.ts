import { apiFetch } from './client'

export type Role = 'ReadOnly' | 'DisputeAdmin'

export type LoginResponse = {
  accessToken: string
  expiresAtUtc: string
  email: string
  role: Role
}

export function login(email: string, password: string): Promise<LoginResponse> {
  return apiFetch<LoginResponse>('/auth/login', {
    method: 'POST',
    body: JSON.stringify({ email, password }),
  })
}
