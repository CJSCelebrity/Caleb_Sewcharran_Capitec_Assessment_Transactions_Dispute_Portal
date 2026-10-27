import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { act, renderHook } from '@testing-library/react'
import type { ReactNode } from 'react'
import { ThemeProvider } from './ThemeProvider'
import { useTheme } from './useTheme'
import { DARK_QUERY, THEME_STORAGE_KEY } from './theme'

type Listener = (event: MediaQueryListEvent) => void

function stubMatchMedia(initialDark: boolean) {
  const listeners: Listener[] = []

  const state = {
    dark: initialDark,
    listenerCount: () => listeners.length,
    emit(dark: boolean) {
      state.dark = dark
      listeners.forEach((listener) => listener({ matches: dark } as MediaQueryListEvent))
    },
  }

  vi.stubGlobal(
    'matchMedia',
    vi.fn(() => ({
      get matches() {
        return state.dark
      },
      media: DARK_QUERY,
      addEventListener: (_: string, listener: Listener) => listeners.push(listener),
      removeEventListener: (_: string, listener: Listener) => {
        const at = listeners.indexOf(listener)
        if (at >= 0) {
          listeners.splice(at, 1)
        }
      },
    })),
  )

  return state
}

function Wrapper({ children }: { children: ReactNode }) {
  return <ThemeProvider>{children}</ThemeProvider>
}

function stamped() {
  return document.documentElement.getAttribute('data-theme')
}

beforeEach(() => {
  localStorage.clear()
  document.documentElement.removeAttribute('data-theme')
})

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('ThemeProvider', () => {
  it('seeds from a dark OS on a first visit', () => {
    stubMatchMedia(true)

    const { result } = renderHook(() => useTheme(), { wrapper: Wrapper })

    expect(result.current.theme).toBe('dark')
    expect(stamped()).toBe('dark')
  })

  it('seeds from a light OS on a first visit', () => {
    stubMatchMedia(false)

    const { result } = renderHook(() => useTheme(), { wrapper: Wrapper })

    expect(result.current.theme).toBe('light')
    expect(stamped()).toBe('light')
  })

  it('falls back to light when the environment has no matchMedia', () => {
    const { result } = renderHook(() => useTheme(), { wrapper: Wrapper })

    expect(result.current.theme).toBe('light')
    expect(stamped()).toBe('light')
  })

  it('restores a stored theme over the OS preference', () => {
    stubMatchMedia(true)
    localStorage.setItem(THEME_STORAGE_KEY, 'light')

    const { result } = renderHook(() => useTheme(), { wrapper: Wrapper })

    expect(result.current.theme).toBe('light')
    expect(stamped()).toBe('light')
  })

  it('toggles between the two themes, persisting and stamping each time', () => {
    stubMatchMedia(false)

    const { result } = renderHook(() => useTheme(), { wrapper: Wrapper })

    act(() => result.current.toggleTheme())

    expect(result.current.theme).toBe('dark')
    expect(stamped()).toBe('dark')
    expect(localStorage.getItem(THEME_STORAGE_KEY)).toBe('dark')

    act(() => result.current.toggleTheme())

    expect(result.current.theme).toBe('light')
    expect(stamped()).toBe('light')
    expect(localStorage.getItem(THEME_STORAGE_KEY)).toBe('light')
  })

  it('sets a theme directly', () => {
    stubMatchMedia(false)

    const { result } = renderHook(() => useTheme(), { wrapper: Wrapper })

    act(() => result.current.setTheme('dark'))

    expect(result.current.theme).toBe('dark')
    expect(localStorage.getItem(THEME_STORAGE_KEY)).toBe('dark')
  })

  it('does not track the OS after load, because there is no follow the system mode', () => {
    const media = stubMatchMedia(false)

    const { result } = renderHook(() => useTheme(), { wrapper: Wrapper })

    expect(result.current.theme).toBe('light')

    act(() => media.emit(true))

    expect(result.current.theme).toBe('light')
    expect(stamped()).toBe('light')
    expect(media.listenerCount()).toBe(0)
  })
})
