## ADR-004: Customer Transactions and Disputes - 29-10-2026

## Status
Accepted - 2026-10-29

## Context

**History was partial.** Only the current `Status` plus `CreatedAt` and `ResolvedAt` were stored, so the moment a dispute moved `Initiated` to `UnderInvestigation` the time of that transition was lost, and no actor was ever recorded. The nearest thing to a historic view was `GET /api/disputes?status=Resolved`, which filters current status rather than showing a timeline.

## Options Considered

* Customer identity: reuse `ReadOnly` as the customer, add a `Customer` role with scoped data, or build a separate customer identity with its own registration and auth path
* Transaction source: a table in the disputes database seeded from CSV, a separate transactions service publishing events as ADR-001 sketched, or a static fixture in the API layer
* History depth: the customer's dispute list alone, that list plus a per-dispute status timeline, or a full field-level audit trail with temporal tables
* Create path: replace the hand-typed form with raise-from-transaction, keep both paths, or keep the manual form and prefill it from a transaction

## Decision

### 1. A Customer role, a customer id claim, and scoping in the service

`Roles` now has three members. `users` and `disputes` carry a `CustomerId`; `transactions` is owned by one from birth. The JWT carries a third claim, `customerId`, and `POST /api/auth/login` returns it in the body so the frontend never decodes the token, preserving ADR-003 decision 5.

A separate customer identity was rejected as far larger than the brief. Reusing `ReadOnly` was rejected because it leaves the data unscoped: the word "customers" in the brief would have been nominal while every customer still saw every other customer's transactions.

The request identity is read through `CallerContext`, a three-field record built from the `ClaimsPrincipal` by an extension method. It lives in the Application layer rather than the API layer because `System.Security.Claims` is in the base library and needs no ASP.NET reference, so the layering holds. Controllers call `User.ToCallerContext()` and pass it down. There is no `IHttpContextAccessor` and no ambient state, which is what keeps `DisputeService` testable with plain arguments.

Worth knowing for anyone extending this: `ClaimsPrincipal.FindFirstValue` is an ASP.NET extension method, not a base library one. `CallerContext` uses `FindFirst(...)?.Value` so the Application project stays free of an ASP.NET dependency.

### 2. The customer id is generated at seed time, never typed into a CSV

`SeedUserCsvReader` splits each row on the **first and last comma** so that a password may contain commas. A fourth column would break that rule, and no person should be hand-typing a GUID into a CSV anyway. So `UserSeeder` mints the id with `Guid.CreateVersion7()` for `Customer` rows and leaves it null for staff. Because seeding is insert-only the id is stable once created, and the transaction and dispute seeders resolve it by email. No seed file contains a GUID.

### 3. Transactions live in the disputes database, seeded from CSV

A new `transactions` table, an `ITransactionRepository`, and `seed-transactions.csv` with twelve rows across two customers. The whole stack still comes up with `docker compose up` and one `dotnet run`.

This **supersedes the ADR-001 sketch** of an external transactions service publishing events that the disputes backend consumes. That design is deferred, not rejected: it needs a broker, a consumer and a second deployable, which is more infrastructure than the brief asks for. `ITransactionRepository` is the seam a real transactions service would replace, and it is the only place that knows where a transaction comes from.

### 4. A transaction has a human readable reference

`Transaction.Reference`, unique, like `TXN-1001`. A seeded dispute has to name the transaction it disputes, and a GUID in a CSV is unreadable and unstable across a reseed. A reference is also how a bank customer actually recognises a transaction, so it earns its place in the UI, and it gives the table a natural unique index.

### 5. A dispute is derived from a transaction, and the server does the deriving

`CreateDisputeRequest` is now two fields, `TransactionId` and `Reason`. Amount, currency, date and merchant are read off the transaction row. `Dispute.Raise` takes the `Transaction` entity itself and sets `CustomerId` from `transaction.CustomerId`, which makes a dispute whose owner disagrees with its transaction's owner unrepresentable.

