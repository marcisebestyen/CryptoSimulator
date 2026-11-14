import { act, renderHook } from '@testing-library/react'
import { beforeEach, describe, expect, it } from 'vitest'
import { usePersistedTableState } from './usePersistedTableState'

const KEY = 'test-table'
const defaults = { pagination: { pageIndex: 0, pageSize: 10 }, globalFilter: '' }

describe('usePersistedTableState', () => {
  beforeEach(() => {
    sessionStorage.clear()
  })

  it('returns defaults when nothing is stored', () => {
    const { result } = renderHook(() => usePersistedTableState(KEY))

    expect(result.current.pagination).toEqual(defaults.pagination)
    expect(result.current.globalFilter).toBe('')
  })

  it('persists changes and restores them in a new hook instance', () => {
    const first = renderHook(() => usePersistedTableState(KEY))
    act(() => first.result.current.onPaginationChange({ pageIndex: 2, pageSize: 25 }))
    act(() => first.result.current.onGlobalFilterChange('eth'))
    first.unmount()

    const second = renderHook(() => usePersistedTableState(KEY))

    expect(second.result.current.pagination).toEqual({ pageIndex: 2, pageSize: 25 })
    expect(second.result.current.globalFilter).toBe('eth')
  })

  it('accepts updater functions like setState', () => {
    const { result } = renderHook(() => usePersistedTableState(KEY))

    act(() => result.current.onPaginationChange((old) => ({ ...old, pageIndex: old.pageIndex + 1 })))
    act(() => result.current.onGlobalFilterChange((old) => `${old ?? ''}x`))

    expect(result.current.pagination.pageIndex).toBe(1)
    expect(result.current.globalFilter).toBe('x')
  })

  it('falls back to defaults for malformed JSON', () => {
    sessionStorage.setItem(KEY, '{not json')

    const { result } = renderHook(() => usePersistedTableState(KEY))

    expect(result.current.pagination).toEqual(defaults.pagination)
    expect(result.current.globalFilter).toBe('')
  })

  it('falls back to defaults for wrong shape', () => {
    sessionStorage.setItem(KEY, JSON.stringify({ pageIndex: 'one', pageSize: 10, globalFilter: 5 }))

    const { result } = renderHook(() => usePersistedTableState(KEY))

    expect(result.current.pagination).toEqual(defaults.pagination)
    expect(result.current.globalFilter).toBe('')
  })
})
