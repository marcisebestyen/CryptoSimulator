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

  it('joins an errors array from a JSON error body into the message', async () => {
    server.use(
      http.get(url, () => HttpResponse.json({ errors: ['Email is taken.', 'Password too short.'] }, { status: 400 })),
    )

    await expect(apiFetch('/api/ping')).rejects.toMatchObject({
      status: 400,
      message: 'Email is taken. Password too short.',
    })
  })

  it('flattens an ASP.NET validation errors object into the message', async () => {
    server.use(
      http.get(url, () =>
        HttpResponse.json(
          { errors: { Username: ['The Username field is required.'], Email: ['Bad email.', 'Too long.'] } },
          { status: 400 },
        ),
      ),
    )

    await expect(apiFetch('/api/ping')).rejects.toMatchObject({
      status: 400,
      message: 'The Username field is required. Bad email. Too long.',
    })
  })

  it.each([
    ['an object with a non-array value', { errors: { Email: 'bad' } }],
    ['an object with a non-string item', { errors: { Email: ['bad', 2] } }],
    ['an array with a non-string', { errors: ['bad', 2] }],
  ])('falls back to the raw body when errors is %s', async (_name, body) => {
    server.use(http.get(url, () => HttpResponse.json(body, { status: 400 })))

    await expect(apiFetch('/api/ping')).rejects.toMatchObject({
      status: 400,
      message: JSON.stringify(body),
    })
  })
})
