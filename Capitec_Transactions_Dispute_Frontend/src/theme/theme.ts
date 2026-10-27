export const THEME_STORAGE_KEY = 'capitec-disputes-theme'

export const THEMES = ['light', 'dark'] as const

export type Theme = (typeof THEMES)[number]

export const DARK_QUERY = '(prefers-color-scheme: dark)'

export function isTheme(value: unknown): value is Theme {
  return typeof value === 'string' && (THEMES as readonly string[]).includes(value)
}

export function prefersDark(): boolean {
  return window.matchMedia?.(DARK_QUERY).matches ?? false
}

export function readStoredTheme(): Theme {
  try {
    const raw = localStorage.getItem(THEME_STORAGE_KEY)

    if (isTheme(raw)) {
      return raw
    }

    return prefersDark() ? 'dark' : 'light'
  } catch {
    return 'light'
  }
}

export function storeTheme(theme: Theme): void {
  try {
    localStorage.setItem(THEME_STORAGE_KEY, theme)
  } catch {
    return
  }
}

export function otherTheme(theme: Theme): Theme {
  return theme === 'dark' ? 'light' : 'dark'
}

export function applyTheme(theme: Theme): void {
  document.documentElement.setAttribute('data-theme', theme)
}
