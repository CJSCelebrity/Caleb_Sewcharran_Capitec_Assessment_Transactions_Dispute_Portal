# Capitec Transactions Dispute Portal, frontend

React 19 + TypeScript + Vite. See the [root README](../README.md) for what the system does, how to run the whole stack, and the decision records.

## Scripts

| Command | Does |
|---|---|
| `npm run dev` | Dev server on http://localhost:5173, proxying `/api` to the backend |
| `npm run build` | `tsc --noEmit` then a production build. The typecheck is the real gate |
| `npm test` | Vitest once |
| `npm run test:watch` | Vitest in watch mode |
| `npm run lint` | ESLint |
| `npm run preview` | Serve the production build |

## Configuration

Copy `.env.example` to `.env` if the backend is not on the default port.

| Variable | Default | Purpose |
|---|---|---|
| `VITE_API_PROXY_TARGET` | `http://localhost:5089` | Where the dev server proxies `/api`. Use `http://localhost:8080` when the API runs in Docker |
| `VITE_API_BASE_URL` | `/api` | Base path the client calls. Only change this if you are not using the dev proxy |

## Conventions worth knowing before editing

- Import from `react-router`, never `react-router-dom`.
- `verbatimModuleSyntax` is on, so type imports must be written `import type`.
- 2 space indent, single quotes, no semicolons, trailing commas.
- MSW runs with `onUnhandledFrame: 'error'` and has no default handlers, so every request a test triggers needs its own handler. Note the option name: MSW 3 renamed it from onUnhandledRequest, so the older spelling is silently ignored.
- `src/index.css` is one stylesheet in three layers: fixed primitives, scales, then semantic tokens. A component rule that names a primitive directly is a bug, because it will not survive the theme switch.
- The theme resolution rule exists twice, in `src/theme/theme.ts` and as an inline script in `index.html`, so there is no flash of the wrong theme before React mounts. Keep them in step.
- Some markup is pinned by tests on purpose: the status badge's text must equal the status label exactly, the formatted amount must stay a single text node, the dispute detail heading must contain only the merchant name, and the lists must remain real tables inside their scroll regions.
