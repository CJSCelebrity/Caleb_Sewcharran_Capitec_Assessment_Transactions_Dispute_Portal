# Capitec Transactions Dispute Portal

A system for customers to view their card transactions and dispute them, with a historic view of every dispute they have raised and a timeline of how each one progressed. Back-office staff sign in to the same portal to work through the disputes customers have raised.

Built for an assessment. The reasoning behind every significant decision is recorded in [`docs/adr/`](docs/adr/), newest last.

## What it does

| Role | Can |
|---|---|
| `Customer` | View their own transactions, raise a dispute on one of them, view their own dispute history and each dispute's timeline |
| `ReadOnly` | View every customer's disputes and timelines. Change nothing |
| `DisputeAdmin` | View every customer's disputes, and advance a dispute one step through `Initiated` to `UnderInvestigation` to `Resolved` |

A customer sees only their own data. Anything belonging to another customer returns `404`, never `403`, so dispute ids stay opaque. Staff cannot raise a dispute, and customers cannot change a status.

Two rules are enforced in the domain and cannot be bypassed from the API:

- A dispute advances **one step forward at a time**. Anything else is a `409`, and `Resolved` is terminal.
- A transaction can carry **one dispute**. A second attempt is a `409`.

## Architecture

```
Capitec_Disputes_Backend/              .NET 10, Clean Architecture
  CapitecDisputes.Domain/              entities and the rules they protect. References nothing
  CapitecDisputes.Application/         contracts, services, repository interfaces, CallerContext
  CapitecDisputes.Infrastructure/      EF Core, Postgres, JWT, CSV seeders
  CapitecDisputes.Api/                 controllers, composition root, OpenAPI
  CapitecDisputes.Tests/               xUnit, Moq, Shouldly

Capitec_Transactions_Dispute_Frontend/ React 19, TypeScript, Vite
  src/api/                             hand written fetch client, one module per resource
  src/domain/                          types, zod schemas, formatting, the transition rule
  src/hooks/                           TanStack Query hooks
  src/pages/                           sign in, transactions, raise dispute, dispute list, dispute detail
  src/components/                      layout, status badge, timeline, field, error banner, theme toggle
```

Both run against one PostgreSQL database. Migrations apply automatically at startup.

### API

| Method | Route | Roles |
|---|---|---|
| `POST` | `/api/auth/login` | anonymous |
| `GET` | `/api/transactions` | `Customer` |
| `GET` | `/api/disputes?status=` | all three, scoped to the caller |
| `GET` | `/api/disputes/{id}` | all three, scoped to the caller |
| `GET` | `/api/disputes/{id}/history` | all three, scoped to the caller |
| `POST` | `/api/disputes` | `Customer` |
| `PATCH` | `/api/disputes/{id}/status` | `DisputeAdmin` |

There is deliberately no `PUT` and no `DELETE`, because a dispute is an audit trail. In Development the browsable API reference is at `/scalar/v1`.

## Running it

Prerequisites: .NET 10 SDK, Node 20 or newer, Docker.

### 1. Configure the backend

```bash
cd Capitec_Disputes_Backend

cp .env.example .env
# set JWT_SIGNING_KEY in .env, minimum 32 bytes:
#   openssl rand -base64 48

cp CapitecDisputes.Api/seed-users.example.csv CapitecDisputes.Api/seed-users.csv
# fill in a password for each of the four accounts
```

`seed-users.csv` is gitignored and never committed. `seed-transactions.csv` and `seed-disputes.csv` are committed, because they hold no secrets.

For `dotnet run` the signing key comes from user secrets rather than `.env`:

```bash
dotnet user-secrets set "Jwt:SigningKey" "$(openssl rand -base64 48)" --project CapitecDisputes.Api
```

The API refuses to start without a signing key of at least 32 bytes. That is deliberate.

### 2. Start the stack

Two terminals, Postgres in Docker and the API on the host:

```bash
# terminal 1
cd Capitec_Disputes_Backend
docker compose up -d postgres
dotnet run --project CapitecDisputes.Api        # http://localhost:5089

# terminal 2
cd Capitec_Transactions_Dispute_Frontend
npm install
npm run dev                                     # http://localhost:5173
```

Or fully containerised, which serves the API on port 8080 instead:

```bash
cd Capitec_Disputes_Backend
docker compose up --build
```

If you do that, point the frontend at it before `npm run dev`:

