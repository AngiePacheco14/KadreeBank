# KadreeBank API — Guía de arquitectura

Este documento explica **qué se construyó, por qué se construyó así, y cómo funciona
cada pieza**. Está pensado para que puedas defender el proyecto en una entrevista
técnica: no solo "qué hace el código" sino "qué problema resuelve" y "qué alternativas
se descartaron y por qué".

---

## 1. El problema, en una frase

KadreeBank necesita APIs para un banco: clientes con cuentas de ahorro/corrientes,
consignaciones/retiros, consulta de saldo y movimientos, extractos mensuales, y dos
reportes agregados. Los dos requisitos que realmente hacen el ejercicio interesante son:

1. **El saldo nunca puede quedar negativo.**
2. **El saldo debe ser consistente si dos operaciones llegan al mismo tiempo sobre la
   misma cuenta** (ej: dos retiros simultáneos que, mal manejados, podrían dejar el
   saldo incorrecto o incluso negativo).

Todo el diseño gira alrededor de resolver bien esos dos puntos, además de demostrar
buenas prácticas de arquitectura .NET.

---

## 2. Por qué Clean Architecture (y no "todo en un solo proyecto")

La idea central de Clean Architecture es una regla de dependencias: **el código que
representa las reglas del negocio no debe depender de detalles técnicos** (base de
datos, framework web, librerías externas). Es al revés: los detalles técnicos dependen
del negocio, nunca al contrario.

```
API  →  Infrastructure  →  Application  →  Domain
```

La flecha indica "depende de". `Domain` no tiene ninguna flecha saliente: es C# puro,
sin EF Core, sin ASP.NET, sin ninguna librería. Esto se ve reflejado literalmente en
`KadreeBank.Domain.csproj`, que no tiene **ningún** `<PackageReference>`.

**¿Por qué importa esto en la práctica?**
- Si mañana cambias PostgreSQL por SQL Server, o MediatR por otra cosa, el `Domain`
  no se entera. Solo cambia `Infrastructure`.
- La lógica de negocio (en este proyecto, dentro de los *Services* de Application —
  ver sección 5) se puede probar con tests unitarios rápidos, usando mocks de los
  repositorios, sin necesitar base de datos real ni HTTP.

---

## 3. Mapa de los 6 proyectos

| Proyecto | Tipo | Depende de | Responsabilidad |
|---|---|---|---|
| `KadreeBank.Domain` | Class Library | *nada* | Entidades (datos planos), interfaces de repositorio, excepciones de dominio |
| `KadreeBank.Application` | Class Library | Domain | **Services** (lógica de negocio + orquestación), Commands/Queries (CQRS) que los invocan, DTOs, validación |
| `KadreeBank.Infrastructure` | Class Library | Domain, Application | EF Core, PostgreSQL, repositorios, reportes SQL |
| `KadreeBank.API` | ASP.NET Core Web API | Domain, Application, Infrastructure | Controllers, HTTP, arranque de la app |
| `KadreeBank.UnitTests` | xUnit | Domain, Application | Pruebas de los Services con repositorios *mockeados* |
| `KadreeBank.IntegrationTests` | xUnit | API, Domain, Application | Pruebas contra la API real + PostgreSQL real (Testcontainers) |

`API` es el único proyecto que conoce a los otros cinco: es el **composition root**,
el lugar donde se "conectan los cables" (inyección de dependencias) — ver `Program.cs`.

---

## 4. `KadreeBank.Domain` — entidades planas + contratos

### 4.1 Entidades (sin lógica)

Las entidades son deliberadamente **anémicas**: solo propiedades públicas, sin
constructores que validen, sin métodos de negocio.

```csharp
// Account.cs
public class Account : BaseEntity
{
    public Guid CustomerId { get; set; }
    public string AccountNumber { get; set; } = default!;
    public AccountType Type { get; set; }
    public decimal Balance { get; set; }
    public string OriginCity { get; set; } = default!;
}
```

