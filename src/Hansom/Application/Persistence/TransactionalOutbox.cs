// Transactional outbox middleware contract.
//
// This is not a middleware type. The actual wiring lands in a follow-up slice that uses
// Hansom.Application.Middleware.IMessageMiddleware to enforce the contract around each
// handler attempt. Keeping the contract here (next to IOutboxTransaction) lets the port
// land first without inventing middleware in this slice.
//
// A handler that participates in the outbox MUST:
//   1. Call IOutboxTransaction.StageAsync for every cascaded message it produces.
//   2. On success, call IOutboxTransaction.CommitAsync so the staged envelopes become
//      pending and survive a host restart.
//   3. On failure, call IOutboxTransaction.RollbackAsync so a throwing handler
//      publishes nothing ("no save then publish").
//
// The transaction is short-lived (one per handler call) while the store is long-lived
// (one per host); obtain it from the store via InMemoryMessageStore.BeginOutboxTransaction.
// The Postgres adapter will implement the same IOutboxTransaction shape over an Npgsql
// connection so outbox rows commit in the same transaction as the handler's writes.