```bash
cd Capitec_Transactions_Dispute_Frontend
cp .env.example .env
# set VITE_API_PROXY_TARGET=http://localhost:8080
```

The frontend reaches the API through a Vite dev proxy on `/api`, so every browser request is same origin and there is no CORS policy on the backend. That covers development only; see the ADRs.

### Seed data and resetting

On startup in Development, three insert-only seeders run in order: users, then transactions, then disputes. Insert-only means editing a password in `seed-users.csv` has no effect until that row is deleted from the database.

| File | Contents |
|---|---|
| `CapitecDisputes.Api/seed-users.csv` | 2 customers, 1 `DisputeAdmin`, 1 `ReadOnly`. Gitignored |
| `CapitecDisputes.Api/seed-transactions.csv` | 12 transactions, 6 per customer |
| `CapitecDisputes.Api/seed-disputes.csv` | 5 disputes at varying statuses, so the history view is populated on first boot |

To start completely clean:

```bash
cd Capitec_Disputes_Backend
docker compose down -v
```

That drops the volume, so migrations and all three seeders run from scratch. Note that the migration which introduced customer ownership deletes the three demo disputes that earlier versions seeded through EF `HasData`.

## Seeing the brief work

Sign in as the customer whose password you set for `thandi@example.test`.

1. You land on **Transactions**, showing six transactions. Three already carry a dispute badge and a link to it; three offer a **Dispute** action.
2. Click **Dispute** on an undisputed transaction. The transaction is shown read only and the only thing to fill in is the reason. Submitting an empty reason is refused in the browser with no request sent.
3. Enter a reason and submit. You land on the new dispute, whose timeline already has one entry, **Raised**, attributed to you.
4. Open **My disputes**. Four disputes now, with a Resolved column and a working status filter. This is the historic view.
5. Open the Takealot dispute. Its timeline has three entries: raised by the customer, then two advances by the admin.
6. Sign out and sign in as `sipho@example.test`. You see a different six transactions and two disputes. Paste one of the first customer's dispute URLs: you get a not-found message, not a permission error and not their data.
7. Sign out and sign in as `admin@capitec.test`. There is no Transactions link, and the disputes list shows **all five** customers' disputes.
8. Advance the dispute from step 3. Its timeline gains an entry attributed to the admin, without a reload. Only the one legal next step is ever offered.
9. Sign in as `readonly@capitec.test`. The list is there; the advance button is not.

The theme toggle in the header works on every page, and the timeline and tables are readable in both themes.

## Tests

```bash
cd Capitec_Disputes_Backend
dotnet test                       # 124 tests

cd ../Capitec_Transactions_Dispute_Frontend
npm test                          # 115 tests across 19 files
npm run build                     # tsc --noEmit, then vite build
npm run lint
```

The backend covers the domain rules, the scoping decisions, the create and advance paths, the CSV readers and the EF model's key configuration. The frontend covers the API client, the hooks, the schemas and every page, against a mocked API with MSW set to fail on any unmocked request.

What is **not** covered automatically, by decision recorded in the specs: the controllers and their `[Authorize]` attributes, the migrations, and the repositories against a real database. The walkthrough above is the compensating control, and running it is what surfaced the EF key-generation bug described in ADR-004 decision 9.

## Known limitations

Carried forward honestly rather than hidden. The full list with reasoning is in [ADR-004](docs/adr/ADR-Customer-Transactions-And-Disputes.md).

- `TransactionService` joins transactions to disputes in memory. Correct at demo scale, wrong at thousands of rows per customer.
- Seeding is Development only, so a Production database boots empty and there is no admin path to create accounts.
- One dispute per transaction is enforced in the service, not by a unique index, so simultaneous requests could race past it.
- No CORS policy, so deployment needs either a policy or the SPA served from the API origin.
- No token refresh and no revocation. A token is valid for eight hours once issued.
- A dev JWT signing key remains in git history and should be rotated before any real deployment.

## Decision records

| ADR | Subject |
|---|---|
| [ADR-001](docs/adr/ADR-Initial-Implementation.md) | First sketch of the dispute flow and the status model |
| [ADR-002](docs/adr/ADR-Seeded-Sign-In.md) | Seeded accounts and JWT sign in, later moved to CSV |
| [ADR-003](docs/adr/ADR-Frontend-UI.md) | The portal UI, TypeScript, the palette, dark mode and the theme toggle |
| [ADR-004](docs/adr/ADR-Customer-Transactions-And-Disputes.md) | Customers, transactions, scoping and the dispute history |
