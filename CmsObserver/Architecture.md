# CMS Observer – Architecture Context (Take-Home Assignment)

## Solution Structure

### CmsObserver.API
- ASP.NET Core Minimal API
- Composition root (DI registration)
- Endpoint mapping only
- No business logic

### CmsObserver.Managers
- Application layer (use cases)
- `ICmsEventProcessor`
- Rx-based event pipeline
- Admin/query managers
- No persistence details

### CmsObserver.Accessors
- `IEntitiesAccessor` abstraction
- `InMemoryEntitiesAccessor` implementation
- Pure data access logic
- No business rules

---

## Flow

1. CMS sends event → `POST /cms/events`
2. Endpoint calls `ICmsEventProcessor.ProcessAsync(dto)`
3. Processor pushes event into a singleton Rx pipeline
4. Pipeline responsibilities:
   - Per-event retry
   - Idempotency check (eventId)
   - Enforce per-entity ordering (timestamp)
   - Branch by type:
     - Publish
     - Unpublish
     - Delete
5. Handlers call `IEntitiesAccessor`

---

## State Semantics

- **Publish** → Create or update entity
- **Unpublish** → Mark as inactive but keep data
- **Delete** → Hard delete
- **Admin disable** → Local override flag (separate from CMS state)

---

## Dependency Injection

- All DI configured in `Program.cs`
- `ICmsEventProcessor` → Singleton
- `IEntitiesAccessor` → Singleton (InMemory for now)
- Rx pipeline constructed once inside processor

---

## Constraints & Principles

- No over-engineering
- Clear separation of concerns
- Interfaces stable so EF Core implementation can replace in-memory later
- Endpoints contain zero business logic