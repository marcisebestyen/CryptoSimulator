import { http, HttpResponse } from 'msw'
import { describe, expect, it } from 'vitest'
import { server } from '../test/server'
import { ApiError, login } from './login'

const loginUrl = 'http://localhost:3000/api/users/login'

describe('login', () => {
  it('returns token and user when credentials are valid', async () => {
    server.use(http.post(loginUrl, () => HttpResponse.json({ token: 'jwt', user: { email: 'a@b.co' } })))

    const result = await login('a@b.co', 'secret')

    expect(result).toEqual({ token: 'jwt', user: { email: 'a@b.co' } })
  })

  it('sends email and password as json body', async () => {
    let body: unknown
    server.use(
      http.post(loginUrl, async ({ request }) => {
        body = await request.json()
        return HttpResponse.json({ token: 'jwt', user: { email: 'a@b.co' } })
      }),
    )

    await login('a@b.co', 'secret')

    expect(body).toEqual({ email: 'a@b.co', password: 'secret' })
  })

  it('throws ApiError with status and message when backend answers 401', async () => {
    server.use(http.post(loginUrl, () => new HttpResponse('Invalid email or password.', { status: 401 })))

    const error = await login('a@b.co', 'bad').catch((e: unknown) => e)

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ status: 401, message: 'Invalid email or password.' })
  })

  it('rejects when backend is unreachable', async () => {
    server.use(http.post(loginUrl, () => HttpResponse.error()))

    await expect(login('a@b.co', 'secret')).rejects.not.toBeInstanceOf(ApiError)
  })
})
