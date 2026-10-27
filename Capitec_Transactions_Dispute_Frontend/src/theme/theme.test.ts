import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import {
  applyTheme,
  DARK_QUERY,
  isTheme,
  otherTheme,
  prefersDark,
  readStoredTheme,
  storeTheme,
  THEME_STORAGE_KEY,
} from './theme'

function stubMatchMedia(matches: boolean) {
  vi.stubGlobal(
    'matchMedia',
    vi.fn(() => ({
      matches,
      media: DARK_QUERY,
      addEventListener: vi.fn(),
      removeEventListener: vi.fn(),
    })),
  )
}

beforeEach(() => {
  localStorage.clear()
  document.documentElement.removeAttribute('data-theme')
})

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('isTheme', () => {
  it('accepts only the two supported themes', () => {
    expect(isTheme('light')).toBe(true)
    expect(isTheme('dark')).toBe(true)
    expect(isTheme('system')).toBe(false)
    expect(isTheme(null)).toBe(false)
  })
})

describe('readStoredTheme', () => {
  it('returns a stored dark choice', () => {
    localStorage.setItem(THEME_STORAGE_KEY, 'dark')

    expect(readStoredTheme()).toBe('dark')
  })

  it('returns a stored light choice even when the OS is dark', () => {
    stubMatchMedia(true)
    localStorage.setItem(THEME_STORAGE_KEY, 'light')

    expect(readStoredTheme()).toBe('light')
  })

  it('seeds from the OS on a first visit with nothing stored', () => {
    stubMatchMedia(true)

    expect(readStoredTheme()).toBe('dark')
  })

  it('ignores a stored value that is not a theme', () => {
    stubMatchMedia(false)
    localStorage.setItem(THEME_STORAGE_KEY, 'system')

    expect(readStoredTheme()).toBe('light')
  })

  it('defaults to light when the environment has no matchMedia', () => {
    expect(readStoredTheme()).toBe('light')
  })
})

describe('storeTheme', () => {
  it('persists the theme', () => {
    storeTheme('dark')

    expect(localStorage.getItem(THEME_STORAGE_KEY)).toBe('dark')
  })

  it('overwrites a previous value', () => {
    storeTheme('dark')
    storeTheme('light')

    expect(localStorage.getItem(THEME_STORAGE_KEY)).toBe('light')
  })
})

describe('prefersDark', () => {
  it('reports false when the environment has no matchMedia', () => {
    expect(prefersDark()).toBe(false)
  })

  it('reads the dark colour scheme query when matchMedia exists', () => {
    stubMatchMedia(true)

    expect(prefersDark()).toBe(true)
  })
})

describe('otherTheme', () => {
  it('flips in both directions', () => {
    expect(otherTheme('light')).toBe('dark')
    expect(otherTheme('dark')).toBe('light')
  })
})

describe('applyTheme', () => {
  it('stamps the theme onto the root element', () => {
    applyTheme('dark')

    expect(document.documentElement.getAttribute('data-theme')).toBe('dark')
  })
})