The six denormalised columns stay on `Dispute`. A dispute is an audit trail and must not change if the transaction is later corrected. That also meant `DisputeResponse` and the whole frontend `Dispute` type were untouched; only the **source** of those values changed, from client input to a server-side lookup.

**This supersedes ADR-003 Amendment 3.** That amendment named the browser-minted GUID as the first decision to revisit once a transaction existed to point at. It has been revisited. `crypto.randomUUID()` is gone from the portal and the transaction id now references a real row.

`POST /api/disputes` is `Customer` only. A `DisputeAdmin` has no customer id and therefore cannot raise a dispute, which matches the brief: customers dispute, staff adjudicate. Keeping both create paths was rejected as two contracts, two schemas and two sets of tests for one capability.

### 6. Another customer's dispute is a 404, not a 403

A 403 confirms the resource exists. Anything the caller cannot see returns 404, so the id space stays opaque. The decision lives in one `CanSee` helper in `DisputeService`, and because the controller already mapped a null service result to `NotFound()`, no controller branch was needed.

A customer whose token somehow carries no `customerId` is treated as seeing nothing rather than everything. That is the one way this design could have failed open, so it is guarded explicitly and covered by a test.

### 7. One dispute per transaction, enforced in the service

A second dispute on the same transaction is a 409. The `TransactionId` index on `disputes` stays **non-unique**, so this is a service-level rule rather than a database constraint, and two simultaneous posts could still race past it. A unique index is the fix if that ever matters.

The transactions list carries each row's `disputeId` and `disputeStatus` so the UI can offer either a dispute action or a link to the dispute that already exists, which keeps a customer out of a form that cannot succeed.

### 8. History is a first class entity, appended inside the aggregate

`DisputeStatusChange` records `FromStatus`, `ToStatus`, `ChangedBy` and `ChangedAt`. `FromStatus` is nullable and `Raise` writes the first entry with a null from-status, so the timeline is entirely the history table and the frontend synthesises nothing.

The append happens **inside `Dispute`**, not in the service. `_statusChanges` is a private list exposed as `IReadOnlyCollection`, and `DisputeStatusChange.Record` is `internal`, so only the domain assembly can create one. There is no path that changes a status without recording it, which is the same reasoning that put the transition rule in the entity in the first place. A test asserts that a rejected transition appends nothing, and it was checked for teeth: appending before the guard passes every other test in the file.

### 9. Every key is declared as assigned by the domain, not the store

This one cost real debugging time and is the most transferable thing in this entry.

All four entities assign their own `Guid` primary key in a static factory. EF Core, however, treats a `Guid` key as **store generated** by default, and for a store generated key it decides Added versus Modified by whether the key already holds a value. A `DisputeStatusChange` appended to a tracked `Dispute` therefore arrived with a key already set, so EF concluded the row existed and issued `UPDATE dispute_status_changes ... WHERE "Id" = @p5`, which matched nothing and threw `DbUpdateConcurrencyException: expected to affect 1 row(s), but actually affected 0`.

`Dispute` itself never hit this because `CreateAsync` calls `dbContext.Disputes.Add(...)`, which sets the state explicitly and cascades Added to its children. Only the append-to-an-existing-aggregate path was affected.

The fix is `ValueGeneratedNever()` on all four keys, which is simply the truth about this model. It changes no DDL, so no migration was needed. Two guards now hold it: `ChangeStatusAsync` loads through `FindWithHistoryAsync` so the collection it appends to is tracked, and `DisputesDbContextModelTests` asserts the `ValueGenerated` of every primary key, with a failure message explaining this trap.

**No mocked unit test could have caught this.** With a mocked repository the in-memory append is simply correct. It was found by exercising the running API against Postgres and reading the generated SQL, which is why the verification walkthrough in the README is part of the deliverable rather than a nicety.

### 10. The raise page derives its transaction from the cached list

