import { http, HttpResponse } from 'msw'
import { afterEach, beforeEach, describe, expect, it } from 'vitest'
import { server } from '../test/server'
import { ApiError, setTokenGetter } from './client'
import { fetchUser, forgotPassword, register, updateUser } from './users'

const base = 'http://localhost:3000/api/users'

const user = {
  id: 7,
  role: 'User',
  username: 'alice',
  firstName: 'Alice',
  lastName: 'Smith',
  email: 'alice@example.com',
}

beforeEach(() => {
  setTokenGetter(() => 'test-token')
})

afterEach(() => {
  setTokenGetter(() => null)
})

describe('fetchUser', () => {
  it('gets the user by id', async () => {
    server.use(http.get(`${base}/7`, () => HttpResponse.json(user)))

    await expect(fetchUser(7)).resolves.toEqual(user)
  })
})

describe('updateUser', () => {
  it('puts the body and returns the updated user', async () => {
    const body = { username: 'alice', firstName: 'Ally', lastName: 'Jones' }
    let received: unknown
    let method = ''
    server.use(
      http.put(`${base}/7`, async ({ request }) => {
        method = request.method
        received = await request.json()
        return HttpResponse.json({ ...user, ...body })
      }),
    )

    await expect(updateUser(7, body)).resolves.toEqual({ ...user, ...body })
    expect(method).toBe('PUT')
    expect(received).toEqual(body)
  })
})

describe('register', () => {
  it('posts the credentials and returns the body', async () => {
    const response = { user, recoveryCode: 'ABCD-1234' }
    let received: unknown
    server.use(
      http.post(`${base}/register`, async ({ request }) => {
        received = await request.json()
        return HttpResponse.json(response, { status: 201 })
      }),
    )

    await expect(register('alice', 'alice@example.com', 'secret')).resolves.toEqual(response)
    expect(received).toEqual({ username: 'alice', email: 'alice@example.com', password: 'secret' })
  })

  it('rejects with ApiError on 400', async () => {
    server.use(http.post(`${base}/register`, () => new HttpResponse('bad', { status: 400 })))

    await expect(register('a', 'b', 'c')).rejects.toBeInstanceOf(ApiError)
  })
})

describe('forgotPassword', () => {
  it('posts the reset data and returns the new code', async () => {
    let received: unknown
    server.use(
      http.post(`${base}/forgot-password`, async ({ request }) => {
        received = await request.json()
        return HttpResponse.json({ recoveryCode: 'NEW-CODE' })
      }),
    )

    await expect(forgotPassword('a@b.co', 'OLD', 'newpass')).resolves.toEqual({ recoveryCode: 'NEW-CODE' })
    expect(received).toEqual({ email: 'a@b.co', recoveryCode: 'OLD', newPassword: 'newpass' })
  })

  it('rejects with ApiError on 400', async () => {
    server.use(http.post(`${base}/forgot-password`, () => new HttpResponse('Invalid code.', { status: 400 })))

    await expect(forgotPassword('a', 'b', 'c')).rejects.toMatchObject({ status: 400, message: 'Invalid code.' })
  })
})
