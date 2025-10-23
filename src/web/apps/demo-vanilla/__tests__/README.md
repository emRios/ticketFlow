# Tests Unitarios Frontend - TicketFlow

Suite de pruebas unitarias para el frontend de TicketFlow usando Vitest.

## 📋 Resumen

- **Framework**: Vitest 1.0.4
- **Environment**: happy-dom (DOM virtual)
- **Coverage**: @vitest/coverage-v8
- **Total de tests**: ~85+ tests

## 🏗️ Estructura

```
src/web/apps/demo-vanilla/__tests__/
├── setup.ts                      # Configuración global
├── api/
│   ├── apiClient.test.ts        # Cliente HTTP (22 tests)
│   └── tickets.test.ts          # API de tickets (18 tests)
├── state/
│   └── session.test.ts          # Gestión de sesión (25 tests)
└── utils/
    └── helpers.test.ts          # Utilidades (20 tests)
```

## 🧪 Cobertura de Tests

### API Client (22 tests)
✅ Gestión de tokens JWT
- Get/Set/Clear token en localStorage
- Inclusión automática en headers

✅ Gestión de User ID
- Persistencia en localStorage
- Header X-UserId

✅ Requests HTTP
- GET, POST, PATCH, DELETE
- Manejo de headers (Authorization, Content-Type)
- Body JSON serialization

✅ Manejo de errores
- ApiError para respuestas no exitosas
- Propagación de errores de red

### Tickets API (18 tests)
✅ Actualización de estado
- Normalización de estados (español/inglés)
- Estados soportados: nuevo, en-proceso, en-espera, resuelto
- Fallback a "nuevo" para estados inválidos
- Inclusión de comentarios

✅ Creación de tickets
- Datos mínimos (title, description)
- Prioridad opcional
- Retorno de ID del ticket creado

### Session State (25 tests)
✅ Token management
- Persistencia en localStorage
- Múltiples operaciones
- Clear independiente

✅ User ID management
- Soporte de user_id y ticketflow_userId
- Limpieza de datos relacionados (role, username, email)
- Persistencia dual

✅ Edge cases
- Strings vacíos
- Caracteres especiales
- Valores con espacios

### Utilities (20 tests)
✅ Validaciones
- Status normalization
- Priority validation
- Role validation
- Email validation

✅ Formateo
- Fechas (ISO strings)
- Truncate strings
- Capitalize

✅ Helpers
- URL parameter extraction
- Array utilities
- Object utilities

## 🚀 Ejecutar Tests

### Instalar dependencias
```powershell
cd src\web
npm install
```

### Ejecutar todos los tests
```powershell
npm test
```

### Modo watch (desarrollo)
```powershell
npm test -- --watch
```

### Ver UI interactiva
```powershell
npm run test:ui
```

### Generar reporte de cobertura
```powershell
npm run test:coverage
```

### Ejecutar tests específicos
```powershell
# Solo tests de apiClient
npm test -- apiClient.test.ts

# Solo tests de session
npm test -- session.test.ts

# Por patrón
npm test -- --grep "Token Management"
```

## 📊 Configuración

### vitest.config.ts
```typescript
{
  test: {
    globals: true,              // Funciones globales (describe, it, expect)
    environment: 'happy-dom',   // DOM virtual ligero
    setupFiles: ['...'],        // Configuración inicial
    coverage: {
      provider: 'v8',
      reporter: ['text', 'json', 'html'],
      exclude: [...],           // Archivos excluidos
    }
  }
}
```

### setup.ts
- Mock de `import.meta.env` para Vite
- Mock de `localStorage`
- Mock de `fetch` global
- Limpieza automática entre tests

## 🎯 Beneficios

1. **Detección temprana de bugs**: Catch errors antes de deployment
2. **Refactoring seguro**: Cambios con confianza
3. **Documentación**: Los tests documentan comportamiento esperado
4. **Calidad de código**: Fomenta funciones pequeñas y testeables
5. **CI/CD ready**: Integración en pipelines automática

## 📝 Convenciones

### Nombres de tests
```typescript
it('debería [acción] [condición]', () => {
  // Test
});
```

### Estructura AAA
```typescript
it('test description', () => {
  // Arrange - Preparar
  const data = { ... };
  
  // Act - Ejecutar
  const result = functionUnderTest(data);
  
  // Assert - Verificar
  expect(result).toBe(expected);
});
```

### Mocking
```typescript
// Mock de módulo completo
vi.mock('./module', () => ({
  funcName: vi.fn(),
}));

// Mock de función individual
const mockFn = vi.fn();
mockFn.mockResolvedValueOnce(data);
```

## 🔄 Integración Continua

### GitHub Actions
```yaml
- name: Install dependencies
  run: cd src/web && npm install

- name: Run tests
  run: cd src/web && npm test

- name: Upload coverage
  uses: codecov/codecov-action@v3
  with:
    files: ./src/web/coverage/coverage-final.json
```

## 🛠️ Próximos Pasos

### Tests pendientes
- [ ] Tests para `board-handlers.ts` (drag & drop logic)
- [ ] Tests para `board-loader.ts` (data fetching)
- [ ] Tests para `users.ts` API
- [ ] Tests para validaciones de formularios
- [ ] Tests de integración para flujos completos

### Mejoras
- [ ] Aumentar cobertura a >80%
- [ ] Agregar tests de snapshot para componentes UI
- [ ] Tests de performance
- [ ] Tests E2E con Playwright

## 📚 Recursos

- [Vitest Docs](https://vitest.dev/)
- [Testing Library](https://testing-library.com/)
- [Happy DOM](https://github.com/capricorn86/happy-dom)

---

**Última actualización**: Octubre 2025  
**Mantenido por**: Equipo TicketFlow
