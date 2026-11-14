import { afterEach, describe, expect, it, vi } from 'vitest'
import { getApiUrl } from './config'

describe('getApiUrl', () => {
  afterEach(() => {
    vi.unstubAllEnvs()
  })

  it('returns the url without trailing slash', () => {
    vi.stubEnv('VITE_API_URL', 'http://x/')
    expect(getApiUrl()).toBe('http://x')
  })

  it('returns the url unchanged when it has no trailing slash', () => {
    vi.stubEnv('VITE_API_URL', 'http://x')
    expect(getApiUrl()).toBe('http://x')
  })

  it('throws when the variable is not set', () => {
    vi.stubEnv('VITE_API_URL', '')
    expect(() => getApiUrl()).toThrow('VITE_API_URL is not set')
  })
})
