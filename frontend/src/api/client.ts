import { getApiUrl } from '../config'

export class ApiError extends Error {
  readonly status: number

  constructor(status: number, message: string) {
    super(message)
    this.status = status
  }
}

export type UnauthorizedHandler = () => void

let unauthorizedHandler: UnauthorizedHandler | null = null
let tokenGetter: () => string | null = () => null

export function setUnauthorizedHandler(handler: UnauthorizedHandler | null): void {
  unauthorizedHandler = handler
}

export function setTokenGetter(getter: () => string | null): void {
  tokenGetter = getter
}

export async function apiFetch<T>(
  path: string,
  init?: RequestInit & { skipUnauthorizedHandler?: boolean },
): Promise<T> {
  const { skipUnauthorizedHandler, ...requestInit } = init ?? {}
  const headers = new Headers(requestInit.headers)
  const token = tokenGetter()
  if (token) {
    headers.set('Authorization', `Bearer ${token}`)
  }

  const response = await fetch(`${getApiUrl()}${path}`, { ...requestInit, headers })

  if (!response.ok) {
    if (response.status === 401 && !skipUnauthorizedHandler) {
      unauthorizedHandler?.()
    }
    throw new ApiError(response.status, await response.text())
  }

  if (response.status === 204) {
    return undefined as T
  }
  return (await response.json()) as T
}
