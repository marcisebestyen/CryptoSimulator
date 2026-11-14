import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw'
import { act } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { PRICE_REFRESH_MS } from '../api/polling'
import { endpoints } from '../api/endpoints'
import { TOKEN_KEY } from '../auth/token'
import { makeJwt } from '../test/jwt'
import { loginAsValidUser, renderApp } from '../test/renderApp'
import { server } from '../test/server'

const url = `http://localhost:3000${endpoints.cryptos}`

describe('CryptosPage', () => {
  beforeEach(() => {
    localStorage.clear()
    sessionStorage.clear()
    loginAsValidUser()
  })

  it('renders rows with a formatted exchange rate', async () => {
    server.use(http.get(url, () => HttpResponse.json([{ id: 1, name: 'Bitcoin', currentExchangeRate: 43250.5 }])))
    renderApp('/cryptos')

    expect(await screen.findByText('Bitcoin')).toBeInTheDocument()
    expect(screen.getByText((43250.5).toLocaleString(undefined, { maximumFractionDigits: 8 }))).toBeInTheDocument()
  })

  describe('polling', () => {
    beforeEach(() => {
      vi.useFakeTimers({ shouldAdvanceTime: true })
    })
    afterEach(() => {
      vi.useRealTimers()
    })

    it('refreshes rates after the refresh interval without user action', async () => {
      let calls = 0
      server.use(
        http.get(url, () => {
          calls += 1
          return HttpResponse.json([{ id: 1, name: 'Bitcoin', currentExchangeRate: calls === 1 ? 100 : 250 }])
        }),
      )
      renderApp('/cryptos')
      expect(await screen.findByText('100')).toBeInTheDocument()

      await act(() => vi.advanceTimersByTimeAsync(PRICE_REFRESH_MS))

      expect(await screen.findByText('250')).toBeInTheDocument()
    })
  })

  it('does not show an Id column', async () => {
    server.use(http.get(url, () => HttpResponse.json([{ id: 1, name: 'Bitcoin', currentExchangeRate: 1 }])))
    renderApp('/cryptos')
    await screen.findByText('Bitcoin')

    expect(screen.queryByRole('columnheader', { name: /^Id/ })).not.toBeInTheDocument()
  })

  describe('search', () => {
    const list = [
      { id: 1, name: 'Bitcoin', currentExchangeRate: 100 },
      { id: 2, name: 'Ether', currentExchangeRate: 5 },
    ]

    it('filters rows by name using the search box', async () => {
      server.use(http.get(url, () => HttpResponse.json(list)))
      renderApp('/cryptos')
      await screen.findByText('Bitcoin')

      await userEvent.setup().type(screen.getByRole('textbox', { name: 'Search' }), 'eth')

      await waitFor(() => expect(screen.queryByText('Bitcoin')).not.toBeInTheDocument())
      expect(screen.getByText('Ether')).toBeInTheDocument()
    })

    it('shows the empty fallback when search matches nothing', async () => {
      server.use(http.get(url, () => HttpResponse.json(list)))
      renderApp('/cryptos')
      await screen.findByText('Bitcoin')

      await userEvent.setup().type(screen.getByRole('textbox', { name: 'Search' }), 'zzz')

      expect(await screen.findByText('No cryptos yet.')).toBeInTheDocument()
    })

    it('price history link is still reachable per row', async () => {
      server.use(http.get(url, () => HttpResponse.json(list)))
      renderApp('/cryptos')

      const links = await screen.findAllByRole('link', { name: 'Price history' })
      expect(links[0]).toHaveAttribute('href', '/cryptos/1/price-history')
    })
  })

  it('shows an empty state for an empty list', async () => {
    server.use(http.get(url, () => HttpResponse.json([])))
    renderApp('/cryptos')

    expect(await screen.findByText('No cryptos yet.')).toBeInTheDocument()
  })

  it('shows an error alert when the server fails and retries on click', async () => {
    server.use(http.get(url, () => new HttpResponse('boom', { status: 500 })))
    renderApp('/cryptos')

    expect(await screen.findByRole('alert')).toBeInTheDocument()

    server.use(http.get(url, () => HttpResponse.json([{ id: 2, name: 'Ether', currentExchangeRate: 1 }])))
    await userEvent.setup().click(screen.getByRole('button', { name: 'Retry' }))

    expect(await screen.findByText('Ether')).toBeInTheDocument()
  })

  it('shows a readable error when the server is unreachable', async () => {
    server.use(http.get(url, () => HttpResponse.error()))
    renderApp('/cryptos')

    expect(await screen.findByText('Cannot reach the server.')).toBeInTheDocument()
  })

  it('sends the user to login on 401', async () => {
    server.use(http.get(url, () => new HttpResponse('no', { status: 401 })))
    renderApp('/cryptos')

    expect(await screen.findByRole('heading', { name: 'Welcome back' })).toBeInTheDocument()
  })

  describe('remembered table state', () => {
    const many = Array.from({ length: 15 }, (_, i) => ({
      id: i + 1,
      name: `Coin${String(i + 1).padStart(2, '0')}`,
      currentExchangeRate: i + 1,
    }))

    const historyEmpty = { items: [], totalCount: 0, page: 1, pageSize: 10 }

    beforeEach(() => {
      server.use(
        http.get(`http://localhost:3000${endpoints.crypto(1)}`, () => HttpResponse.json(many[0])),
        http.get(`http://localhost:3000${endpoints.cryptoPriceHistory(1)}`, () => HttpResponse.json(historyEmpty)),
      )
    })

    async function goToPageTwo(user: ReturnType<typeof userEvent.setup>) {
      await screen.findByText('Coin01')
      await user.click(screen.getByRole('button', { name: 'Go to next page' }))
      expect(await screen.findByText('Coin11')).toBeInTheDocument()
    }

    it('keeps the page after opening price history and going back', async () => {
      server.use(http.get(url, () => HttpResponse.json(many)))
      renderApp('/cryptos')
      const user = userEvent.setup()
      await goToPageTwo(user)

      await user.click(screen.getAllByRole('link', { name: 'Price history' })[0])
      await user.click(await screen.findByRole('link', { name: 'Back to cryptos' }))

      expect(await screen.findByText('Coin11')).toBeInTheDocument()
      expect(screen.queryByText('Coin01')).not.toBeInTheDocument()
    })

    it('keeps the page when the list refetches', async () => {
      vi.useFakeTimers({ shouldAdvanceTime: true })
      try {
        let calls = 0
        server.use(
          http.get(url, () => {
            calls += 1
            return HttpResponse.json(many)
          }),
        )
        renderApp('/cryptos')
        const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime })
        await goToPageTwo(user)

        await act(() => vi.advanceTimersByTimeAsync(PRICE_REFRESH_MS))
        await waitFor(() => expect(calls).toBeGreaterThan(1))

        expect(screen.getByText('Coin11')).toBeInTheDocument()
        expect(screen.queryByText('Coin01')).not.toBeInTheDocument()
      } finally {
        vi.useRealTimers()
      }
    })

    it('search text is remembered after going back', async () => {
      server.use(http.get(url, () => HttpResponse.json(many)))
      renderApp('/cryptos')
      const user = userEvent.setup()
      await screen.findByText('Coin01')

      await user.type(screen.getByRole('textbox', { name: 'Search' }), 'Coin07')
      await waitFor(() => expect(screen.queryByText('Coin01')).not.toBeInTheDocument())
      await user.click(screen.getByRole('link', { name: 'Price history' }))
      await user.click(await screen.findByRole('link', { name: 'Back to cryptos' }))

      expect(await screen.findByRole('textbox', { name: 'Search' })).toHaveValue('Coin07')
      expect(screen.getByText('Coin07')).toBeInTheDocument()
      expect(screen.queryByText('Coin01')).not.toBeInTheDocument()
    })

    it('clamps to the last page when the saved page no longer exists', async () => {
      sessionStorage.setItem('cryptos-table', JSON.stringify({ pageIndex: 5, pageSize: 10, globalFilter: '' }))
      server.use(http.get(url, () => HttpResponse.json(many)))
      renderApp('/cryptos')

      expect(await screen.findByText('Coin11')).toBeInTheDocument()
    })
  })

  describe('admin controls', () => {
    const bitcoin = { id: 1, name: 'Bitcoin', currentExchangeRate: 100 }

    function loginAsAdmin() {
      localStorage.setItem(TOKEN_KEY, makeJwt(Math.floor(Date.now() / 1000) + 3600, { role: 'Admin' }))
    }

    function listWith(...cryptos: (typeof bitcoin)[]) {
      server.use(http.get(url, () => HttpResponse.json(cryptos)))
    }

    it('hides Add, Edit and Delete for normal user', async () => {
      listWith(bitcoin)
      renderApp('/cryptos')
      await screen.findByText('Bitcoin')

      expect(screen.queryByRole('button', { name: 'Add' })).not.toBeInTheDocument()
      expect(screen.queryByRole('button', { name: 'Edit Bitcoin' })).not.toBeInTheDocument()
      expect(screen.queryByRole('button', { name: 'Delete Bitcoin' })).not.toBeInTheDocument()
    })

    it('shows Add and per-row Edit and Delete for admin', async () => {
      loginAsAdmin()
      listWith(bitcoin, { id: 2, name: 'Ether', currentExchangeRate: 5 })
      renderApp('/cryptos')
      await screen.findByText('Bitcoin')

      expect(screen.getByRole('button', { name: 'Add' })).toBeInTheDocument()
      expect(screen.getByRole('button', { name: 'Edit Bitcoin' })).toBeInTheDocument()
      expect(screen.getByRole('button', { name: 'Delete Bitcoin' })).toBeInTheDocument()
      expect(screen.getByRole('button', { name: 'Edit Ether' })).toBeInTheDocument()
      expect(screen.getByRole('button', { name: 'Delete Ether' })).toBeInTheDocument()
    })

    it('Add posts name and price and refreshes list', async () => {
      loginAsAdmin()
      let created: unknown = null
      let list = [bitcoin]
      server.use(
        http.get(url, () => HttpResponse.json(list)),
        http.post(url, async ({ request }) => {
          created = await request.json()
          list = [...list, { id: 2, name: 'Ether', currentExchangeRate: 12.5 }]
          return HttpResponse.json(list[1], { status: 201 })
        }),
      )
      renderApp('/cryptos')
      const user = userEvent.setup()
      await screen.findByText('Bitcoin')

      await user.click(screen.getByRole('button', { name: 'Add' }))
      await user.type(await screen.findByLabelText('Name'), 'Ether')
      await user.type(screen.getByLabelText('Price'), '12.5')
      await user.click(screen.getByRole('button', { name: 'Save' }))

      expect(await screen.findByText('Ether')).toBeInTheDocument()
      expect(created).toEqual({ name: 'Ether', initialPrice: 12.5 })
    })

    it('Add keeps Save disabled until price is at least 0.01', async () => {
      loginAsAdmin()
      listWith(bitcoin)
      renderApp('/cryptos')
      const user = userEvent.setup()
      await screen.findByText('Bitcoin')

      await user.click(screen.getByRole('button', { name: 'Add' }))
      await user.type(await screen.findByLabelText('Name'), 'Ether')
      const save = screen.getByRole('button', { name: 'Save' })
      expect(save).toBeDisabled()

      await user.type(screen.getByLabelText('Price'), '0')
      expect(save).toBeDisabled()

      await user.clear(screen.getByLabelText('Price'))
      await user.type(screen.getByLabelText('Price'), '0.01')
      expect(save).toBeEnabled()
    })

    async function openEdit(user: ReturnType<typeof userEvent.setup>) {
      await user.click(await screen.findByRole('button', { name: 'Edit Bitcoin' }))
      return within(await screen.findByRole('dialog'))
    }

    it('Edit sends only rename when only name changed', async () => {
      loginAsAdmin()
      listWith(bitcoin)
      const calls: string[] = []
      server.use(
        http.put(`${url}/1`, () => {
          calls.push('rename')
          return new HttpResponse(null, { status: 204 })
        }),
        http.put(`${url}/1/price`, () => {
          calls.push('price')
          return new HttpResponse(null, { status: 204 })
        }),
      )
      renderApp('/cryptos')
      const user = userEvent.setup()
      const dialog = await openEdit(user)

      await user.clear(dialog.getByLabelText('Name'))
      await user.type(dialog.getByLabelText('Name'), 'BTC')
      await user.click(dialog.getByRole('button', { name: 'Save' }))

      await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument())
      expect(calls).toEqual(['rename'])
    })

    it('Edit sends only price when only price changed', async () => {
      loginAsAdmin()
      listWith(bitcoin)
      const calls: string[] = []
      let priceBody: unknown = null
      server.use(
        http.put(`${url}/1`, () => {
          calls.push('rename')
          return new HttpResponse(null, { status: 204 })
        }),
        http.put(`${url}/1/price`, async ({ request }) => {
          calls.push('price')
          priceBody = await request.json()
          return new HttpResponse(null, { status: 204 })
        }),
      )
      renderApp('/cryptos')
      const user = userEvent.setup()
      const dialog = await openEdit(user)

      await user.clear(dialog.getByLabelText('Price'))
      await user.type(dialog.getByLabelText('Price'), '250')
      await user.click(dialog.getByRole('button', { name: 'Save' }))

      await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument())
      expect(calls).toEqual(['price'])
      expect(priceBody).toEqual({ newPrice: 250 })
    })

    it('Edit shows error when rename returns 409', async () => {
      loginAsAdmin()
      listWith(bitcoin)
      server.use(http.put(`${url}/1`, () => new HttpResponse('Name already exists', { status: 409 })))
      renderApp('/cryptos')
      const user = userEvent.setup()
      const dialog = await openEdit(user)

      await user.clear(dialog.getByLabelText('Name'))
      await user.type(dialog.getByLabelText('Name'), 'Ether')
      await user.click(dialog.getByRole('button', { name: 'Save' }))

      expect(await dialog.findByRole('alert')).toBeInTheDocument()
      expect(screen.getByRole('dialog')).toBeInTheDocument()
    })

    it('Delete does nothing until confirmed', async () => {
      loginAsAdmin()
      listWith(bitcoin)
      let deleted = false
      server.use(
        http.delete(`${url}/1`, () => {
          deleted = true
          return new HttpResponse(null, { status: 204 })
        }),
      )
      renderApp('/cryptos')
      const user = userEvent.setup()
      await user.click(await screen.findByRole('button', { name: 'Delete Bitcoin' }))
      const dialog = within(await screen.findByRole('dialog'))

      expect(dialog.getByText(/Bitcoin/)).toBeInTheDocument()
      await user.click(dialog.getByRole('button', { name: 'Cancel' }))

      await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument())
      expect(deleted).toBe(false)
    })

    it('Delete calls API after confirm', async () => {
      loginAsAdmin()
      let list = [bitcoin]
      server.use(
        http.get(url, () => HttpResponse.json(list)),
        http.delete(`${url}/1`, () => {
          list = []
          return new HttpResponse(null, { status: 204 })
        }),
      )
      renderApp('/cryptos')
      const user = userEvent.setup()
      await user.click(await screen.findByRole('button', { name: 'Delete Bitcoin' }))
      await user.click(within(await screen.findByRole('dialog')).getByRole('button', { name: 'Delete' }))

      expect(await screen.findByText('No cryptos yet.')).toBeInTheDocument()
    })

    it('row link goes to price-history for normal user', async () => {
      listWith(bitcoin)
      renderApp('/cryptos')

      const link = await screen.findByRole('link', { name: 'Price history' })
      expect(link).toHaveAttribute('href', '/cryptos/1/price-history')
    })
  })
})
