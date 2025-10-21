# Notas de Movimiento del Frontend

## Archivos movidos de raíz a frontend/

- ✅ index.html
- ✅ package.json
- ✅ package-lock.json
- ✅ vite.config.ts
- ✅ tsconfig.json
- ✅ script.js
- ✅ style.css
- ✅ apps/
- ✅ packages/

## TODOs - Rutas a Revisar

### 1. vite.config.ts
- **TODO:** Verificar que `root: 'apps/demo-vanilla'` sigue siendo correcto
- Las rutas relativas dentro del frontend no deberían cambiar

### 2. package.json scripts
- **TODO:** Verificar que los scripts `dev`, `build`, `preview` funcionen desde `frontend/`
- Ejecutar `cd frontend && npm run dev` para validar

### 3. tsconfig.json
- **TODO:** Verificar que los paths aliases sigan funcionando:
  - `@domain` → `packages/domain-core`
  - `@board-core` → `packages/board-core`
  - `@board-adapter` → `packages/board-adapter-vanilla`

### 4. Imports relativos
- **TODO:** Verificar imports en `apps/demo-vanilla/main.ts`:
  - `import { initBoard } from '../../packages/board-adapter-vanilla/index'`
  - Estas rutas relativas NO deberían cambiar (siguen dentro de frontend/)

### 5. Node modules
- **TODO:** Ejecutar `npm install` dentro de `frontend/` después del movimiento
- El `node_modules/` quedó en la raíz, debe regenerarse en `frontend/`

## Comandos Post-Movimiento

```powershell
# Entrar a frontend y reinstalar dependencias
cd frontend
npm install

# Verificar que el servidor funciona
npm run dev

# Debería abrir en http://localhost:5173 o 5174
```

## Verificaciones

- [ ] `npm run dev` funciona desde `frontend/`
- [ ] Hot reload (HMR) sigue funcionando
- [ ] Drag & Drop operativo
- [ ] Logs de debugging visibles en consola
- [ ] TypeScript compila sin errores
