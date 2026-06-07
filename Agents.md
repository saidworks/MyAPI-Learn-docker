# Agents Guide

This file provides quick orientation and working rules for coding agents contributing to this repository.

## Project Snapshot

- **Tech stack:** ASP.NET Core Web API, YARP gateway, Redis, SQL Server, Docker Compose
- **Main services:** `gateway`, `myapi1`, `myapi2`, `myapi3`, `redis`, `sqlserver`
- **Key entry point:** gateway exposed on `http://localhost:8080`

## Repository Map

- `MyAPI-Learn-K8S/` - Main API service (controllers, EF Core, Redis integration)
- `MyApiGateway/` - YARP reverse proxy and load balancer
- `docker-compose.yml` - Local orchestration of gateway, API replicas, Redis, and SQL Server
- `README.md` - Architecture explanation and setup notes

## Local Setup

1. Create a root-level `.env` file (same directory as `docker-compose.yml`) with:

```env
SA_PASSWORD=<YOUR_SQL_PASSWORD>
REDIS_CONNECTION=redis:6379
SQL_CONNECTION=Server=sqlserver,1433;Database=ProductDb;User Id=sa;Password=<YOUR_SQL_PASSWORD>;TrustServerCertificate=True;
```

2. Start the system:

```bash
docker compose up --build
```

## Useful Runtime Checks

- Gateway root:

```text
GET http://localhost:8080/
```

- API through gateway:

```text
GET http://localhost:8080/instance
GET http://localhost:8080/redis-test
GET http://localhost:8080/api/products
```

## Agent Working Rules

- Keep changes minimal and scoped to the issue.
- Update gateway configuration if API routing topology changes.
- Keep API replica behavior stateless and compatible with load balancing.
- Preserve cache-aside flow (`Redis` first, then SQL when cache misses).
- Do **not** commit secrets or a populated `.env` file.

## Validation Expectations

- For code changes, at minimum run a build for affected projects:

```bash
dotnet build MyAPI-Learn-K8S/MyAPI-Learn-K8S.csproj
dotnet build MyApiGateway/MyApiGateway.csproj
```

- If Docker-related behavior changes, validate with:

```bash
docker compose up --build
```
