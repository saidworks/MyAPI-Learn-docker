# Distributed ASP.NET Core Architecture with Docker Compose, YARP, Redis, and SQL Server

## Overview

This project demonstrates a local distributed system architecture using:

- ASP.NET Core Web API
- YARP Reverse Proxy / Load Balancer
- Redis Distributed Cache
- SQL Server Database
- Docker Compose orchestration
- Multiple API container replicas

The goal of this project is to simulate real-world cloud-native architecture patterns locally before moving to Kubernetes.

---

# Architecture Diagram

```text
Browser / Client
        │
        ▼
┌──────────────────────────────┐
│ YARP Reverse Proxy Gateway   │
│  - Entry Point               │
│  - Load Balancing            │
│  - Request Routing           │
└──────────────┬───────────────┘
               │
       Round Robin Distribution
               │
   ┌───────────┼───────────┐
   ▼           ▼           ▼
┌────────┐ ┌────────┐ ┌────────┐
│ API 1  │ │ API 2  │ │ API 3  │
│Replica │ │Replica │ │Replica │
└────┬───┘ └────┬───┘ └────┬───┘
     │           │           │
     └───────────┼───────────┘
                 │
                 ▼
        ┌────────────────┐
        │ Redis Cache    │
        │ Cache-Aside    │
        └──────┬─────────┘
               │
               ▼
      ┌───────────────────┐
      │ SQL Server DB     │
      │ Source of Truth   │
      └───────────────────┘
```

---

# Architecture Components

## 1. YARP Reverse Proxy / Load Balancer

YARP acts as the public entry point to the system.

### Responsibilities

- Reverse proxy
- Request routing
- Load balancing
- Traffic distribution
- Hiding internal API replicas

The browser never talks directly to API containers.

Instead:

```text
Browser → YARP → API replicas
```

YARP distributes requests using a `RoundRobin` load balancing policy.

---

## 2. ASP.NET Core API Replicas

The API application is deployed as multiple container replicas:

```text
myapi1
myapi2
myapi3
```

### Responsibilities

- Business logic
- Cache-aside implementation
- Redis communication
- SQL Server communication

The API is designed to be stateless so it can scale horizontally.

---

## 3. Redis Distributed Cache

Redis acts as a shared distributed cache across all API replicas.

### Pattern Used

- Cache-Aside Pattern

### Request Flow

1. API checks Redis cache
2. If found → return cached data
3. If missing → query SQL Server
4. Store result in Redis
5. Return response

### Benefits

- Reduced database load
- Faster response times
- Shared cache across replicas

---

## 4. SQL Server Database

SQL Server acts as the source of truth.

### Responsibilities

- Persistent storage
- Durable data
- Relational data management

A Docker volume is used to persist database files outside the container lifecycle.

---

# Docker Compose

Docker Compose orchestrates the entire local distributed system.

### Services

- Gateway
- API replicas
- Redis
- SQL Server

Compose also provides:

- Internal networking
- Service discovery
- Container lifecycle management

---

# Scaling

The project demonstrates horizontal scaling using multiple API replicas.

YARP distributes traffic across replicas using:

```json
"LoadBalancingPolicy": "RoundRobin"
```

---

# Cache-Aside Pattern

Example request flow:

```text
GET /api/products/1

1. API checks Redis
2. Cache miss
3. Query SQL Server
4. Store result in Redis
5. Return response

Next request:
1. API checks Redis
2. Cache hit
3. Return cached data
```

---

# Local Development vs Production

## Local Development

```text
API Containers
Redis Container
SQL Server Container
Docker Compose
```

## Production / Kubernetes

```text
API Pods
Azure Cache for Redis
Azure SQL Database
Kubernetes Services
Ingress / API Gateway
```

The same application code can be reused while infrastructure changes.

---

# Key Cloud-Native Concepts Demonstrated

- Containerization
- Stateless APIs
- Distributed caching
- Shared infrastructure services
- Horizontal scaling
- Reverse proxy architecture
- Load balancing
- Service-to-service communication
- Persistent volumes
- Externalized configuration
- Distributed system orchestration

---

# Technologies Used

- ASP.NET Core
- YARP Reverse Proxy
- Docker
- Docker Compose
- Redis
- SQL Server
- Entity Framework Core

---

# Environment Variables

This project uses a local `.env` file for sensitive configuration values such as database passwords and connection strings.

Create a `.env` file at the solution root level (same level as `docker-compose.yml`).

Example:

```env
SA_PASSWORD=<YOUR_SQL_PASSWORD>

REDIS_CONNECTION=redis:6379

SQL_CONNECTION=Server=sqlserver,1433;Database=ProductDb;User Id=sa;Password=<YOUR_SQL_PASSWORD>;TrustServerCertificate=True;
```

> Important:
> The `.env` file should NOT be committed to source control.

Make sure `.gitignore` contains:

```gitignore
.env
```

Each developer can create their own local credentials and configuration values.
---

# Running the Project

```powershell
docker compose up --build
```

