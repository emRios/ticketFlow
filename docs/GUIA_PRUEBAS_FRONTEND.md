# 🧪 Guía de Pruebas - Frontend + Backend Integration

## 🚀 Estado Actual

✅ **Frontend:** Corriendo en http://localhost:5173  
⚠️ **Backend:** Necesita iniciarse en http://localhost:5000  
✅ **JWT:** Configurado para leer desde localStorage  
✅ **CORS:** Configurado para localhost:5173  
✅ **API Client:** `apiFetch()` listo para usar  

---

## 📋 Paso 1: Iniciar el Backend

Abre una **nueva terminal** y ejecuta:

```powershell
cd "C:\Users\HP\Documents\PRUEBAS\SLC TRADE\TicketFlow\src\server\backend\Api"
dotnet run
```

Deberías ver:

```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
```

---

## 📋 Paso 2: Generar Token JWT de Prueba

### Opción A: Usar jwt.io (Más Rápido)

1. Ve a https://jwt.io
2. En la sección **PAYLOAD**, pega:

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

3. En la sección **VERIFY SIGNATURE**, pega la clave secreta:

```
SuperSecretKey123456789012345678901234567890
```

4. Copia el **JWT Token** generado (en la sección "Encoded")

### Opción B: Token de Prueba Pre-generado

Usa este token (válido hasta diciembre 2025):

```
eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJ1MTIzIiwiaHR0cDovL3NjaGVtYXMueG1sc29hcC5vcmcvd3MvMjAwNS8wNS9pZGVudGl0eS9jbGFpbXMvbmFtZSI6InRlc3R1c2VyIiwiaHR0cDovL3NjaGVtYXMubWljcm9zb2Z0LmNvbS93cy8yMDA4LzA2L2lkZW50aXR5L2NsYWltcy9yb2xlIjoiQUdFTlQiLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9lbWFpbGFkZHJlc3MiOiJhZ2VudEB0ZXN0LmNvbSIsImV4cCI6MTczNTY4OTYwMCwiaXNzIjoiVGlja2V0Rmxvd0FwaSIsImF1ZCI6IlRpY2tldEZsb3dDbGllbnQifQ.Bx5LrqGvZ8K0RjH3Hf8fqP5gZ5Kx7pW9qT3nR4vS6lM
```

---

## 📋 Paso 3: Configurar el Token en el Frontend

### Método 1: Usando la Consola del Navegador

1. Abre http://localhost:5173 en tu navegador
2. Abre las **DevTools** (F12)
3. Ve a la pestaña **Console**
4. Ejecuta:

```javascript
localStorage.setItem('jwt_token', 'TU_TOKEN_AQUI');
```

5. Recarga la página (F5)

### Método 2: Usando un Script

Crea un botón temporal en el HTML para facilitar las pruebas:

```html
<!-- Agregar temporalmente en index.html -->
<button onclick="setTestToken()">🔑 Configurar Token de Prueba</button>

<script>
function setTestToken() {
  const token = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...';
  localStorage.setItem('jwt_token', token);
  alert('✅ Token configurado. Recargando...');
  location.reload();
}
</script>
```

---

## 🧪 Pruebas Disponibles

### ✅ Prueba 1: Verificar la Conexión con el Backend

**En la consola del navegador (F12 → Console):**

```javascript
// Test 1: Health check (público, no requiere JWT)
fetch('http://localhost:5000/api/tickets')
  .then(r => r.json())
  .then(d => console.log('✅ Backend respondiendo:', d))
  .catch(e => console.error('❌ Error:', e));

// Test 2: Verificar token JWT
fetch('http://localhost:5000/api/me', {
  headers: { 'Authorization': 'Bearer ' + localStorage.getItem('jwt_token') }
})
  .then(r => r.json())
  .then(d => console.log('✅ Usuario autenticado:', d))
  .catch(e => console.error('❌ Error de auth:', e));
```

### ✅ Prueba 2: Cargar Tickets desde el Backend

**En la consola del navegador:**

