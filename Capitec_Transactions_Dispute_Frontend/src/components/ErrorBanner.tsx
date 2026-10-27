import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faCircleExclamation } from '@fortawesome/free-solid-svg-icons'

export function ErrorBanner({ title, message }: { title: string; message?: string }) {
  return (
    <div className="error-banner" role="alert">
      <FontAwesomeIcon icon={faCircleExclamation} />
      <div className="error-banner__body">
        <strong>{title}</strong>
        {message ? <p>{message}</p> : null}
      </div>
    </div>
  )
}
