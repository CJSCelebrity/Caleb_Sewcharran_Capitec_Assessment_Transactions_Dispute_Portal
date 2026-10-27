export type ProblemDetails = {
  type?: string
  title?: string
  status?: number
  detail?: string
  errors?: Record<string, string[]>
}

export class ApiError extends Error {
  readonly status: number
  readonly problem: ProblemDetails | null

  constructor(status: number, problem: ProblemDetails | null) {
    super(problem?.detail ?? problem?.title ?? `Request failed with status ${status}`)
    this.name = 'ApiError'
    this.status = status
    this.problem = problem
  }

  get fieldErrors(): Record<string, string> {
    const errors = this.problem?.errors

    if (!errors) {
      return {}
    }

    return Object.fromEntries(
      Object.entries(errors).map(([key, messages]) => [
        key.charAt(0).toLowerCase() + key.slice(1),
        messages[0],
      ]),
    )
  }
}

const baseUrl = import.meta.env.VITE_API_BASE_URL ?? '/api'

let readToken: () => string | null = () => null
let onUnauthorized: () => void = () => {}

export function setTokenReader(reader: () => string | null) {
  readToken = reader
}

export function setUnauthorizedHandler(handler: () => void) {
  onUnauthorized = handler
}

export async function apiFetch<T>(path: string, init?: RequestInit): Promise<T> {
  const token = readToken()

  const response = await fetch(`${baseUrl}${path}`, {
    ...init,
    headers: {
      ...(init?.body ? { 'Content-Type': 'application/json' } : {}),
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...init?.headers,
    },
  })

  if (!response.ok) {
    if (response.status === 401) {
      onUnauthorized()
    }

    throw new ApiError(response.status, await readProblem(response))
  }

  if (response.status === 204) {
    return undefined as T
  }

  return (await response.json()) as T
}

async function readProblem(response: Response): Promise<ProblemDetails | null> {
  if (!(response.headers.get('content-type') ?? '').includes('json')) {
    return null
  }

  try {
    return (await response.json()) as ProblemDetails
  } catch {
    return null
  }
}
