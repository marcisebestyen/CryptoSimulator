import { http, HttpResponse } from 'msw'
import { afterEach, beforeEach, describe, expect, it } from 'vitest'
import { server } from '../test/server'
import { setTokenGetter } from './client'
import { fetchUser, updateUser } from './users'

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
