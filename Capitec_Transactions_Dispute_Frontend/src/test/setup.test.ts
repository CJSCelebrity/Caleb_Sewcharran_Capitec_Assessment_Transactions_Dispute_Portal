import { expect, test } from 'vitest'
import { http, HttpResponse } from 'msw'
import { server } from './msw/server'

test('msw intercepts a stubbed api call', async () => {
  server.use(http.get('*/api/ping', () => HttpResponse.json({ ok: true })))

  const response = await fetch('/api/ping')

  expect(await response.json()).toEqual({ ok: true })
})
