import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faMoon, faSun } from '@fortawesome/free-solid-svg-icons'
import type { IconDefinition } from '@fortawesome/fontawesome-svg-core'
import { otherTheme } from '../theme/theme'
import type { Theme } from '../theme/theme'
import { useTheme } from '../theme/useTheme'

const TARGET_ICON: Record<Theme, IconDefinition> = {
  light: faSun,
  dark: faMoon,
}

export function ThemeToggle() {
  const { theme, toggleTheme } = useTheme()
  const target = otherTheme(theme)

  return (
    <button
      type="button"
      className="secondary theme-toggle"
      onClick={toggleTheme}
      aria-label={`Switch to ${target} theme`}
    >
      <FontAwesomeIcon icon={TARGET_ICON[target]} />
    </button>
  )
}
