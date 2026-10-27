export function ErrorBanner({ title, message }: { title: string; message?: string }) {
  return (
    <div className="error-banner" role="alert">
      <strong>{title}</strong>
      {message ? <p>{message}</p> : null}
    </div>
  )
}
