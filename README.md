# Tanks Game

Battle Tanks frontend and backend in one repository. The applications retain their own code, dependencies and configuration.

- frontend/: Angular, canvas, SignalStore, SignalR and MQTT client.
- backend/: ASP.NET Core API, SignalR hub, PostgreSQL/Redis integration and Docker services.

## Requirements

Node.js 24, npm, .NET SDK 10 and Docker with Compose.

## Run the backend

From the repository root, in the first terminal:

```text
cd backend
docker compose up -d --wait
dotnet run --no-launch-profile --urls http://localhost:5174
```

The API runs at http://localhost:5174. Migrations run during startup. PostgreSQL uses port 5434, Redis 6381 and EMQX uses ports 1883 and 8083. Keep this terminal open.

## Run the frontend

From the repository root, in a second terminal:

```text
cd frontend
npm ci
npm start -- --port 4206
```

Open http://localhost:4206. Register an account. Open an incognito window for a second player, create a room, join it with the second account and start the game as the room creator. Ctrl+C stops each application. The database volume persists when Docker services are stopped.

## Verification

From frontend/:

```text
npm run build
npm test -- --watch=false
```

From backend/:

```text
dotnet build
node scripts/test-system.mjs
```

The system test requires the API, Docker services and frontend dependencies to be running or installed. It creates test users and checks concurrent capacity, permissions, movement, chat and session revocation.

## Scope

This is a local, single-server demonstration. Active room membership is kept in memory and closes when the backend restarts. The original game computes collisions on the client. The default appsettings.json values are for local use. See backend/docs/revision.md for the course review and the load/profiling exercises that have not been executed.
