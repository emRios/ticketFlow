# TicketFlow (Demo Tablero Desacoplado)
- HTML5 Drag & Drop nativo (sin libs DnD).
- Lógica de negocio fuera de la UI (domain-core, board-core).
- Adapter de tablero vanilla (board-adapter-vanilla).

## Instalación y Uso

### 1. Instalar dependencias
```bash
npm i
```

### 2. Iniciar servidor de desarrollo
```bash
npm run dev
```

### 3. Abrir en el navegador
Visita: **http://localhost:5173**

## Scripts disponibles
- `npm run dev` - Inicia servidor de desarrollo en puerto 5173
- `npm run build` - Compila la aplicación para producción
- `npm run preview` - Previsualiza el build de producción

---

## Eventos y auditoría

La aplicación emite **Domain Events** con metadatos de auditoría para trazabilidad completa:

### Eventos disponibles

| Evento | Descripción | Payload |
|--------|-------------|---------|
| **TicketMoved** | Ticket movido entre columnas | `ticketId, from, to, newIndex` |
| **TicketReordered** | Orden cambiado dentro de columna | `ticketId, columnId, newIndex` |
| **TicketAssigned** | Ticket asignado a usuario | `ticketId, assigneeId` |
| **TicketTagAdded** | Tag agregado al ticket | `ticketId, tagId` |
| **TicketTagRemoved** | Tag removido del ticket | `ticketId, tagId` |

### Metadatos de auditoría (todos los eventos)

Cada evento incluye:
```typescript
{
  eventId: string;        // UUID único del evento
  type: string;           // Tipo de evento (ej: "TicketMoved")
  occurredAt: string;     // Timestamp ISO 8601 (ej: "2025-10-20T14:30:00.000Z")
  byUserId: string;       // ID del usuario que ejecutó la acción
  version: number;        // Versión del agregado (optimistic locking)
  // ... payload específico del evento
}
```

### Separación de dominio y presentación

**Importante**: Los datos de presentación (colores de tags, avatars de usuarios) NO pertenecen al dominio core.

- **Dominio** (`packages/domain-core`): Solo maneja **IDs** (tagIds, assigneeId) y lógica de negocio.
- **Proyección UI** (`apps/demo-vanilla/state.ts`): Catálogos `availableTags` y `assignees` con datos de presentación (color, avatarUrl).
- **Adaptador** (`board-adapter-vanilla`): Recibe **DTOs** con datos enriquecidos (TicketDTO con tags[].color y assignee.avatarUrl).

Esta separación permite cambiar colores/avatares sin modificar el dominio ni regenerar eventos históricos.
