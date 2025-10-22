# Seeds - Datos Iniciales (MVP TicketFlow)

Archivos CSV con datos de prueba para inicializar el sistema en entornos de desarrollo y demo.

---

## Propósito

Los seeds permiten:

1. **Desarrollo local**: Inicializar base de datos con datos coherentes para pruebas
2. **Testing**: Generar datasets predecibles para tests de integración
3. **Demos**: Mostrar funcionalidad del sistema con datos realistas
4. **Onboarding**: Nuevos desarrolladores pueden ver datos de ejemplo al levantar el proyecto

**Importante**: Estos seeds son **solo para desarrollo y demo**. En producción, los datos reales se crean vía API.

---

## Archivos Disponibles

### 1. USERS.csv

Usuarios de prueba del sistema (admin, agentes, clientes).

**Columnas**:

| Columna | Tipo | Descripción | Ejemplo |
|---------|------|-------------|---------|
| `Id` | string | Identificador único del usuario | `u001` |
| `Name` | string | Nombre completo del usuario | `Ana Garcia` |
| `Email` | string | Correo electrónico (único) | `ana.garcia@ticketflow.com` |
| `Role` | enum | Rol del usuario: `ADMIN`, `AGENT`, `CLIENT` | `AGENT` |
| `IsActive` | boolean | Estado activo/inactivo | `true` |

**Datos incluidos**:
- 1 administrador (`ADMIN`)
- 2 agentes (`AGENT`)
- 1 cliente (`CLIENT`)

**Notas**:
- Passwords no se incluyen en CSV (se deben generar al importar)
- En desarrollo, usar password temporal: `Demo123!`
- Emails usan dominio `@ticketflow.com` para staff, `@example.com` para clientes
- `CreatedAt` se genera automáticamente al momento de importar

---

### 2. TAGS.csv

Etiquetas predefinidas para categorizar tickets.

**Columnas**:

