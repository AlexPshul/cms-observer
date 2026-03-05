# CMS Observer

CMS Observer is a regular ASP.NET Core API that listens to CMS webhook events and keeps a local, queryable view of entities.
For this repo, the sample events are MCU-themed: a character is **published** while active in the timeline, and **unpublished** when they are no longer publicly visible (for example, after a character death event).

## What this service does

1. Receives CMS event batches at `POST /cms/events` (Basic auth).
2. Processes `publish`, `unpublish`, and `delete` events.
3. Persists entity state in SQLite.
4. Exposes read endpoints for observer users/admins.
5. Allows admins to locally disable an entity without changing CMS data.

## Quick start

Start the service by running the `CmsObserver.API` project. You can use the provided `.http` files to interact with the API and simulate CMS events.  
Default URLs:

- `https://localhost:7295` (used by the `.http` files)
- `http://localhost:5185`

## SQLite and credentials (no setup needed)

- This project uses SQLite only, so no external database setup is required.
- Databases are already in the repo under `CmsObserver.API\`:
  - `cms-observer.db` (entities)
  - `cms-observer-users.db` (observer users)
- On startup, the API ensures both databases exist.
- The `.http` files already contain valid Basic auth headers for CMS ingestion and observer user/admin calls.

## Try the MCU event simulations

Use files in `CmsObserver.API\DataSimulations\` to send webhook batches.

Example from `03-TChalla-Fallen-King.http`:

```http
{
  "type": "publish",
  "id": "tchalla",
  "version": 1
},
{
  "type": "unPublish",
  "id": "tchalla",
  "version": 2
}
```

This simulates T'Challa first being available, then becoming unpublished (hidden from regular users).  

> [!IMPORTANT]
> R.I.P Chadwick Boseman


### Suggested flow:

1. Run [`01-Avengers-Assemble.http`](./CmsObserver.API/DataSimulations/01-Avengers-Assemble.http) to seed characters.
2. Run [`03-TChalla-Fallen-King.http`](./CmsObserver.API/DataSimulations/03-TChalla-Fallen-King.http) to apply an unpublish timeline event.
3. Open [`CmsObserver.API.http`](./CmsObserver.API/CmsObserver.API.http) and execute:
   - `GET /entities` as user
   - `GET /entities?includeUnpublished=true` as admin

> [!NOTE]
> `POST /cms/events` returns `202 Accepted`; processing is asynchronous, so wait a brief moment before querying.

## Main endpoints

- `POST /cms/events` - CMS webhook ingestion
- `GET /entities` - list active entities
- `GET /entities?includeUnpublished=true` - includes unpublished entities for admin role
- `POST /admin/entities/{id}/disable` - admin-only local disable

## Solution structure

- `CmsObserver.API` - ASP.NET Core host, authentication, endpoint mapping
- `CmsObserver.Managers` - event processing and application logic
- `CmsObserver.Accessors` - EF Core data access for entities
- `CmsObserver.Users` - Cross cutting observer user credentials store
- `CmsObserver.API.Tests` - API approval/integration tests
- 