Igual para `Customer` (Type, FullName, DocumentNumber) y `Transaction` (AccountId,
Type, Amount, City, BalanceAfter, Timestamp). `BaseEntity` solo aporta `Id` y
`CreatedAt`.

**¿Dónde está entonces la regla "el saldo no puede quedar negativo" o "un cliente
natural solo puede tener cuenta de ahorro"?** En la capa Application, dentro de los
*Services* — ver sección 5.2. Las entidades son el **modelo de datos** que los
Services leen y modifican; no son responsables de protegerse a sí mismas.

### 4.2 Excepciones de dominio

`DomainException` (abstracta) → `InsufficientFundsException`, `InvalidAccountTypeException`,
`InvalidTransactionAmountException`, `NotFoundException`. Siguen viviendo en Domain
porque son parte del **vocabulario del negocio** (tipos de error con significado
propio), aunque ahora quien las lanza es un Service en Application, no la entidad.
El middleware de la API (sección 7.2) las traduce a códigos HTTP correctos.

### 4.3 Interfaces (contratos, no implementaciones)

`ICustomerRepository`, `IAccountRepository`, `ITransactionRepository`, `IUnitOfWork`
viven en Domain pero se **implementan** en Infrastructure. Es el patrón *Dependency
Inversion*: Domain define *qué* se necesita ("poder buscar una cuenta por id, y poder
bloquearla para actualizarla"), Infrastructure decide *cómo* ("con EF Core contra
Postgres"). Application los consume a través de la interfaz, nunca de la
implementación concreta.

---

## 5. `KadreeBank.Application` — Services + CQRS

Esta es la capa donde vive toda la lógica de negocio y la orquestación de casos de
uso. Tiene dos tipos de piezas que trabajan juntas:

```
Controller (API)  →  Handler (Command/Query)  →  Service  →  Repository (vía IUnitOfWork)
```

### 5.1 Los Handlers son delgados — solo traducen y delegan

Cada Command/Query tiene un Handler de una sola línea de lógica real: llama al Service
correspondiente y devuelve el resultado.

```csharp
// DepositCommandHandler.cs
public sealed class DepositCommandHandler(IAccountService accountService) : IRequestHandler<DepositCommand, BalanceDto>
{
    public Task<BalanceDto> Handle(DepositCommand request, CancellationToken cancellationToken) =>
        accountService.DepositAsync(request.AccountId, request.Amount, request.City, cancellationToken);
}
```

El Handler no valida reglas de negocio ni toca repositorios directamente — solo existe
para que MediatR pueda enrutar la request del controller hacia el Service correcto.
La validación de **formato** de la request (¿el monto viene, es un número, la ciudad no
está vacía?) sigue ocurriendo antes, en el pipeline de FluentValidation (sección 5.4);
la validación de **negocio** (¿alcanza el saldo?) ocurre dentro del Service.

### 5.2 Los Services — acá vive la lógica de negocio

`Application/Services/`:

| Service | Responsabilidad |
|---|---|
| `ICustomerService` / `CustomerService` | Crear y consultar clientes |
| `IAccountService` / `AccountService` | Crear cuentas, depósitos, retiros, saldo, movimientos, extracto mensual — **todas** las reglas de negocio de cuentas |
| `IReportService` / `ReportService` | Los dos reportes agregados (delega a `IReportQueries`, ver sección 6.4) |

Ejemplo — la regla "un cliente natural solo puede abrir cuenta de ahorro" vive en
`AccountService.CreateAccountAsync`:

```csharp
public async Task<AccountDto> CreateAccountAsync(Guid customerId, AccountType type, string accountNumber, string originCity, ...)
{
    var customer = await unitOfWork.Customers.GetByIdAsync(customerId, cancellationToken)
        ?? throw new NotFoundException(nameof(Customer), customerId);

    var expectedType = customer.Type == CustomerType.Natural ? AccountType.Savings : AccountType.Checking;
    if (type != expectedType)
        throw new InvalidAccountTypeException(customer.Type, type);

    var account = new Account { CustomerId = customer.Id, AccountNumber = accountNumber, Type = type, OriginCity = originCity, Balance = 0m };

    await unitOfWork.Accounts.AddAsync(account, cancellationToken);
    await unitOfWork.SaveChangesAsync(cancellationToken);

    return account.ToDto();
}
```

Y la regla "el saldo nunca puede quedar negativo" vive en `AccountService.WithdrawAsync`
(el mecanismo completo de concurrencia se explica en la sección 8, que también vive
acá):

```csharp
if (account.Balance - amount < 0)
    throw new InsufficientFundsException(account.Id, account.Balance, amount);

account.Balance -= amount;
```

**Los Services son el único lugar que llama a los repositorios** (a través de
`IUnitOfWork`). Ni los Handlers ni los Controllers tocan `IAccountRepository`
directamente — eso mantiene la regla de una sola responsabilidad: el Handler enruta,
el Service decide y persiste.

### 5.3 DTOs y por qué no hay AutoMapper

Los DTOs (`AccountDto`, `TransactionDto`, `BalanceDto`, etc.) son `record`s
inmutables en `*/Dtos/`. El mapeo Entity → DTO es manual y explícito en
`Common/Mappings/MappingExtensions.cs` (usado desde los Services):

```csharp
public static AccountDto ToDto(this Account account) =>
    new(account.Id, account.CustomerId, account.AccountNumber, account.Type, account.Balance, account.OriginCity, account.CreatedAt);
```

Se evaluó AutoMapper y se descartó a propósito: la versión sin vulnerabilidades
conocidas empuja hacia su modelo de licenciamiento comercial más reciente, y para
DTOs planos como estos el mapeo manual es más explícito, más rápido (sin reflection
en runtime) y más fácil de depurar.

### 5.4 El pipeline de validación (formato de la request)

```csharp
// ValidationBehavior.cs
public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, ...)
{
    var failures = ... ejecutar todos los validators de FluentValidation para TRequest ...
    if (failures.Count != 0) throw new ValidationException(failures);
    return await next();
}
```

MediatR permite registrar **pipeline behaviors**: código que corre automáticamente
*antes* de cualquier Handler. Así, ni el Handler ni el Service tienen que preocuparse
por "¿vino un monto negativo en el JSON?" — eso ya lo filtró FluentValidation. Lo que
sí queda para el Service es la validación que necesita **estado del negocio** (leer la
cuenta, comparar contra el saldo actual), que no se puede resolver solo mirando la
forma del request.

---

## 6. `KadreeBank.Infrastructure` — el mundo exterior

### 6.1 `KadreeBankDbContext`

Un `DbContext` estándar de EF Core con 3 `DbSet`. Las reglas de mapeo (nombres de
tabla, longitudes máximas, índices) están separadas en `Persistence/Configurations/`
(`IEntityTypeConfiguration<T>` por entidad) en vez de con atributos en las entidades —
así el Domain sigue sin saber nada de EF Core.

### 6.2 Repositorios + Unit of Work

Cada repositorio (`CustomerRepository`, `AccountRepository`, `TransactionRepository`)
implementa la interfaz definida en Domain usando EF Core. `UnitOfWork` los agrupa y
además expone `BeginTransactionAsync` / `CommitTransactionAsync` /
`RollbackTransactionAsync` — esto es clave para la sección 8. Quien los usa siempre es
un Service de Application, nunca un Handler ni un Controller directamente.

### 6.3 Migraciones

`Persistence/Migrations/` — generadas con `dotnet ef migrations add`. Se aplican
automáticamente al arrancar en Docker/Development (`Program.cs`,
`dbContext.Database.MigrateAsync()`), y explícitamente en los tests de integración.

### 6.4 Los reportes (`ReportQueries.cs`)

`ReportService` (Application) delega en `IReportQueries`, implementada acá con LINQ
directo contra el `DbContext` — no pasa por `IAccountRepository`/`ITransactionRepository`
porque un reporte no es una operación sobre un agregado, es una proyección que cruza
tres tablas.

**Reporte 1 — clientes por # de transacciones en un mes:**

```csharp
from t in dbContext.Transactions
where t.Timestamp >= fromUtc && t.Timestamp < toUtc
join a in dbContext.Accounts on t.AccountId equals a.Id
join c in dbContext.Customers on a.CustomerId equals c.Id
group c by new { c.Id, c.FullName } into g
orderby g.Count() descending
select new CustomerTransactionCountDto(g.Key.Id, g.Key.FullName, g.Count());
```

EF Core traduce esto a un `JOIN` + `GROUP BY` + `ORDER BY COUNT(*) DESC` real en
Postgres — no se traen los datos a memoria para contarlos en C#, importante porque el
enunciado pide reportes "en tiempo real" sobre una tabla de transacciones que en
producción puede ser enorme.

**Reporte 2 — retiros fuera de la ciudad de origen > $1.000.000:**

Detalle técnico interesante para contar en la entrevista: la primera versión intentaba
proyectar directo al DTO final con un `Where` después del `GroupBy().Select(...)`, y
EF Core 8 **no pudo traducir** esa combinación a SQL (*"Translation of 'Select' which
contains grouping parameter without composition is not supported"*). La solución fue
partir la consulta en dos pasos:

```csharp
// 1. Se agrega en el servidor (JOIN + GROUP BY + SUM, sí se traduce)
var aggregated = await (
    from t in dbContext.Transactions
    where t.Type == TransactionType.Withdrawal
    join a in dbContext.Accounts on t.AccountId equals a.Id
    where t.City != a.OriginCity
    join c in dbContext.Customers on a.CustomerId equals c.Id
    group t by new { c.Id, c.FullName } into g
    select new { g.Key.Id, g.Key.FullName, Total = g.Sum(x => x.Amount) })
    .ToListAsync(cancellationToken);

// 2. El filtro por monto mínimo y el orden se hacen en memoria
return aggregated
    .Where(r => r.Total > minAmount)
    .OrderByDescending(r => r.Total)
    .Select(r => new OutOfCityWithdrawalDto(r.Id, r.FullName, r.Total))
    .ToList();
```

Decisión consciente: el `GROUP BY`/`SUM` (la parte costosa) sí ocurre en la base de
datos; el filtro final ocurre en memoria sobre un conjunto ya reducido a "un registro
por cliente con retiros fuera de ciudad" — en la práctica, un puñado de filas. Esto se
descubrió gracias a los tests de integración contra Postgres real — los unit tests con
mocks nunca lo hubieran detectado, porque no ejecutan LINQ contra una base de datos
real.

---

## 7. `KadreeBank.API` — la puerta de entrada

### 7.1 Controllers delgados

Cada acción de cada controller (`CustomersController`, `AccountsController`,
`ReportsController`) arma el Command/Query, lo manda con `ISender.Send(...)`, devuelve
el resultado. **No hay lógica de negocio en los controllers**, y tampoco llaman a los
Services directamente — siempre pasan por MediatR, que enruta al Handler, que llama al
Service.

### 7.2 `ExceptionHandlingMiddleware`

Convierte las excepciones que lanzan los Services en respuestas HTTP consistentes
(`application/problem+json`):

| Excepción | Status HTTP |
|---|---|
| `NotFoundException` | 404 |
| `InsufficientFundsException` | 422 (Unprocessable Entity) |
| Otra `DomainException` | 400 |
| `FluentValidation.ValidationException` | 400 (con detalle por campo) |
| Cualquier otra | 500 (genérico, sin filtrar detalles internos) |

Así ningún error de negocio se ve como un 500 genérico — un 422 con mensaje claro dice
"tu operación no es válida, y por qué".

### 7.3 `Program.cs` (composition root)

Registra Swagger, `AddApplication()` (MediatR + FluentValidation + los tres Services),
`AddInfrastructure()` (DbContext + repositorios), el middleware de errores, y aplica
migraciones automáticamente al arrancar en Development/Docker.

---

## 8. El problema de concurrencia (la pregunta más probable en la entrevista)

### 8.1 El problema, concretamente

Dos requests de retiro llegan casi al mismo tiempo para la **misma cuenta**, que tiene
$100.000:

```
Request A: lee saldo = 100.000
Request B: lee saldo = 100.000
Request A: retira 80.000 → calcula nuevo saldo = 20.000 → guarda
Request B: retira 80.000 → calcula nuevo saldo = 20.000 → guarda
```

Resultado: se retiraron $160.000 de una cuenta que solo tenía $100.000. Esto se llama
**lost update** / race condition: la validación (`Balance - amount < 0`) es correcta,
pero si ambos requests la evalúan sobre el **mismo saldo leído antes de que el otro
escriba**, ambos pasan la validación.

### 8.2 La solución: bloqueo pesimista a nivel de fila (`SELECT ... FOR UPDATE`)

Todo el mecanismo vive dentro de `AccountService` (Application), usando el repositorio
a través de `IUnitOfWork`:

```csharp
// AccountRepository.cs (Infrastructure)
public Task<Account?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default) =>
    dbContext.Accounts
        .FromSqlInterpolated($"""SELECT * FROM accounts WHERE "Id" = {id} FOR UPDATE""")
        .FirstOrDefaultAsync(cancellationToken);
```

```csharp
// AccountService.cs (Application) — WithdrawAsync
await unitOfWork.BeginTransactionAsync(cancellationToken);
try
{
    var account = await unitOfWork.Accounts.GetForUpdateAsync(accountId, cancellationToken)
        ?? throw new NotFoundException(nameof(Account), accountId);

    if (account.Balance - amount < 0)
        throw new InsufficientFundsException(account.Id, account.Balance, amount);

    account.Balance -= amount;

    var transaction = new Transaction { AccountId = account.Id, Type = TransactionType.Withdrawal, Amount = amount, City = city, BalanceAfter = account.Balance };
    await unitOfWork.Transactions.AddAsync(transaction, cancellationToken);
    await unitOfWork.SaveChangesAsync(cancellationToken);
    await unitOfWork.CommitTransactionAsync(cancellationToken);

    return new BalanceDto(account.Id, account.Balance);
}
catch
{
    await unitOfWork.RollbackTransactionAsync(cancellationToken);
    throw;
}
```

**Cómo funciona, paso a paso:**

1. El Service abre una transacción explícita.
2. `SELECT ... FOR UPDATE` le pide a PostgreSQL: "dame esta fila, y **bloquéala** hasta
   que yo termine mi transacción". Si otra transacción intenta lo mismo sobre la misma
   fila, Postgres la hace **esperar**, no falla ni corrompe datos.
3. Cuando el Request A hace commit, el bloqueo se libera y el Request B (que estaba
   esperando) recién ahí lee el saldo — ya actualizado con el retiro de A.
4. B vuelve a evaluar `Balance - amount < 0` sobre el saldo **actualizado**, y si ya no
   alcanza, lanza `InsufficientFundsException` correctamente.

Las dos operaciones concurrentes sobre la misma cuenta quedan **serializadas** (una
espera a la otra), pero cuentas distintas no se bloquean entre sí — el lock es por
fila, no por tabla.

### 8.3 Por qué esta opción y no otra

| Estrategia | Por qué se descartó / no se usó como principal |
|---|---|
| **Optimistic concurrency** (token de versión, reintentar si cambió) | Requiere lógica de reintento y, bajo alta contención sobre la misma cuenta, genera muchos fallos y reintentos. Para operaciones financieras, pesimista es más simple y predecible. |
| **Lock a nivel de aplicación** (`lock`, `SemaphoreSlim` en memoria) | Solo funciona si la API corre en **una sola instancia**. Al escalar horizontalmente (2+ pods/contenedores), cada instancia tiene su propio lock y el problema vuelve. El lock de base de datos funciona sin importar cuántas instancias de la API haya. |
| **`SERIALIZABLE` isolation level** | Más pesado; bajo alta contención Postgres empieza a abortar transacciones con errores de serialización que hay que reintentar manualmente. `FOR UPDATE` es más quirúrgico. |

### 8.4 Cómo se verificó (no solo se afirma, se demuestra)

`tests/KadreeBank.IntegrationTests/AccountsConcurrencyTests.cs` dispara, contra una
API real corriendo sobre un Postgres real (Testcontainers), **30 requests HTTP en
paralelo** de depósitos y retiros sobre la misma cuenta, y verifica que el saldo final
es **exactamente** el esperado matemáticamente. Un segundo test dispara 5 retiros
concurrentes que en total exceden el saldo disponible y confirma que exactamente los
que caben se aprueban y el resto falla de forma controlada (422), sin que el saldo
quede negativo nunca.

---

## 9. Estrategia de testing

| Tipo | Proyecto | Qué prueba | Dependencias externas |
|---|---|---|---|
| Unit | `KadreeBank.UnitTests` | Los **Services** (`AccountServiceTests`, `CustomerServiceTests`, `ReportServiceTests`) con `IUnitOfWork`/repositorios *mockeados* (Moq) | Ninguna — corren en milisegundos |
| Integración | `KadreeBank.IntegrationTests` | La API completa (HTTP real) contra un PostgreSQL real levantado en un contenedor Docker efímero (Testcontainers) por cada corrida | Docker |

**Por qué toda la lógica de negocio se prueba a nivel de Service, y no de entidad ni
de Handler:** como las entidades son planas y los Handlers son solo delegación, el
único lugar donde vale la pena escribir un test unitario "interesante" (uno que pueda
fallar por una razón de negocio real) es el Service. Ejemplo:
`WithdrawAsync_ExceedingBalance_RollsBackAndThrowsInsufficientFunds` en
`AccountServiceTests.cs` prueba exactamente la regla de saldo no negativo, mockeando
el repositorio para no necesitar base de datos.

Los integration tests siguen siendo necesarios además de los unit tests porque:
- Prueban el bloqueo de fila real de PostgreSQL (`FOR UPDATE`), que un mock de
  `IAccountRepository` nunca podría simular de forma confiable.
- De hecho, el bug de traducción LINQ del reporte de retiros (sección 6.4) solo lo
  detectaron los integration tests, no los unit tests con mocks.

`CustomWebApplicationFactory` (en `tests/KadreeBank.IntegrationTests/Common/`) levanta
un contenedor de Postgres antes de cada corrida (`IAsyncLifetime.InitializeAsync`),
reemplaza la cadena de conexión de la app por la del contenedor, aplica las
migraciones, y lo destruye al final.

---

## 10. Docker

- `Dockerfile` (multi-stage): una etapa con el SDK completo compila y publica; la
  etapa final usa solo el runtime de ASP.NET (imagen más liviana).
- `docker-compose.yml`: levanta `db` (Postgres 16 con healthcheck) y `api` (espera a
  que `db` esté *healthy* antes de arrancar).
- `.dockerignore`: excluye `bin/`/`obj/` locales — sin esto, al copiar el código
  fuente al contenedor se sobrescriben los artefactos de compilación de Linux con los
  `obj/` de Windows del entorno local, causando errores de build difíciles de
  diagnosticar (pasó durante el desarrollo).

Con `docker-compose up --build` un evaluador levanta todo el sistema sin instalar
PostgreSQL, en un solo comando.

---

## 11. Resumen de decisiones de diseño (para responder "¿por qué elegiste X?")

| Decisión | Alternativa considerada | Por qué se eligió esta |
|---|---|---|
| **Entidades anémicas + Services con la lógica** | Entidades "ricas" con métodos de negocio (`Account.Withdraw()`) | Preferencia explícita del equipo: separar claramente "qué son los datos" de "qué hacen las reglas". Es el patrón *Transaction Script* / *Service Layer*, muy común en equipos .NET medianos, más fácil de seguir para alguien nuevo en el proyecto: toda la lógica de una operación está en un solo método del Service, no repartida entre la entidad y el handler. |
| CQRS (Commands/Queries) + Handlers delgados que llaman a un Service | Que el controller llame al Service directamente, sin MediatR | Mantiene una capa de indirección uniforme (todo pasa por el pipeline de validación) y un punto único de entrada por caso de uso, sin que los Services necesiten saber nada de MediatR |
| Mapeo manual | AutoMapper | Vulnerabilidad conocida en versiones recientes + explícito es más fácil de depurar para DTOs simples |
| `SELECT FOR UPDATE` (pesimista) en `AccountService` | Optimistic concurrency / lock en memoria | Correcto sin importar cuántas instancias de la API corran; predecible bajo alta contención |
| FluentValidation + pipeline behavior | Validación manual en cada Handler/Service | Evita repetir validación de formato en cada caso de uso; centraliza el formato de errores |
| Reportes con LINQ directo al `DbContext` (vía `IReportQueries`, detrás de `ReportService`) | Meter la lógica de reporte dentro de `IAccountRepository` | Los reportes cruzan 3 entidades y no representan operaciones sobre un agregado — forzarlos en un repositorio de agregado sería una abstracción incorrecta |
| Testcontainers para integration tests | Base de datos compartida de test / SQLite en memoria | Prueba contra el motor real (Postgres), incluyendo comportamiento específico como `FOR UPDATE`, que SQLite ni siquiera soporta igual |

### Nota honesta sobre "modelo anémico"

Si el entrevistador conoce DDD, puede señalar que un modelo anémico (entidades sin
comportamiento) es considerado por algunos un anti-patrón, porque dispersa el
conocimiento de "qué es válido" fuera del objeto que dice representarlo. La respuesta
defendible acá es: **no está disperso, está centralizado en un único Service por
agregado** (`AccountService` es la única puerta de entrada para mutar una `Account`;
nada más en la solución escribe `account.Balance` directamente fuera de ahí). El
trade-off real es "cohesión en el objeto" (DDD rico) vs. "cohesión en el caso de uso"
(Transaction Script) — ambos son válidos; este proyecto eligió el segundo de forma
consciente porque es lo que se pidió y es un patrón perfectamente defendible en
proyectos que no requieren un modelo de dominio muy profundo.

---

## 12. Preguntas típicas que te pueden hacer (y dónde está la respuesta en el código)

- **"¿Cómo garantizas que el saldo no quede negativo bajo concurrencia?"** → Sección 8,
  `AccountService.WithdrawAsync` + `AccountRepository.GetForUpdateAsync`.
- **"¿Por qué separaste en tantos proyectos?"** → Sección 2, regla de dependencias.
- **"¿Dónde está la validación de que un cliente natural no puede tener cuenta
  corriente?"** → `AccountService.CreateAccountAsync` (Application), sección 5.2.
- **"¿Cómo manejas errores?"** → `ExceptionHandlingMiddleware`, sección 7.2 —
  excepciones de dominio tipadas, cada una con su status HTTP correcto.
- **"¿Qué pasaría si la API escala a varias instancias?"** → El lock es a nivel de base
  de datos (Postgres), no en memoria del proceso, así que sigue siendo correcto sin
  importar cuántas instancias de la API corran (sección 8.3).
- **"¿Cómo probaste que la concurrencia funciona?"** → Sección 8.4,
  `AccountsConcurrencyTests.cs`, tests con requests HTTP paralelos reales.
- **"¿Por qué no usaste AutoMapper?"** → Sección 5.3.
- **"¿Dónde están tus Services, y qué hacen los Handlers entonces?"** → Sección 5.1 y
  5.2: los Handlers son puro enrutamiento (MediatR → Service correcto); toda la lógica
  de negocio y la orquestación con los repositorios vive en `AccountService`,
  `CustomerService` y `ReportService`.
- **"¿Por qué tus entidades no tienen comportamiento?"** → Ver la nota honesta al
  final de la sección 11: decisión consciente de Transaction Script sobre modelo de
  dominio rico, con la lógica centralizada en un Service por agregado.