| Columna | Tipo | Descripción | Ejemplo |
|---------|------|-------------|---------|
| `Id` | string | Identificador único del tag | `t001` |
| `Name` | string | Nombre del tag (único) | `urgente` |
| `Color` | string | Color en formato hex (#RRGGBB) | `#ef4444` |

**Datos incluidos**:
- `login` (azul #3b82f6): Problemas de autenticación
- `urgente` (rojo #ef4444): Tickets de alta prioridad
- `pagos` (verde #10b981): Relacionados con facturación/pagos
- `bug` (naranja #f59e0b): Errores del sistema

**Notas**:
- Colors en formato hex web-safe
- Nombres en minúsculas sin espacios (slug-friendly)
- Se pueden agregar más tags vía API después de inicializar

---

## Cómo se Consumirán

### Enfoque Recomendado: Seeder Script en Backend

El backend incluirá un comando o endpoint para importar seeds:

```
# Opción 1: Comando CLI (recomendado)
dotnet run --project backend/src/Api -- seed --environment Development

# Opción 2: Endpoint admin (solo desarrollo)
POST /api/admin/seed
Authorization: Bearer <admin-token>
```

### Flujo de Importación

1. **Verificar entorno**: Solo permitir en `Development` o `Testing`
2. **Leer archivos CSV**: Desde `deploy/seeds/`
3. **Validar datos**: Verificar formato y restricciones (emails únicos, etc.)
4. **Insertar en DB**: Usando repositorios o queries directas
5. **Evitar duplicados**: Verificar existencia antes de insertar (por `Id` o `Email`)
6. **Generar passwords**: Para usuarios, usar hash seguro (BCrypt/Argon2)
7. **Log resultados**: Indicar cuántos registros se insertaron/omitieron

### Ejemplo de Implementación (Pseudocódigo)

```csharp
// Backend: Application/UseCases/SeedDatabaseUseCase.cs
public class SeedDatabaseUseCase {
  
  public async Task<Result> ExecuteAsync() {
    // 1. Verificar entorno
    if (_env.IsProduction()) {
      return Result.Fail("Seeding not allowed in production");
    }
    
    // 2. Importar usuarios
    var usersPath = Path.Combine(_seedsDir, "USERS.csv");
    var userRecords = CsvReader.Read(usersPath);
    
    foreach (var record in userRecords) {
      // Verificar si ya existe
      var exists = await _userRepo.ExistsByEmailAsync(record.Email);
      if (exists) continue;
      
      // Crear usuario con password temporal
      var user = new User {
        Id = record.Id,
        Name = record.Name,
        Email = record.Email,
        Role = Enum.Parse<Role>(record.Role),
        IsActive = bool.Parse(record.IsActive),
        PasswordHash = _hasher.Hash("Demo123!"),
        CreatedAt = DateTime.UtcNow
      };
      
      await _userRepo.CreateAsync(user);
    }
    
    // 3. Importar tags
    var tagsPath = Path.Combine(_seedsDir, "TAGS.csv");
    var tagRecords = CsvReader.Read(tagsPath);
    
    foreach (var record in tagRecords) {
      var exists = await _tagRepo.ExistsByNameAsync(record.Name);
      if (exists) continue;
      
      var tag = new Tag {
        Id = record.Id,
        Name = record.Name,
        Color = record.Color
      };
      
      await _tagRepo.CreateAsync(tag);
    }
    
    return Result.Ok("Seeding completed successfully");
  }
}
```

---

## Formato CSV

### Convenciones

- **Encoding**: UTF-8 sin BOM
- **Delimitador**: Coma (`,`)
- **Header**: Primera fila contiene nombres de columnas
- **Quotes**: No usar comillas a menos que el valor contenga comas
- **Newlines**: `\n` (LF) o `\r\n` (CRLF)

### Validación

Al importar, validar:

1. **Unicidad**: `Email` en USERS.csv, `Name` en TAGS.csv
2. **Enums**: `Role` debe ser `ADMIN|AGENT|CLIENT`
3. **Booleans**: `IsActive` debe ser `true|false`
4. **Color**: Formato `#RRGGBB` válido (6 dígitos hexadecimales)
5. **IDs**: No vacíos, formato consistente (`u001`, `t001`)

---

## Extensiones Futuras

### TICKETS.csv (Post-MVP)

Agregar tickets de prueba con asignaciones y estados:

```csv
Id,Code,Title,Status,Priority,CreatorId,AssignedTo,CreatedAt
tk001,TF-1001,Problema con login,IN_PROGRESS,HIGH,u004,u002,2025-10-20T10:00:00Z
tk002,TF-1002,Error en facturación,NEW,URGENT,u004,null,2025-10-21T08:30:00Z
```

### TEAMS.csv (Post-MVP)

Equipos con múltiples agentes:

```csv
Id,Name,AgentIds
team001,Soporte Nivel 1,"u002,u003"
team002,Escalación,"u001"
```

### TICKET_TAGS.csv (Post-MVP)

Relación muchos-a-muchos entre tickets y tags:

```csv
TicketId,TagId
tk001,t001
tk001,t002
tk002,t003
```

---

## Uso en Docker Compose

El seeding puede ejecutarse automáticamente al levantar contenedores:

```yaml
# deploy/docker-compose.yml
services:
  api:
    image: ticketflow-api:latest
    depends_on:
      - postgres
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
    command: ["sh", "-c", "dotnet Api.dll && dotnet Api.dll seed"]
```

O manualmente después de levantar:

```bash
docker-compose up -d
docker-compose exec api dotnet Api.dll seed
```

---

## Uso en Migraciones de EF Core

Alternativamente, usar `DbContext.OnModelCreating` con `HasData`:

```csharp
// Backend/Infrastructure/Persistence/EfCore/TicketFlowDbContext.cs
protected override void OnModelCreating(ModelBuilder modelBuilder) {
  base.OnModelCreating(modelBuilder);
  
  // Solo en Development
  if (_env.IsDevelopment()) {
    modelBuilder.Entity<User>().HasData(
      new User { Id = "u001", Name = "Carlos Admin", Email = "carlos.admin@ticketflow.com", ... }
    );
    
    modelBuilder.Entity<Tag>().HasData(
      new Tag { Id = "t001", Name = "login", Color = "#3b82f6" }
    );
  }
}
```

Pero el enfoque de CSV es más flexible y no requiere recompilar.

---

## Testing con Seeds

Los seeds también se usan en tests de integración:

```csharp
// Backend.Tests/Integration/TicketApiTests.cs
public class TicketApiTests : IClassFixture<WebApplicationFactory> {
  
  [Fact]
  public async Task CreateTicket_WithSeedData_ReturnsCreated() {
    // Arrange: Base de datos ya tiene usuarios y tags de seeds
    var client = _factory.CreateClient();
    var adminToken = await GetTokenForUser("carlos.admin@ticketflow.com");
    
    // Act: Crear ticket asignado a agente u002
    var response = await client.PostAsJsonAsync("/api/tickets", new {
      title = "Test ticket",
      assignedTo = "u002",
      tags = new[] { "t001" }
    });
    
    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Created);
  }
}
```

---

## Limpieza de Seeds

Para resetear la base de datos en desarrollo:

```bash
# Opción 1: Recrear base de datos
docker-compose down -v
docker-compose up -d
dotnet ef database update --project backend/src/Api
dotnet run --project backend/src/Api -- seed

# Opción 2: Endpoint de limpieza (solo desarrollo)
DELETE /api/admin/seed
Authorization: Bearer <admin-token>
```

---

## Referencias

- Ver `docs/data-model.md` para estructura completa de tablas
- Ver `deploy/env/backend.env.example` para configuración de entorno
- [CSV RFC 4180](https://tools.ietf.org/html/rfc4180) - Formato estándar CSV
- [Entity Framework Core - Data Seeding](https://docs.microsoft.com/en-us/ef/core/modeling/data-seeding)
