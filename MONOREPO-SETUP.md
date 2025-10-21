# TicketFlow - Estructura del Monorepo

## ✅ Árbol del Directorio Final

```
TicketFlow/
├── .editorconfig                    # Placeholder para reglas de formateo
├── .gitignore                       # (mantenido en raíz)
├── README.md                        # (mantenido en raíz)
├── instrucciones.MD                 # (mantenido en raíz)
├── reorganize-monorepo.ps1          # Script de reorganización ejecutado
│
├── .github/
│   └── workflows/
│       └── ci.yml                   # Placeholder para CI/CD pipeline
│
├── backend/
│   ├── README.md                    # API REST y lógica de negocio (DDD + Hexagonal)
│   ├── src/
│   │   ├── Domain/                  # Entidades, Value Objects, Domain Services
│   │   ├── Application/             # Use Cases, Commands, Queries, DTOs
│   │   ├── Infrastructure/          # Repositories, External Services, Outbox
│   │   └── Api/                     # Controllers, Middleware, Routing
│   └── tests/
│       ├── Domain/                  # Unit tests del dominio
│       ├── Application/             # Unit tests de casos de uso
│       └── Integration/             # Integration tests (API, DB)
│
├── worker/
│   ├── README.md                    # Procesamiento asíncrono de eventos
│   ├── src/                         # Consumers de RabbitMQ, Jobs
│   └── tests/                       # Unit tests del worker
│
├── frontend/                        # 🆕 Frontend movido aquí
│   ├── README.move-notes.md         # TODOs y notas del movimiento
│   ├── package.json                 # Dependencias del frontend
│   ├── package-lock.json
│   ├── vite.config.ts               # Configuración de Vite
│   ├── tsconfig.json                # Configuración de TypeScript
│   ├── index.html                   # HTML raíz (no usado, apps/demo-vanilla tiene el propio)
│   ├── script.js                    # Script viejo (no usado actualmente)
│   ├── style.css                    # Estilo viejo (no usado actualmente)
│   ├── node_modules/                # Dependencias instaladas
│   ├── apps/
│   │   └── demo-vanilla/            # Aplicación demo del kanban
│   │       ├── index.html           # HTML de la app
│   │       ├── main.ts              # Entry point con handlers
│   │       └── state.ts             # Estado de demo (tickets, tags, assignees)
│   └── packages/
│       ├── domain-core/             # Lógica de dominio pura (Ticket entities, events)
│       │   ├── types.ts             # Tipos del dominio
│       │   └── index.ts             # Funciones del dominio
│       ├── board-core/              # Helpers de estado del tablero
│       │   └── index.ts             # applyMove, applyReorder, etc.
│       └── board-adapter-vanilla/   # Adaptador UI con HTML5 Drag & Drop
│           ├── index.ts             # Renderizado y event handlers
│           └── styles.css           # Estilos del tablero
│
├── contracts/
│   └── README.md                    # DTOs, eventos y contratos compartidos
│
├── docs/
│   └── README.md                    # Arquitectura, diagramas, ADRs
│
└── deploy/
    ├── docker-compose.yml           # Placeholder para orquestación de servicios
    └── env/
        ├── backend.env.example      # Placeholder vars de backend
        ├── worker.env.example       # Placeholder vars de worker
        └── frontend.env.example     # Placeholder vars de frontend
```

---

## 📦 Resumen del Movimiento

### ✅ Archivos movidos a `frontend/`:
- ✅ `index.html`
- ✅ `package.json` + `package-lock.json`
- ✅ `vite.config.ts` + `tsconfig.json`
- ✅ `script.js` + `style.css` (legacy, no usados actualmente)
- ✅ `apps/` (demo-vanilla app)
- ✅ `packages/` (domain-core, board-core, board-adapter-vanilla)
- ✅ `node_modules/`

### ✅ Archivos mantenidos en raíz:
- ✅ `.gitignore`
- ✅ `README.md`
- ✅ `instrucciones.MD`

