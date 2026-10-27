import { describe, expect, it } from 'vitest'
import { http, HttpResponse } from 'msw'
import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { server } from '../test/msw/server'
import { renderWithProviders } from '../test/renderWithProviders'
import { NewDisputePage } from './NewDisputePage'

async function fillValidForm() {
  await userEvent.type(screen.getByLabelText(/transaction id/i), '0199c0de-1001-7000-8000-000000000009')
  await userEvent.type(screen.getByLabelText(/amount/i), '250.00')
  await userEvent.clear(screen.getByLabelText(/currency/i))
  await userEvent.type(screen.getByLabelText(/currency/i), 'zar')
  await userEvent.type(screen.getByLabelText(/transaction date/i), '2026-10-20T12:00')
  await userEvent.type(screen.getByLabelText(/merchant/i), 'Checkers')
  await userEvent.type(screen.getByLabelText(/reason/i), 'Goods not received.')
}

describe('NewDisputePage', () => {
  it('posts an uppercased currency and a utc date', async () => {
    let body: Record<string, unknown> = {}
    server.use(
      http.post('*/api/disputes', async ({ request }) => {
        body = (await request.json()) as Record<string, unknown>
        return HttpResponse.json({ ...body, id: 'new-id', status: 'Initiated' }, { status: 201 })
      }),
    )
    renderWithProviders(<NewDisputePage />)

    await fillValidForm()
    await userEvent.click(screen.getByRole('button', { name: /raise dispute/i }))

    await waitFor(() => expect(body.transactionCurrency).toBe('ZAR'))
    expect(String(body.transactionDate)).toMatch(/Z$/)
    expect(body.transactionAmount).toBe(250)
  })

  it('does not send server assigned fields', async () => {
    let body: Record<string, unknown> = {}
    server.use(
      http.post('*/api/disputes', async ({ request }) => {
        body = (await request.json()) as Record<string, unknown>
        return HttpResponse.json({ id: 'new-id', status: 'Initiated' }, { status: 201 })
      }),
    )
    renderWithProviders(<NewDisputePage />)

    await fillValidForm()
    await userEvent.click(screen.getByRole('button', { name: /raise dispute/i }))

    await waitFor(() => expect(Object.keys(body)).toHaveLength(6))
    expect(body).not.toHaveProperty('status')
    expect(body).not.toHaveProperty('createdAt')
    expect(body).not.toHaveProperty('id')
  })

  it('blocks submission and shows a field error for a bad transaction id', async () => {
    renderWithProviders(<NewDisputePage />)

    await userEvent.type(screen.getByLabelText(/transaction id/i), 'not-a-uuid')
    await userEvent.click(screen.getByRole('button', { name: /raise dispute/i }))

    expect(await screen.findByText(/valid transaction id/i)).toBeInTheDocument()
  })

  it('rejects an amount with three decimal places before sending', async () => {
    renderWithProviders(<NewDisputePage />)

    await fillValidForm()
    await userEvent.clear(screen.getByLabelText(/amount/i))
    await userEvent.type(screen.getByLabelText(/amount/i), '10.999')
    await userEvent.click(screen.getByRole('button', { name: /raise dispute/i }))

    expect(await screen.findByText(/at most 2 decimal places/i)).toBeInTheDocument()
  })

  it('maps a server 400 onto the matching form field', async () => {
    server.use(
      http.post('*/api/disputes', () =>
        HttpResponse.json(
          {
            title: 'One or more validation errors occurred.',
            status: 400,
            errors: { MerchantName: ['The merchant is not recognised.'] },
          },
          { status: 400, headers: { 'content-type': 'application/problem+json' } },
        ),
      ),
    )
    renderWithProviders(<NewDisputePage />)

    await fillValidForm()
    await userEvent.click(screen.getByRole('button', { name: /raise dispute/i }))

    expect(await screen.findByText('The merchant is not recognised.')).toBeInTheDocument()
  })

  it('shows a banner when the user lacks permission', async () => {
    server.use(http.post('*/api/disputes', () => new HttpResponse(null, { status: 403 })))
    renderWithProviders(<NewDisputePage />)

    await fillValidForm()
    await userEvent.click(screen.getByRole('button', { name: /raise dispute/i }))

    expect(await screen.findByRole('alert')).toHaveTextContent(/permission/i)
  })
})
