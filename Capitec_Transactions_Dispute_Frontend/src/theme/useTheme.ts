import { useContext } from 'react'
import { ThemeContext } from './context'
import type { ThemeContextValue } from './context'

export function useTheme(): ThemeContextValue {
  const context = useContext(ThemeContext)

  if (!context) {
    throw new Error('useTheme must be used inside a ThemeProvider')
  }

  return context
}
