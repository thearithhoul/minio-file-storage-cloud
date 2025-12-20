# File Storage System (MinIO + .NET API)

Small playground project to experiment with a “file storage system” setup:

- **MinIO** for S3-compatible object storage (files/blobs)
- **SQL Server** for metadata (items/records)
- **C# ASP.NET Core** backend for basic CRUD + upload/download patterns

## What’s in this repo

- `docker-compose.yaml` — starts **MinIO** and **SQL Server**
- `backend/` — .NET backend project (referenced by `file-storage-system.sln`)
- `test-image.html` — a quick HTML page with an embedded base64 image (useful for simple manual testing)

## Prerequisites

- Docker (Docker Desktop is fine)
- .NET SDK (the build output suggests `net9.0`, so **.NET 9 SDK** is recommended)

## Quick start

### 1) Start MinIO + SQL Server

```bash
docker compose up -d
```

Services/ports:

- MinIO S3 API: `http://localhost:9000`
- MinIO Console: `http://localhost:9090`
- SQL Server: `localhost:1433`

Default credentials (from `docker-compose.yaml`):

- MinIO: `minioadmin` / `minioadmin`
- SQL Server (SA): `Str0ng!Passw0rd`

### 2) Create DB schema (items table)

How you create the `items` table depends on how the backend is implemented:

- If the backend uses **EF Core migrations**, run:

```bash
dotnet ef database update --project backend
```

- If you’re using **manual SQL**, run your `CREATE TABLE ...` script against the SQL Server container.

This repo includes a starter schema at `scripts/schema.sql` (adjust column names/types to match your backend model).
To apply it to the SQL Server container:

```bash
cat scripts/schema.sql | docker exec -i mssql /opt/mssql-tools/bin/sqlcmd -S localhost -U SA -P "Str0ng!Passw0rd"
```

Connection info (local):

- Server: `localhost,1433`
- User: `SA`
- Password: `Str0ng!Passw0rd`
- Recommended options: `TrustServerCertificate=True`

### 3) Run the .NET backend

```bash
dotnet run --project backend
```

If you prefer hot reload:

```bash
dotnet watch --project backend run
```

## Configuration (typical)

Most setups like this need:

- MinIO endpoint (e.g. `http://localhost:9000`)
- Access key / secret key
- Bucket name (e.g. `items`)
- SQL Server connection string

In ASP.NET Core, these are commonly provided via `appsettings.json` and/or environment variables like:

- `ConnectionStrings__DefaultConnection=Server=localhost,1433;User Id=SA;Password=Str0ng!Passw0rd;TrustServerCertificate=True;`
- `MINIO_ENDPOINT=http://localhost:9000`
- `MINIO_ACCESS_KEY=minioadmin`
- `MINIO_SECRET_KEY=minioadmin`
- `MINIO_BUCKET=items`