There is deliberately no `GET /api/transactions/{id}`. `RaiseDisputePage` calls the existing `useTransactionsQuery()` and selects by the route param. The customer has almost always just come from the transactions page, so the list is already in the TanStack Query cache; on a deep link the hook fetches it on mount and the lookup still resolves. The trade is fetching a dozen rows to render one, in exchange for one fewer endpoint, service method, controller action and set of tests. Revisit this once a customer can have thousands of transactions.

### 11. One route serves both audiences

`/disputes` is kept for customers and staff alike rather than adding a parallel `/my/disputes`. The server scopes the list, so one page, one set of hooks and one set of tests serve both. The heading reads "My disputes" for a customer and "Disputes" for staff, and the primary action is "View transactions" for a customer and nothing for staff, since staff can no longer raise.

### 12. The three HasData demo disputes were dropped

They predated customer ownership, their ids were fixed, and giving them an owner would have meant hand-writing a customer GUID into a seed file, which decision 2 exists to avoid. The migration deletes them **before** adding the non-nullable `CustomerId`, which is the only safe order; EF happened to scaffold it that way, and it is worth checking on any similar migration.

They are replaced by `seed-disputes.csv`, whose rows are raised through `Dispute.Raise` and advanced through `ChangeStatus` one legal step at a time. The seeded history is therefore produced by the same code path the UI uses and cannot drift from it, and a reviewer sees a populated timeline on first boot. Resetting a local database is `docker compose down -v`.

### 13. Every new fetch path got its own MSW handler

Worth recording because it is an easy thing to get wrong when reading the code. MSW 3 renamed `onUnhandledRequest` to `onUnhandledFrame`, since the option now governs WebSocket connections as well as requests. `src/test/setup.ts` already uses the current name, so the suite really does fail on any unmocked call, and `onUnhandledRequest` would be silently ignored if anyone "corrected" it back.

The practical consequence is that adding the timeline to the dispute detail page meant adding a history handler to **every** test in that file, not just the new ones, because each of them now triggers a second request.

## Consequences

**Positive**

- The brief is satisfied end to end: a customer signs in, sees only their own transactions, disputes one of them, and reads a timeline of every status change with its actor.
- Scoping lives in one place per concern, `CanSee` and `CustomerFilterFor` in `DisputeService`, and the repositories take the customer filter as a parameter so there is no overload that can accidentally return another customer's rows. `ITransactionRepository` puts the filter inside both of its methods.
- The transaction id finally references something. The create path cannot be used to invent a transaction, because the server derives every transaction field from a row it looked up itself.
- A status change cannot happen without being recorded, because the append is inside the entity and the history factory is internal to the domain assembly.
- Seeded demo data goes through the real domain methods, so the history a reviewer sees on first boot is the same shape the UI produces.
- The backend suite went from 47 tests to 124 and the frontend from 88 to 115, with the security boundary, the create path and the timeline all covered.

**Negative**

- `TransactionService` joins transactions to disputes **in memory**, with two customer-scoped queries. That is right at a dozen rows per customer and wrong at a thousand. The fix is a `GroupJoin` in `TransactionRepository` returning a projection.
- Seeding is still Development only, so a Production database boots with no users, no transactions and no disputes. There is no administrative path to create any of them.
- One dispute per transaction is a service rule, not a constraint, so it can be raced.
- There is still no CORS policy. The Vite dev proxy covers development only and a deployed frontend on another origin still needs that decision, exactly as ADR-003 recorded.
- There is still no token refresh or revocation, unchanged from ADR-002.
- The controllers and the `[Authorize]` attributes still have no automated coverage, consistent with the decision in the disputes controller spec. The compensating control is the manual walkthrough in the README, which exercises every role boundary and every failure code; it was run and it is what caught decision 9.
- A dev JWT signing key remains in git history, unchanged and still worth rotating before this is deployed anywhere real.
- `RaiseDisputePage` fetching the whole transaction list to render one row is a deliberate shortcut, recorded in decision 10.