```javascript
// Importar la función desde el módulo (si está disponible)
// O copiar este código directamente:

async function testGetTickets() {
  try {
    const response = await fetch('http://localhost:5000/api/tickets', {
      headers: { 
        'Authorization': 'Bearer ' + localStorage.getItem('jwt_token'),
        'Content-Type': 'application/json'
      }
    });
    
    if (!response.ok) {
      throw new Error(`HTTP ${response.status}: ${response.statusText}`);
    }
    
    const tickets = await response.json();
    console.log('✅ Tickets obtenidos:', tickets);
    console.table(tickets);
  } catch (error) {
    console.error('❌ Error:', error);
  }
}

testGetTickets();
```

### ✅ Prueba 3: Crear un Ticket

**En la consola del navegador:**

```javascript
async function testCreateTicket() {
  try {
    const response = await fetch('http://localhost:5000/api/tickets', {
      method: 'POST',
      headers: { 
        'Authorization': 'Bearer ' + localStorage.getItem('jwt_token'),
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        title: 'Ticket de prueba desde frontend',
        description: 'Este ticket fue creado desde la consola del navegador',
        priority: 'HIGH'
      })
    });
    
    if (!response.ok) {
      throw new Error(`HTTP ${response.status}: ${response.statusText}`);
    }
    
    const result = await response.json();
    console.log('✅ Ticket creado:', result);
  } catch (error) {
    console.error('❌ Error:', error);
  }
}

testCreateTicket();
```

### ✅ Prueba 4: Actualizar Estado de un Ticket

**En la consola del navegador:**

```javascript
async function testUpdateTicket(ticketId = 1) {
  try {
    const response = await fetch(`http://localhost:5000/api/tickets/${ticketId}/status`, {
      method: 'PATCH',
      headers: { 
        'Authorization': 'Bearer ' + localStorage.getItem('jwt_token'),
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        status: 'IN_PROGRESS'
      })
    });
    
    if (!response.ok) {
      throw new Error(`HTTP ${response.status}: ${response.statusText}`);
    }
    
    console.log('✅ Ticket actualizado correctamente');
  } catch (error) {
    console.error('❌ Error:', error);
  }
}

testUpdateTicket(1); // Cambia el ID según tus tickets
```

### ✅ Prueba 5: Verificar Usuario Actual (GET /api/me)

**En la consola del navegador:**

```javascript
async function testGetMe() {
  try {
    const response = await fetch('http://localhost:5000/api/me', {
      headers: { 
        'Authorization': 'Bearer ' + localStorage.getItem('jwt_token')
      }
    });
    
    if (!response.ok) {
      throw new Error(`HTTP ${response.status}: ${response.statusText}`);
    }
    
    const user = await response.json();
    console.log('✅ Usuario actual:', user);
    console.log('👤 UserID:', user.userId);
    console.log('🎭 Role:', user.role);
    console.log('📧 Email:', user.email);
  } catch (error) {
    console.error('❌ Error:', error);
  }
}

testGetMe();
```

---

## 🎨 Prueba 6: Usar el API Client del Frontend

Si el frontend tiene las funciones integradas, puedes probarlas directamente:

**En la consola del navegador:**

```javascript
// Asumiendo que las funciones están disponibles globalmente o en un módulo

// Probar getMe()
// Si no está disponible globalmente, tendrás que importarlo en el código
```

**O agregar temporalmente en main.ts:**

```typescript
import { getMe } from './api/me';
import { getBoard } from './api/board';
import { createTicket, updateTicketStatus } from './api/tickets';

// Hacer las funciones globales para testing
(window as any).testGetMe = getMe;
(window as any).testGetBoard = getBoard;
(window as any).testCreateTicket = createTicket;
(window as any).testUpdateTicketStatus = updateTicketStatus;
```

Luego en la consola:

```javascript
// Test usuario actual
await testGetMe();

// Test obtener tablero
await testGetBoard('assigned');

// Test crear ticket
await testCreateTicket({
  title: 'Bug en el sistema',
  description: 'Descripción del bug',
  priority: 'HIGH'
});