### ✅ Estructura nueva creada:
- ✅ `backend/` con src (Domain, Application, Infrastructure, Api) + tests
- ✅ `worker/` con src + tests
- ✅ `contracts/` para DTOs compartidos
- ✅ `docs/` para documentación
- ✅ `deploy/` con docker-compose.yml + env examples
- ✅ `.github/workflows/` con ci.yml placeholder
- ✅ `.editorconfig` placeholder

---

## 🔧 Comandos Ejecutados

```powershell
# Script de PowerShell para mover archivos
.\reorganize-monorepo.ps1

# Movimientos realizados:
Move-Item index.html → frontend\
Move-Item package.json → frontend\
Move-Item package-lock.json → frontend\
Move-Item vite.config.ts → frontend\
Move-Item tsconfig.json → frontend\
Move-Item script.js → frontend\
Move-Item style.css → frontend\
Move-Item apps → frontend\
Move-Item packages → frontend\
Move-Item node_modules → frontend\
```

---

## ⚠️ TODOs Post-Movimiento

Ver archivo completo en: `frontend/README.move-notes.md`

### Verificaciones inmediatas:

1. **Reinstalar dependencias en frontend:**
   ```powershell
   cd frontend
   npm install
   ```

2. **Probar servidor de desarrollo:**
   ```powershell
   npm run dev
   # Debería abrir en http://localhost:5173 o 5174
   ```

3. **Verificar rutas relativas:**
   - ✅ Rutas internas del frontend NO cambiaron (todo sigue dentro de `frontend/`)
   - ✅ `vite.config.ts` apunta a `apps/demo-vanilla` ✓
   - ✅ `tsconfig.json` tiene aliases `@domain`, `@board-core`, `@board-adapter` ✓
   - ✅ Imports relativos en `main.ts` siguen igual (`../../packages/...`) ✓

4. **Funcionalidades a validar:**
   - [ ] Drag & Drop funciona
   - [ ] Logs de debugging visibles en consola
   - [ ] Hot Module Replacement (HMR) funciona
   - [ ] Tags y assignees se renderizan correctamente
   - [ ] No hay errores de TypeScript

---

## 🚀 Próximos Pasos del Monorepo

### Backend (futuro):
1. Implementar API REST en .NET/Node.js/Go
2. Configurar PostgreSQL + Entity Framework / Prisma
3. Implementar Outbox pattern para publicación de eventos
4. Configurar autenticación (JWT)
5. Agregar CORS y middleware

### Worker (futuro):
1. Configurar RabbitMQ consumer
2. Implementar handlers para eventos del dominio
3. Agregar Redis para caché
4. Configurar jobs programados

### Contracts (futuro):
1. Definir DTOs compartidos en TypeScript
2. Generar contratos desde backend (.NET → TypeScript con NSwag/OpenAPI)
3. Publicar como paquete npm interno

### Deploy (futuro):
1. Completar `docker-compose.yml` con:
   - PostgreSQL
   - RabbitMQ
   - Redis
   - Backend API
   - Worker
   - Nginx para frontend
2. Configurar variables de entorno
3. Setup de CI/CD en GitHub Actions

### Docs (futuro):
1. Documentar arquitectura (C4 diagrams)
2. Crear ADRs (Architecture Decision Records)
3. Guías de desarrollo
4. Diagramas de eventos y flujos

---

## 📊 Estado Actual

✅ **Estructura del monorepo creada**  
✅ **Frontend movido correctamente a `frontend/`**  
✅ **Placeholders creados (backend, worker, contracts, docs, deploy)**  
✅ **Sin modificación de código existente**  
⏳ **Pendiente: Validar frontend funciona desde nueva ubicación**

---

## 🎯 Validación Final

Ejecutar estos comandos para validar todo:

```powershell
# 1. Verificar estructura
tree /F /A

# 2. Entrar a frontend
cd frontend

# 3. Instalar (si es necesario)
npm install

# 4. Ejecutar servidor
npm run dev

# 5. Abrir navegador en http://localhost:5173
# 6. Verificar consola del navegador:
#    - [adapter:dragstart] logs
#    - [adapter:drop] logs  
#    - [onMove] / [onReorder] logs
#    - [state] after onMove logs
```

Si todo funciona → ✅ Monorepo listo para commit  
Si hay errores → Ver `frontend/README.move-notes.md` para troubleshooting
