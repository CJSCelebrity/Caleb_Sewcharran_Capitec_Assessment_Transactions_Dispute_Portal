import { beforeEach, describe, expect, it, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import type { ReactNode } from 'react'
import { ThemeProvider } from '../theme/ThemeProvider'
import { THEME_STORAGE_KEY } from '../theme/theme'
import { ThemeToggle } from './ThemeToggle'

function renderToggle() {
  function Wrapper({ children }: { children: ReactNode }) {
    return <ThemeProvider>{children}</ThemeProvider>
  }

  return render(<ThemeToggle />, { wrapper: Wrapper })
}

function stamped() {
  return document.documentElement.getAttribute('data-theme')
}

beforeEach(() => {
  localStorage.clear()
  document.documentElement.removeAttribute('data-theme')
  vi.unstubAllGlobals()
})

describe('ThemeToggle', () => {
  it('names the theme the next click will select, so an icon only control still reads', () => {
    renderToggle()

    expect(screen.getByRole('button', { name: 'Switch to dark theme' })).toBeInTheDocument()
  })

  it('flips between the two themes on repeated clicks', async () => {
    renderToggle()

    await userEvent.click(screen.getByRole('button'))

    expect(screen.getByRole('button')).toHaveAccessibleName('Switch to light theme')
    expect(stamped()).toBe('dark')
    expect(localStorage.getItem(THEME_STORAGE_KEY)).toBe('dark')

    await userEvent.click(screen.getByRole('button'))

    expect(screen.getByRole('button')).toHaveAccessibleName('Switch to dark theme')
    expect(stamped()).toBe('light')
    expect(localStorage.getItem(THEME_STORAGE_KEY)).toBe('light')
  })

  it('shows the glyph of the theme it will switch to', async () => {
    const { container } = renderToggle()

    expect(container.querySelector('svg')!.getAttribute('data-icon')).toBe('moon')

    await userEvent.click(screen.getByRole('button'))

    expect(container.querySelector('svg')!.getAttribute('data-icon')).toBe('sun')
  })

  it('keeps its icon decorative so the accessible name comes from the button alone', () => {
    const { container } = renderToggle()

    const icon = container.querySelector('svg')

    expect(icon).not.toBeNull()
    expect(icon!.getAttribute('aria-hidden')).toBe('true')
    expect(screen.getByRole('button').textContent).toBe('')
  })

  it('offers no third state, however many times it is clicked', async () => {
    renderToggle()

    const seen = new Set<string>()

    for (let click = 0; click < 5; click += 1) {
      seen.add(stamped() ?? 'unset')
      await userEvent.click(screen.getByRole('button'))
    }

    expect([...seen].sort()).toEqual(['dark', 'light'])
  })
})
