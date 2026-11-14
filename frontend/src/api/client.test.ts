import { http, HttpResponse } from 'msw'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { server } from '../test/server'
import { ApiError, apiFetch, setTokenGetter, setUnauthorizedHandler } from './client'

const url = 'http://localhost:3000/api/ping'

describe('apiFetch', () => {
  afterEach(() => {
    setTokenGetter(() => null)
    setUnauthorizedHandler(null)
  })

  it('sends bearer header when a token exists', async () => {
    let auth: string | null = null
    server.use(
      http.get(url, ({ request }) => {
        auth = request.headers.get('Authorization')
        return HttpResponse.json({ ok: true })
      }),
    )
    setTokenGetter(() => 'jwt')

    await apiFetch('/api/ping')

    expect(auth).toBe('Bearer jwt')
  })

  it('omits the authorization header when there is no token', async () => {
    let auth: string | null = 'unset'
    server.use(
      http.get(url, ({ request }) => {
        auth = request.headers.get('Authorization')
        return HttpResponse.json({ ok: true })
      }),
    )

    await apiFetch('/api/ping')

    expect(auth).toBeNull()
  })

  it('calls the unauthorized handler and throws ApiError on 401', async () => {
    server.use(http.get(url, () => new HttpResponse('nope', { status: 401 })))
    const handler = vi.fn()
    setUnauthorizedHandler(handler)

    const error = await apiFetch('/api/ping').catch((e: unknown) => e)

    expect(handler).toHaveBeenCalledOnce()
    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ status: 401, message: 'nope' })
  })

  it('skips the unauthorized handler when asked', async () => {
    server.use(http.get(url, () => new HttpResponse('nope', { status: 401 })))
    const handler = vi.fn()
    setUnauthorizedHandler(handler)

    await expect(apiFetch('/api/ping', { skipUnauthorizedHandler: true })).rejects.toBeInstanceOf(ApiError)

    expect(handler).not.toHaveBeenCalled()
  })

  it('throws ApiError without calling the handler on 500', async () => {
    server.use(http.get(url, () => new HttpResponse('boom', { status: 500 })))
    const handler = vi.fn()
    setUnauthorizedHandler(handler)

    const error = await apiFetch('/api/ping').catch((e: unknown) => e)

    expect(error).toMatchObject({ status: 500, message: 'boom' })
    expect(handler).not.toHaveBeenCalled()
  })

  it('returns undefined on 204', async () => {
    server.use(http.get(url, () => new HttpResponse(null, { status: 204 })))

    await expect(apiFetch('/api/ping')).resolves.toBeUndefined()
  })

  it('rejects when the network fails', async () => {
    server.use(http.get(url, () => HttpResponse.error()))

    await expect(apiFetch('/api/ping')).rejects.not.toBeInstanceOf(ApiError)
  })
})
