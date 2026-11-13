// Shared HTTP helper. Vite forwards /api to the backend (see vite.config.ts).
import { getToken } from './session'

const BASE = '/api'

/** Error thrown for any non-2xx response, carrying the HTTP status. */
export class ApiError extends Error {
  readonly status: number

  constructor(message: string, status: number) {
    super(message)
    this.name = 'ApiError'
    this.status = status
  }
}

type Method = 'GET' | 'POST' | 'PUT' | 'DELETE'

/** Sends a JSON request, adding the signed-in user's token when there is one. */
async function request<TResponse>(method: Method, path: string, body?: unknown): Promise<TResponse> {
  const headers: Record<string, string> = {}
  if (body !== undefined) headers['Content-Type'] = 'application/json'
  const token = getToken()
  if (token) headers.Authorization = `Bearer ${token}`

  const res = await fetch(`${BASE}${path}`, {
    method,
    headers,
    body: body === undefined ? undefined : JSON.stringify(body),
  })
  const data: unknown = await res.json().catch(() => null)
  if (!res.ok) {
    const message =
      typeof data === 'object' && data !== null && 'message' in data && typeof data.message === 'string'
        ? data.message
        : `Request failed (${res.status})`
    throw new ApiError(message, res.status)
  }
  return data as TResponse
}

export function getJson<TResponse>(path: string): Promise<TResponse> {
  return request<TResponse>('GET', path)
}

export function postJson<TResponse, TBody = unknown>(path: string, body?: TBody): Promise<TResponse> {
  return request<TResponse>('POST', path, body)
}

export function putJson<TResponse, TBody = unknown>(path: string, body: TBody): Promise<TResponse> {
  return request<TResponse>('PUT', path, body)
}

export function deleteJson<TResponse>(path: string): Promise<TResponse> {
  return request<TResponse>('DELETE', path)
}
