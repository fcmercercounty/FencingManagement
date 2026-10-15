# Fencing Club API

ASP.NET Core 8 Web API scaffold for fencing club management, built to run on Azure App Service
with Azure SQL. It includes user accounts with JWT auth, role-based access, CRUD over EF Core,
and push notifications through Azure Notification Hubs.

## What's in it

| Area | Where | Notes |
|---|---|---|
| Models (M) | `src/FencingClub.Api/Models` | `Club`, `Member`, `FencingClass`, `ApplicationUser`, roles |
| Controllers (C) | `src/FencingClub.Api/Controllers` | Auth, Clubs, Members, Classes, Notifications |
| Views (V) | `src/FencingClub.Api/Dtos` | Request/response shapes returned as JSON; entities never leave the API |
| Data | `src/FencingClub.Api/Data` | `AppDbContext` (Identity + domain tables), migrations, startup initializer |
| CRUD | `src/FencingClub.Api/Repositories` | Generic `IRepository<T>` with paging, used by every controller |
| Auth | `Services/JwtTokenService.cs`, `Program.cs` | ASP.NET Core Identity accounts, JWT bearer tokens, lockout after 5 bad logins |
| Push | `Services/NotificationHubPushService.cs` | Template pushes to iOS (APNs), Android (FCM v1) and Windows (WNS); logs instead when no hub is configured |
| Tests | `tests/FencingClub.Api.Tests` | Integration tests running the whole app against in-memory SQLite |
| Azure | `infra/main.bicep` | App Service, Azure SQL (Entra-only auth), Notification Hub, Key Vault |
| CI/CD | `.github/workflows/ci-cd.yml` | Build and test on every push; deploy `main` to App Service once configured |

### Roles

New sign-ups get **Member**. **Admin** and **Coach** together are "staff".

| Endpoint | Member | Coach | Admin |
|---|---|---|---|
| `GET /api/clubs`, `GET /api/classes` | yes | yes | yes |
| `POST/PUT/DELETE /api/clubs` | | | yes |
| `/api/members` (all verbs) | | yes | yes |
| `POST/PUT/DELETE /api/classes` | | yes | yes |
| `PUT/DELETE /api/notifications/installations` | yes | yes | yes |
| `POST /api/notifications/send` | | yes | yes |
| `POST /api/auth/users/{id}/roles` | | | yes |

The first admin is created at startup from `Seed:AdminEmail` / `Seed:AdminPassword` if both are set.

## Run locally

You need the .NET 8 SDK and a SQL Server. The quickest SQL Server is Docker:

```bash
docker run -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD='<a-strong-password>' -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest
```

Then set secrets (stored outside the repo) and run:

```bash
cd src/FencingClub.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=FencingClub;User Id=sa;Password=<a-strong-password>;TrustServerCertificate=True;"
dotnet user-secrets set "Jwt:SigningKey" "$(openssl rand -base64 48)"
dotnet user-secrets set "Seed:AdminEmail" "you@example.com"
dotnet user-secrets set "Seed:AdminPassword" "<an-admin-password>"
dotnet run
```

Open http://localhost:5078/swagger, call `POST /api/auth/login`, click **Authorize** and paste the token.
`FencingClub.Api.http` has ready-made requests for VS Code / Visual Studio / Rider.

The database schema is created on startup (`Database:Initialize = Migrate`). Run the tests with `dotnet test`.

### Changing the schema

```bash
dotnet tool restore
dotnet ef migrations add <Name> -p src/FencingClub.Api -o Data/Migrations
```

## Deploy to Azure

1. Create a resource group and deploy the infrastructure (fill in `infra/main.parameters.json` first):
   ```bash
   az group create -n fencingclub-rg -l eastus
   az deployment group create -g fencingclub-rg -f infra/main.bicep -p infra/main.parameters.json
   ```
2. Give the web app's managed identity access to the database. Connect to the database as the
   Entra admin (Azure Portal query editor or `sqlcmd`) and run, using the `webAppName` output:
   ```sql
   CREATE USER [<webAppName>] FROM EXTERNAL PROVIDER;
   ALTER ROLE db_datareader ADD MEMBER [<webAppName>];
   ALTER ROLE db_datawriter ADD MEMBER [<webAppName>];
   ALTER ROLE db_ddladmin  ADD MEMBER [<webAppName>];  -- lets startup migrations run; drop later if you migrate from CI
   ```
3. Connect the Notification Hub to Apple (APNs) and Google (FCM v1) credentials in the Azure Portal.
4. Deploy the code, either with `az webapp deploy` or by enabling the GitHub workflow: add an
   Entra app registration with a federated credential for the repo, then set secrets
   `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID` and variable `AZURE_WEBAPP_NAME`.

In Azure the app reads config from App Service settings (`Jwt__SigningKey` etc. use `__` for nesting);
secrets come from Key Vault references, and SQL uses the managed identity, so no passwords are stored.

## Push notifications from a client app

1. The app gets a push token from the OS (APNs device token, FCM registration token, or WNS channel URI).
2. On every launch, signed in, it calls `PUT /api/notifications/installations` with a stable
   `installationId`, the `platform` (`Apns`, `Fcm`, `Wns`), the token as `pushChannel`, and the `clubIds` it follows.
3. The server tags the device `user:{userId}` and `club:{clubId}`. Scheduling a class pushes to its club;
   staff can push to a user or a club with `POST /api/notifications/send`.

## Next steps worth considering

- Refresh tokens (access tokens currently last 60 minutes and the user signs in again after).
- Email confirmation and password reset (Identity's token providers are already registered).
- Per-club authorization, so a coach only manages their own club's members and classes.
- Tracking which user owns each push installation id before allowing overwrite/delete.
- Application Insights for logs and metrics.
