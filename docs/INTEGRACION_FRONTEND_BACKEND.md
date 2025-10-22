# Integración Frontend-Backend

## ✅ Cambios Implementados

### 1. **Nuevos Archivos**
- `src/web/apps/demo-vanilla/api/client.ts` - Cliente HTTP con JWT
- `src/web/apps/demo-vanilla/api/mappers.ts` - Mapeo de DTOs backend→frontend
- `src/web/apps/demo-vanilla/api/tickets.ts` - Operaciones sobre tickets (PATCH, POST)
- `src/web/.env.local.example` - Configuración del backend

### 2. **Archivos Modificados**
- `src/web/apps/demo-vanilla/api/me.ts` - Ahora usa `GET /api/me`
- `src/web/apps/demo-vanilla/api/board.ts` - Ahora usa `GET /api/tickets`

### 3. **Características**
✅ JWT Bearer token desde `localStorage.getItem('jwt_token')`
✅ Headers con `Authorization: Bearer <token>`
✅ Mapeo de tipos: `TicketResponse` → `TicketDTO`
✅ Mapeo de tipos: `UserResponse` → `CurrentUser`
✅ Fallback a datos mock si hay error de conexión
✅ Manejo de errores 401 (limpia token automáticamente)
✅ Filtrado por scope (assigned/team/all)

---

## 🚀 Cómo Probar

### Paso 1: Iniciar el Backend

```powershell
cd src\server\backend\Api
dotnet run
```

Debería mostrar algo como:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
```

### Paso 2: Generar un JWT Token de Prueba

Para probar la integración, necesitas un JWT válido. Tienes 3 opciones:

#### **Opción A: Token Mock (más rápido)**
Usa jwt.io para generar un token con estos datos:

**Header:**
```json
{
  "alg": "HS256",
  "typ": "JWT"
}
```

**Payload:**
```json
{
  "sub": "u123",
  "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name": "testuser",
  "http://schemas.microsoft.com/ws/2008/06/identity/claims/role": "AGENT",
  "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress": "agent@test.com",
  "exp": 1735689600,
  "iss": "TicketFlowApi",
  "aud": "TicketFlowClient"
}
```

**Firma (Secret Key):**
```
SuperSecretKey123456789012345678901234567890
```

Copia el token resultante (JWT Token en la sección "Encoded").

#### **Opción B: Crear endpoint de login**
Implementa un endpoint temporal en el backend:
```csharp
app.MapPost("/api/auth/login", (string username) => {
    var token = GenerateJwtToken(username, "AGENT", "agent@test.com");
    return Results.Ok(new { token });
});
```

#### **Opción C: Token hardcoded en el código**
Si solo necesitas probar rápido, agrega en `client.ts`:
```typescript
export function getAuthToken(): string | null {
  // Token de prueba temporal
  return "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJ1MTIzIiwiaHR0cDovL3NjaGVtYXMueG1sc29hcC5vcmcvd3MvMjAwNS8wNS9pZGVudGl0eS9jbGFpbXMvbmFtZSI6InRlc3R1c2VyIiwiaHR0cDovL3NjaGVtYXMubWljcm9zb2Z0LmNvbS93cy8yMDA4LzA2L2lkZW50aXR5L2NsYWltcy9yb2xlIjoiQUdFTlQiLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9lbWFpbGFkZHJlc3MiOiJhZ2VudEB0ZXN0LmNvbSIsImV4cCI6MTczNTY4OTYwMCwiaXNzIjoiVGlja2V0Rmxvd0FwaSIsImF1ZCI6IlRpY2tldEZsb3dDbGllbnQifQ.xyz";
  // return localStorage.getItem('jwt_token'); // Descomentar después
}
```

### Paso 3: Configurar el Token en el Frontend

Abre la consola del navegador y ejecuta:
```javascript
localStorage.setItem('jwt_token', 'TU_TOKEN_AQUI');
```

### Paso 4: Iniciar el Frontend

```powershell
cd src\web
npm run dev
```

Abre http://localhost:5173

---

## 🔍 Verificación

### En la Consola del Navegador (F12)

#### ✅ Si todo funciona:
```
✅ Ticket TF-1 actualizado a en-proceso
```

#### ❌ Si hay error 401:
```
Error obteniendo usuario actual: Error: No autorizado. Token inválido o expirado.
[getMe:mock] Usando datos de fallback
```

#### ❌ Si el backend no está corriendo:
```
Error obteniendo tablero: TypeError: Failed to fetch
[getBoard:mock] Usando datos de fallback
```

### En la Pestaña Network (Red)

Deberías ver:
```
GET http://localhost:5000/api/me        200 OK
GET http://localhost:5000/api/tickets   200 OK
```

Con headers:
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR...
Content-Type: application/json
```

