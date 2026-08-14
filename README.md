# KadreeBank API

API bancaria para KadreeBank construida en **.NET 8** con **Clean Architecture**, **CQRS
(MediatR)** y **PostgreSQL**. Modela clientes naturales (cuentas de ahorro) y empresas
(cuentas corrientes), consignaciones/retiros, consulta de saldo y movimientos, extractos
mensuales, y dos reportes en tiempo real.

## Arquitectura

```
KadreeBank.Domain          Entidades, invariantes de negocio, interfaces de repositorio.
                            No depende de ninguna otra capa.

KadreeBank.Application      Casos de uso (CQRS: Commands/Queries + MediatR), DTOs,
                            validación (FluentValidation). Depende solo de Domain.

KadreeBank.Infrastructure   EF Core + Npgsql, repositorios, migraciones, reportes.
                            Implementa las interfaces de Domain/Application.

KadreeBank.API              Controllers, middleware de errores, composition root.
```

Regla de dependencias: `API → Infrastructure → Application → Domain`. Domain no conoce
a nadie; Application no conoce EF Core ni HTTP.

### Reglas de negocio

- Un cliente **natural** solo puede tener cuentas de **ahorro**; una **empresa** solo
  cuentas **corrientes** (validado en `Account.Create`).
- El saldo de una cuenta **nunca puede ser negativo** (`Account.Withdraw`).
- **Consistencia bajo concurrencia**: los depósitos/retiros se ejecutan dentro de una
  transacción que bloquea la fila de la cuenta (`SELECT ... FOR UPDATE`) antes de leer
  y mutar el saldo. Esto serializa cualquier par de operaciones concurrentes sobre la
  misma cuenta, evitando pérdidas de actualización (*lost updates*). Ver
  `AccountRepository.GetForUpdateAsync` y `DepositCommandHandler` / `WithdrawCommandHandler`.

### Por qué no hay AutoMapper

Se evaluó AutoMapper pero se descartó: la versión libre de vulnerabilidades conocidas
empuja hacia su modelo de licenciamiento comercial más reciente, y para DTOs planos como
los de este dominio el mapeo manual (`Common/Mappings/MappingExtensions.cs`) es más
explícito, no tiene *runtime reflection* y es trivial de depurar.

## Cómo levantar el proyecto

### Opción 1: Docker (recomendado)

```bash
docker-compose up --build
```

Levanta la API (puerto `8080`) y PostgreSQL (puerto `5432`), aplica las migraciones
automáticamente al iniciar y expone Swagger en `http://localhost:8080/swagger`.

### Opción 2: Local

1. Levanta solo la base de datos: `docker-compose up db`
2. `dotnet run --project src/KadreeBank.API` (usa `appsettings.json`, ya apunta a
   `localhost:5432` con las mismas credenciales del `docker-compose.yml`).

## Pruebas

```bash
# Unit tests (dominio + handlers, no requieren nada externo)
dotnet test tests/KadreeBank.UnitTests

# Integration tests (requieren Docker corriendo: levantan un Postgres real con Testcontainers)
dotnet test tests/KadreeBank.IntegrationTests
```

`AccountsConcurrencyTests` dispara depósitos y retiros en paralelo contra la misma
cuenta vía HTTP y verifica que el saldo final es exactamente el esperado — es la
evidencia automatizada de la regla de consistencia bajo concurrencia.

## Endpoints principales

| Método | Ruta | Descripción |
|---|---|---|
| POST | `/api/customers` | Crear cliente (natural o empresa) |
| GET | `/api/customers` | Listar clientes (`?type=Natural\|Business` opcional, filtra por tipo) |
| GET | `/api/customers/{id}` | Consultar cliente |
| POST | `/api/accounts` | Crear cuenta (valida tipo cliente ↔ tipo cuenta) |
| POST | `/api/accounts/{id}/deposits` | Consignación |
| POST | `/api/accounts/{id}/withdrawals` | Retiro |
| GET | `/api/accounts/{id}/balance` | Saldo actual |
| GET | `/api/accounts/{id}/transactions/recent?count=10` | Movimientos recientes |
| GET | `/api/accounts/{id}/statements/{year}/{month}` | Extracto mensual |
| GET | `/api/reports/customer-transaction-counts?year=&month=` | Clientes por # transacciones en el mes (desc.) |
| GET | `/api/reports/out-of-city-withdrawals?minAmount=1000000` | Clientes con retiros fuera de la ciudad de origen |

### Ejemplo de flujo completo

```bash
# 1. Crear cliente natural
curl -X POST http://localhost:8080/api/customers \
  -H "Content-Type: application/json" \
  -d '{"type":"Natural","fullName":"Ana Pérez","documentNumber":"CC-123"}'

# 2. Crear cuenta de ahorro (usar el id del paso anterior; el número de cuenta lo
#    genera el servidor, no se envía en el request)
curl -X POST http://localhost:8080/api/accounts \
  -H "Content-Type: application/json" \
  -d '{"customerId":"<id>","type":"Savings","originCity":"Bogotá"}'

# 3. Consignar
curl -X POST http://localhost:8080/api/accounts/<accountId>/deposits \
  -H "Content-Type: application/json" \
  -d '{"amount":500000,"city":"Bogotá"}'

# 4. Consultar saldo
curl http://localhost:8080/api/accounts/<accountId>/balance
```

Todos los errores de negocio (fondos insuficientes, cuenta no encontrada, tipo de
cuenta inválido, validación) se devuelven como `application/problem+json` con el
status HTTP correspondiente, no como error 500 genérico.
