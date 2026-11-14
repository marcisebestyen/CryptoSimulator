import type { MRT_PaginationState, MRT_Updater } from 'mantine-react-table'
import { useCallback, useState } from 'react'

type PersistedState = { pageIndex: number; pageSize: number; globalFilter: string }

const DEFAULT_STATE: PersistedState = { pageIndex: 0, pageSize: 10, globalFilter: '' }

function isPersistedState(value: unknown): value is PersistedState {
  if (typeof value !== 'object' || value === null) return false
  const { pageIndex, pageSize, globalFilter } = value as Record<string, unknown>
  return (
    Number.isInteger(pageIndex) &&
    (pageIndex as number) >= 0 &&
    Number.isInteger(pageSize) &&
    (pageSize as number) > 0 &&
    typeof globalFilter === 'string'
  )
}

function readState(key: string): PersistedState {
  try {
    const raw = sessionStorage.getItem(key)
    if (raw === null) return DEFAULT_STATE
    const parsed: unknown = JSON.parse(raw)
    return isPersistedState(parsed) ? parsed : DEFAULT_STATE
  } catch {
    // Storage blocked or bad JSON: start fresh instead of breaking the page.
    return DEFAULT_STATE
  }
}

function writeState(key: string, state: PersistedState): void {
  try {
    sessionStorage.setItem(key, JSON.stringify(state))
  } catch {
    // Storage blocked or full: the table still works, it just is not remembered.
  }
}

function resolve<T>(updater: MRT_Updater<T>, old: T): T {
  return typeof updater === 'function' ? (updater as (old: T) => T)(old) : updater
}

export function usePersistedTableState(key: string) {
  const [state, setState] = useState<PersistedState>(() => readState(key))

  const update = useCallback(
    (change: (old: PersistedState) => PersistedState) => {
      setState((old) => {
        const next = change(old)
        writeState(key, next)
        return next
      })
    },
    [key],
  )

  const onPaginationChange = useCallback(
    (updater: MRT_Updater<MRT_PaginationState>) =>
      update((old) => {
        const { pageIndex, pageSize } = resolve(updater, { pageIndex: old.pageIndex, pageSize: old.pageSize })
        return { ...old, pageIndex, pageSize }
      }),
    [update],
  )

  const onGlobalFilterChange = useCallback(
    (updater: MRT_Updater<string | undefined>) =>
      update((old) => ({ ...old, globalFilter: resolve(updater, old.globalFilter) ?? '' })),
    [update],
  )

  return {
    pagination: { pageIndex: state.pageIndex, pageSize: state.pageSize },
    globalFilter: state.globalFilter,
    onPaginationChange,
    onGlobalFilterChange,
  }
}
