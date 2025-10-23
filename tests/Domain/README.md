# Tests Unitarios de Dominio - TicketFlow

Este proyecto contiene pruebas unitarias para la capa de dominio del sistema TicketFlow.

## 📋 Resumen

- **Framework de testing**: xUnit 2.6.2
- **Assertions**: FluentAssertions 6.12.0
- **Total de tests**: 79
- **Estado**: ✅ Todos los tests pasando

## 🏗️ Estructura

```
tests/Domain/
├── Entities/
│   ├── TicketTests.cs       # 22 tests - Entidad Ticket
│   └── UserTests.cs         # 23 tests - Entidad User
├── ValueObjects/
│   └── TicketStatusTests.cs # 7 tests - Value Object TicketStatus
├── Enums/
│   └── TicketPriorityTests.cs # 13 tests - Enum TicketPriority
├── GlobalUsings.cs
└── TicketFlow.Domain.Tests.csproj
```

## 🧪 Cobertura de Tests

### Entidad Ticket (22 tests)
- ✅ Creación con valores por defecto
- ✅ Creación con prioridad personalizada
- ✅ Normalización de prioridad a mayúsculas
- ✅ Generación de eventos de dominio (TicketCreatedEvent)
- ✅ Cambio de estado (ChangeStatus)
- ✅ Normalización de estados (español/inglés)
- ✅ Validación de estados vacíos
- ✅ Asignación de tickets (AssignTo)
- ✅ Generación de eventos de asignación
- ✅ Validación de IDs vacíos
- ✅ Limpieza de eventos (ClearDomainEvents)
- ✅ Flujo completo (crear → asignar → cambiar estado)

**Estados probados**:
- `nuevo`, `en-proceso`, `en-espera`, `resuelto`
- Variantes: `enproceso`, `en_proceso`, `in-progress`, `inprogress`, `resolved`, `closed`, `on-hold`, etc.

### Entidad User (23 tests)
- ✅ Creación con valores por defecto (rol AGENT)
- ✅ Creación con rol personalizado
- ✅ Normalización de roles a mayúsculas
- ✅ Validación de roles válidos (ADMIN, AGENT, CLIENT)
- ✅ Validación de roles inválidos (lanza excepción)
- ✅ Actualización de nombre
- ✅ Actualización de email
- ✅ Actualización de rol
- ✅ Actualización múltiple de campos
- ✅ Validación de valores vacíos en update
- ✅ Validación de roles inválidos en update
- ✅ Comportamiento con parámetros null
- ✅ Flujo completo (crear → actualizar múltiples veces)

### Value Object TicketStatus (7 tests)
- ✅ Creación de instancias estáticas (Todo, InProgress, Done)
- ✅ Comparación por valor (equality)
- ✅ Inmutabilidad (record type)
- ✅ ToString

### Enum TicketPriority (13 tests)
- ✅ Valores numéricos correctos (1-4)
- ✅ Comparación entre prioridades
- ✅ ToString
- ✅ Cast desde entero
- ✅ Ordenamiento por jerarquía
- ✅ Parse de nombres
- ✅ Parse case-insensitive
- ✅ GetValues (todos los valores)

## 🚀 Ejecutar Tests

### Ejecutar todos los tests
```powershell
cd tests\Domain
dotnet test
```

### Ejecutar con verbosidad
```powershell
dotnet test --verbosity normal
```

### Ejecutar tests específicos
```powershell
# Solo tests de Ticket
dotnet test --filter "FullyQualifiedName~TicketTests"

# Solo tests de User
dotnet test --filter "FullyQualifiedName~UserTests"

# Solo un test específico
dotnet test --filter "FullyQualifiedName~Create_DebeCrearTicketConValoresPorDefecto"
```

### Generar reporte de cobertura
```powershell
dotnet test --collect:"XPlat Code Coverage"
```

## 📊 Resultados Actuales

```
Resumen de pruebas: 
- Total: 79
- Con errores: 0
- Correcto: 79
- Omitido: 0
- Duración: ~3.6s
```

## 🎯 Beneficios de estos Tests

1. **Documentación viva**: Los tests documentan el comportamiento esperado de las entidades
2. **Regresión**: Detectan cambios no intencionados en el dominio
3. **Refactoring seguro**: Permiten modificar código con confianza
4. **Diseño**: Ayudan a validar que las entidades tienen una API clara
5. **CI/CD**: Se pueden ejecutar en pipelines de integración continua

## 🔧 Mantenimiento

### Agregar nuevos tests
1. Crear archivo `*Tests.cs` en la carpeta apropiada
2. Usar `[Fact]` para tests únicos
3. Usar `[Theory]` con `[InlineData]` para tests parametrizados
4. Seguir el patrón AAA (Arrange, Act, Assert)

### Ejemplo de test
```csharp
[Fact]
public void MetodoQueSeEstaTesteando_DebeHacerAlgo()
{
    // Arrange - Preparar datos
    var ticket = Ticket.Create("Título", "Descripción");
    
    // Act - Ejecutar acción
    ticket.ChangeStatus("en-proceso");
    
    // Assert - Verificar resultado
    ticket.Status.Should().Be("en-proceso");
}
```

## 📝 Convenciones

- Nombres de tests: `Metodo_DebeHacer_CuandoCondicion`
- Usar FluentAssertions para assertions legibles
- Un assert por concepto (pueden ser múltiples líneas del mismo concepto)
- Tests independientes (no deben depender del orden de ejecución)

## 🔄 Integración Continua

Estos tests se pueden integrar en GitHub Actions, Azure DevOps, o cualquier CI/CD:

```yaml
# Ejemplo GitHub Actions
- name: Run Domain Tests
  run: dotnet test tests/Domain/TicketFlow.Domain.Tests.csproj --verbosity normal
```

---

**Última actualización**: Octubre 2025  
**Mantenido por**: Equipo TicketFlow
