# Vulnerable API Lab

This is my API security lab built with ASP.NET Core and PostgreSQL. Each exercise uses fictional data and runs only on my local machine. Deliberately unsafe examples live in the tests; the running API does not expose those routes.

## Run locally

You need .NET 10, Docker Desktop with Linux containers, and PowerShell. From the repository root:

```powershell
if (-not (Test-Path .env)) {
    "LAB_DB_PASSWORD=$([guid]::NewGuid().ToString('N'))" | Set-Content -NoNewline .env
}
docker compose up -d --wait db
$passwordLine = Get-Content .env | Where-Object { $_ -match '^LAB_DB_PASSWORD=' } | Select-Object -First 1
$labPassword = $passwordLine.Substring('LAB_DB_PASSWORD='.Length)
$env:LAB_DB_CONNECTION_STRING = "Host=127.0.0.1;Port=5433;Database=vulnerable_api_lab;Username=lab_user;Password=$labPassword"
dotnet test tests/VulnerableApiLab.Api.Tests/VulnerableApiLab.Api.Tests.csproj
dotnet run --project src/VulnerableApiLab.Api/VulnerableApiLab.Api.csproj --launch-profile local
```

In another terminal, `Invoke-RestMethod http://127.0.0.1:5080/health` should return `status: ok`. An anonymous request to `/api/notes/1` should return 401 in the local profile. PostgreSQL binds to `127.0.0.1:5433`, and the API binds to `127.0.0.1:5080`.

The [seed script](db/seed.sql) runs when the database volume is first created. To update an older volume without deleting its data:

```powershell
Get-Content -Raw db/seed.sql | docker compose exec -T db psql -v ON_ERROR_STOP=1 -U lab_user -d vulnerable_api_lab
```

The `.env` file stays out of Git. Use only fictional accounts and passwords. Do not expose this lab to a public network.

## What the lab covers

- [SQL injection](tests/VulnerableApiLab.Api.Tests/SqlInjectionTests.cs): a test compares a concatenated query over fictional notes with the parameterized lookup in [NoteLookup.cs](src/VulnerableApiLab.Api/NoteLookup.cs). The lookup has no HTTP route.
- [Authentication](tests/VulnerableApiLab.Api.Tests/AuthenticationTests.cs): local registration and login store salted password hashes. An incorrect password returns 401; a valid session is checked through `/api/auth/me`.
- [Object authorization](tests/VulnerableApiLab.Api.Tests/ObjectAuthorizationTests.cs): a test shows that looking up a note by ID alone can retrieve another user's note. The corrected route uses the session identity and the owner stored in the database: the owner gets 200, another user gets 404, and an anonymous caller gets 401.
- [Login rate limiting](tests/VulnerableApiLab.Api.Tests/ApiAbuseTests.cs): local login allows three requests per IP in five seconds. Excess requests get 429, and access resumes after the window expires.
- [Error handling](tests/VulnerableApiLab.Api.Tests/SecurityLayersTests.cs): a simulated failure returns a generic 500 without copying diagnostic details into the response or captured logs.

Authentication and note reading exist only in `Development` and accept loopback requests only. In `Production`, authentication routes are absent and note reading remains closed. Note creation, updates, and deletion still return 403. PostgreSQL tests require `LAB_DB_CONNECTION_STRING`; without it, those tests are skipped.

## Known limits

The database role `lab_user` still has superuser privileges. Separating setup and runtime roles remains open work. Login rate limiting is in memory, groups users by IP, and does not cover registration. Session revocation and write authorization have not been tested. The dependency audit could not finish because the NuGet advisory source was unavailable. The tests establish the described behavior under local conditions, not overall API security.