---

## 🐛 Troubleshooting

### Error: CORS Policy
**Problema:** `Access to fetch at 'http://localhost:5000/api/tickets' from origin 'http://localhost:5173' has been blocked by CORS policy`

**Solución:** Verifica que el backend tenga CORS configurado:
```csharp
builder.Services.AddCors(options => {
    options.AddPolicy("AllowFrontend", policy => {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

app.UseCors("AllowFrontend");
```

### Error: 401 Unauthorized
**Problema:** El token no es válido o expiró

**Solución:**
1. Genera un nuevo token con `exp` mayor a `Date.now() / 1000`
2. Verifica que el SecretKey sea el mismo en jwt.io y en `appsettings.Development.json`
3. Verifica que el `iss` y `aud` sean correctos

### Error: Cannot read property 'userId' of null
**Problema:** `currentUser` es null cuando se intenta filtrar tickets

**Solución:** Asegúrate de que `initSession()` se llame ANTES de `loadBoard()` en `main.ts`

### Error: 404 Not Found en /api/tickets
**Problema:** El backend no está mapeando los endpoints

**Solución:** Verifica que `TicketsEndpoints.Map(app)` esté en `Program.cs`

---

## 📊 Estructura de Datos

### Backend → Frontend Mapping

| Backend (TicketResponse) | Frontend (TicketDTO) |
|-------------------------|---------------------|
| `id: number`           | `id: string` (formato: TF-123) |
| `status: "OPEN"`       | `columnId: "nuevo"` |
| `status: "IN_PROGRESS"`| `columnId: "en-proceso"` |
| `status: "WAITING"`    | `columnId: "en-espera"` |
| `status: "RESOLVED"`   | `columnId: "resuelto"` |
| `assignedTo: string?`  | `assignee: {id, name}` |
| `priority: "HIGH"`     | `tags: [{id, label, color: red}]` |
| `createdAt: string`    | `updatedAt: string` |

### UserResponse → CurrentUser Mapping

| Backend | Frontend |
|---------|---------|
| `role: "AGENT"` | `role: "agent"` + scopes derivados |
| N/A | `teamIds` derivado del role |
| N/A | `scopes` según role (AGENT = all, CLIENT = limited) |

---

## 🎯 Próximos Pasos

1. **Implementar Login UI**: Crear formulario de login que llame a un endpoint `/api/auth/login`
2. **Persist Token**: El token ya se guarda en localStorage, pero agregar refresh token
3. **Handle Token Expiry**: Detectar 401 y redirigir a login
4. **Database Integration**: Conectar el backend a PostgreSQL en lugar de mock data
5. **Real-time Updates**: Implementar SignalR o WebSockets para notificaciones en tiempo real
6. **Error Boundaries**: Agregar UI para errores (toast notifications)

---

## 📝 Notas

- El frontend **siempre tiene fallback** a datos mock si el backend no responde
- El token se limpia automáticamente si el backend responde 401
- Los scopes y capabilities se mapean automáticamente según el rol del usuario
- El filtrado por scope (assigned/team/all) se hace en el frontend después de obtener todos los tickets
