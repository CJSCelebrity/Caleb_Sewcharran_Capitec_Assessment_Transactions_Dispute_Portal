import { describe, expect, it } from 'vitest'
import { render, screen, within } from '@testing-library/react'
import type { DisputeStatusChange } from '../domain/disputeHistory'
import { DisputeTimeline } from './DisputeTimeline'

const history: DisputeStatusChange[] = [
  {
    fromStatus: null,
    toStatus: 'Initiated',
    changedBy: 'thandi@example.test',
    changedAt: '2026-10-21T08:00:00+00:00',
  },
  {
    fromStatus: 'Initiated',
    toStatus: 'UnderInvestigation',
    changedBy: 'admin@capitec.test',
    changedAt: '2026-10-22T09:00:00+00:00',
  },
]

describe('DisputeTimeline', () => {
  it('renders an ordered list with one item per change, oldest first', () => {
    render(<DisputeTimeline history={history} isPending={false} isError={false} />)

    const items = within(screen.getByRole('list')).getAllByRole('listitem')

    expect(items).toHaveLength(2)
    expect(items[0]).toHaveTextContent('Raised')
    expect(items[1]).toHaveTextContent('Moved to under investigation')
  })

  it('names the actor behind each change', () => {
    render(<DisputeTimeline history={history} isPending={false} isError={false} />)

    const items = within(screen.getByRole('list')).getAllByRole('listitem')

    expect(items[0]).toHaveTextContent('thandi@example.test')
    expect(items[1]).toHaveTextContent('admin@capitec.test')
  })

  it('shows a pending note while loading', () => {
    render(<DisputeTimeline history={undefined} isPending isError={false} />)

    expect(screen.getByText(/Loading the history/)).toBeInTheDocument()
    expect(screen.queryByRole('list')).not.toBeInTheDocument()
  })

  it('explains a failure without claiming there is no history', () => {
    render(<DisputeTimeline history={undefined} isPending={false} isError />)

    expect(screen.getByText('The history for this dispute could not be loaded.')).toBeInTheDocument()
    expect(screen.queryByRole('list')).not.toBeInTheDocument()
  })
})
