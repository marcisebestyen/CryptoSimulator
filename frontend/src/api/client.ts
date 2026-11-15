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

function errorText(body: string): string {
  try {
    const parsed: unknown = JSON.parse(body)
    if (typeof parsed === 'object' && parsed !== null && 'errors' in parsed) {
      const { errors } = parsed
      if (Array.isArray(errors) && errors.every((item) => typeof item === 'string')) {
        return errors.join(' ')
      }
      if (typeof errors === 'object' && errors !== null && !Array.isArray(errors)) {
        const groups = Object.values(errors)
        const isStringArray = (value: unknown): value is string[] =>
          Array.isArray(value) && value.every((item) => typeof item === 'string')
        if (groups.length > 0 && groups.every(isStringArray)) {
          return groups.flat().join(' ')
        }
      }
    }
  } catch {
    return body
  }
  return body
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
    throw new ApiError(response.status, errorText(await response.text()))
  }

  if (response.status === 204) {
    return undefined as T
  }
  return (await response.json()) as T
}