// Test actualizar ticket
await testUpdateTicketStatus('TF-1', 'en-proceso');
```

---

## 🔍 Verificación en Network Tab

1. Abre **DevTools** (F12)
2. Ve a la pestaña **Network**
3. Filtra por **Fetch/XHR**
4. Ejecuta cualquiera de las pruebas anteriores
5. Deberías ver las peticiones HTTP con:
   - ✅ Status 200 (éxito) o 201 (creado)
   - ✅ Header `Authorization: Bearer <token>`
   - ✅ Header `Content-Type: application/json`
   - ✅ Response body con los datos

---

## 🐛 Troubleshooting

### ❌ Error: CORS Policy

**Mensaje:**
```
Access to fetch at 'http://localhost:5000/api/tickets' from origin 'http://localhost:5173' 
has been blocked by CORS policy
```

**Solución:**
Verifica que el backend tenga CORS configurado en `Program.cs`:

```csharp
builder.Services.AddCors(options => {
    options.AddPolicy("AllowFrontend", policy => {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

app.UseCors("AllowFrontend");
```

### ❌ Error: 401 Unauthorized

**Mensaje:**
```
HTTP 401: Unauthorized
```

**Solución:**
1. Verifica que el token esté en localStorage: `localStorage.getItem('jwt_token')`
2. Genera un nuevo token con fecha de expiración futura
3. Verifica que la clave secreta sea la misma en el backend y jwt.io

### ❌ Error: Failed to fetch

**Mensaje:**
```
TypeError: Failed to fetch
```

**Solución:**
1. Verifica que el backend esté corriendo: `http://localhost:5000`
2. Verifica que no haya firewall bloqueando el puerto
3. Revisa los logs del backend para ver si la petición llegó

### ❌ Error: Cannot read properties of null

**Mensaje:**
```
Cannot read properties of null (reading 'userId')
```

**Solución:**
El usuario no se cargó correctamente. Verifica:
1. Que `initSession()` se ejecute antes de cargar el tablero
2. Que el token JWT sea válido
3. Que el endpoint `/api/me` responda correctamente

---

## 📊 Resultados Esperados

### ✅ GET /api/tickets

```json
[
  {
    "id": 1,
    "title": "Configurar autenticación JWT",
    "description": "Implementar autenticación con tokens JWT",
    "status": "IN_PROGRESS",
    "priority": "HIGH",
    "assignedTo": "juan.perez",
    "createdAt": "2025-10-22T10:30:00",
    "updatedAt": "2025-10-22T11:00:00"
  },
  {
    "id": 2,
    "title": "Diseñar base de datos",
    "description": "Crear esquema inicial de la base de datos",
    "status": "OPEN",
    "priority": "MEDIUM",
    "assignedTo": null,
    "createdAt": "2025-10-22T09:00:00",
    "updatedAt": null
  }
]
```

### ✅ GET /api/me

```json
{
  "userId": "u123",
  "username": "testuser",
  "role": "AGENT",
  "email": "agent@test.com"
}
```

### ✅ POST /api/tickets

```json
{
  "id": 5
}
```

### ✅ PATCH /api/tickets/{id}/status

```
(No body - Status 204 No Content)
```

---

## 🎯 Checklist de Pruebas

- [ ] ✅ Backend corriendo en http://localhost:5000
- [ ] ✅ Frontend corriendo en http://localhost:5173
- [ ] ✅ Token JWT configurado en localStorage
- [ ] ✅ GET /api/tickets responde con array de tickets
- [ ] ✅ GET /api/me responde con información del usuario
- [ ] ✅ POST /api/tickets crea un nuevo ticket
- [ ] ✅ PATCH /api/tickets/{id}/status actualiza el estado
- [ ] ✅ Headers Authorization y Content-Type se envían correctamente
- [ ] ✅ No hay errores CORS
- [ ] ✅ Network tab muestra status 200/201/204
- [ ] ✅ Console muestra logs de apiClient

---

## 🚀 Siguiente Paso: Integración Completa

Una vez que hayas verificado que todos los endpoints funcionan, el siguiente paso es:

1. **Conectar el tablero Kanban** con `getBoard()` para mostrar los tickets reales
2. **Implementar drag & drop** para mover tickets y llamar a `updateTicketStatus()`
3. **Agregar formulario** para crear tickets con `createTicket()`
4. **Mostrar información del usuario** en el header usando `getMe()`

---

**¿Todo funcionando?** 🎉 
Si ves los datos mock del backend en la consola, ¡estás listo para integrar el frontend completo!

**¿Hay errores?** 🐛
Revisa la sección de Troubleshooting o comparte el error para ayudarte.
