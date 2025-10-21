2) Prompt Único para Copilot

Pega todo lo siguiente en Copilot Chat, estando posicionado en el repo TicketFlow:

Eres un “project refactor bot”. Estamos en el repo `TicketFlow` (origen: chiorji/kanban-board). Objetivo: dejar un demo funcional con una arquitectura desacoplada (**domain-core**, **board-core**, **board-adapter-vanilla**) y una app de demo (**apps/demo-vanilla**) usando **HTML5 Drag & Drop** (cero libs DnD) y **TypeScript**. Usa **Vite** solo para servir/bundle.

### Requisitos clave
- **Cero** librerías de drag-and-drop (NO SortableJS, NO dnd-kit).
- Lógica de negocio **fuera** de la UI (DDD/Hexagonal):
  - `/packages/domain-core`: comandos, eventos, reglas.
  - `/packages/board-core`: helpers de reorden/mover (agnósticos de UI/DOM).
  - `/packages/board-adapter-vanilla`: render + handlers HTML5 DnD; expone callbacks `onMove`/`onReorder`; **no** muta estado por sí mismo.
- Demo en `/apps/demo-vanilla` que compone todo y muestra las 4 columnas: **Nuevo, En Proceso, En Espera, Resuelto**.
- Mantén dependencias al mínimo: `vite`, `typescript`, `tslib`, `@types/node`.

### Tareas
1) **Crear el árbol de carpetas y archivos** siguiente (crea archivos con contenido real, no placeholders vacíos):


/packages
/domain-core
index.ts
types.ts
/board-core
index.ts
/board-adapter-vanilla
index.ts
styles.css
/apps
/demo-vanilla
index.html
main.ts
state.ts
package.json
tsconfig.json
vite.config.ts
.gitignore
README.md


2) **Contenido de archivos** (copiar literalmente):

**package.json**
```json
{
  "name": "ticketflow",
  "private": true,
  "version": "0.1.0",
  "type": "module",
  "scripts": {
    "dev": "vite",
    "build": "vite build",
    "preview": "vite preview"
  },
  "devDependencies": {
    "typescript": "^5.6.3",
    "vite": "^5.4.8",
    "@types/node": "^20.11.30",
    "tslib": "^2.8.1"
  }
}


tsconfig.json

{
  "compilerOptions": {
    "target": "ES2021",
    "module": "ESNext",
    "moduleResolution": "Bundler",
    "strict": true,
    "jsx": "react-jsx",
    "baseUrl": ".",
    "paths": {
      "@domain/*": ["packages/domain-core/*"],
      "@board-core/*": ["packages/board-core/*"],
      "@board-adapter/*": ["packages/board-adapter-vanilla/*"]
    }
  },
  "include": ["packages", "apps"]
}


vite.config.ts

import { defineConfig } from 'vite';
export default defineConfig({
  root: 'apps/demo-vanilla',
  server: { port: 5173 },
  resolve: {
    alias: {
      '@domain': '/packages/domain-core',
      '@board-core': '/packages/board-core',
      '@board-adapter': '/packages/board-adapter-vanilla'
    }
  }
});


.gitignore

node_modules
dist
.vite
.DS_Store


README.md

# TicketFlow (Demo Tablero Desacoplado)
- HTML5 Drag & Drop nativo (sin libs DnD).
- Lógica de negocio fuera de la UI (domain-core, board-core).
- Adapter de tablero vanilla (board-adapter-vanilla).

## Scripts
- `npm i`
- `npm run dev` (abre http://localhost:5173)


/packages/domain-core/types.ts

export type Deal = { id: string; title: string; columnId: string; order: number; version: number };
export type Column = { id: string; name: string };

export type MoveDealCmd = { dealId: string; from: string; to: string; newIndex: number; byUserId?: string };
export type ReorderDealCmd = { dealId: string; columnId: string; newIndex: number; byUserId?: string };

export type DomainEvent =
  | { type: 'DealMoved'; dealId: string; from: string; to: string; newIndex: number; version: number }
  | { type: 'DealReordered'; dealId: string; columnId: string; newIndex: number; version: number };


/packages/domain-core/index.ts

import { Deal, Column, MoveDealCmd, ReorderDealCmd, DomainEvent } from './types';

export function moveDeal(cmd: MoveDealCmd, deals: Deal[], columns: Column[]): DomainEvent[] {
  const d = deals.find(x => x.id === cmd.dealId);
  if (!d) throw new Error('Deal not found');
  if (!columns.some(c => c.id === cmd.to)) throw new Error('Target column not found');
  const nextVersion = d.version + 1;
  return [{ type: 'DealMoved', dealId: d.id, from: cmd.from, to: cmd.to, newIndex: cmd.newIndex, version: nextVersion }];
}

export function reorderDeal(cmd: ReorderDealCmd, deals: Deal[]): DomainEvent[] {
  const d = deals.find(x => x.id === cmd.dealId && x.columnId === cmd.columnId);
  if (!d) throw new Error('Deal not found in column');
  return [{ type: 'DealReordered', dealId: d.id, columnId: cmd.columnId, newIndex: cmd.newIndex, version: d.version + 1 }];
}


/packages/board-core/index.ts

export function applyMove(
  deals: { id: string; columnId: string; order: number; version: number }[],
  dealId: string, from: string, to: string, newIndex: number
) {
  const src = deals.filter(d => d.columnId === from).sort((a,b) => a.order - b.order);
  const dst = deals.filter(d => d.columnId === to).sort((a,b) => a.order - b.order);
  const moving = deals.find(d => d.id === dealId)!;
  const srcRest = src.filter(d => d.id !== dealId);
  dst.splice(newIndex, 0, { ...moving, columnId: to });
  srcRest.forEach((d,i) => d.order = i);
  dst.forEach((d,i) => d.order = i);
  return deals.map(d => (d.id === moving.id ? { ...d, columnId: to } : d));
}

export function applyReorder(
  deals: { id: string; columnId: string; order: number; version: number }[],
  dealId: string, columnId: string, newIndex: number
) {
  const list = deals.filter(d => d.columnId === columnId).sort((a,b) => a.order - b.order);
  const idx = list.findIndex(d => d.id === dealId);
  const [item] = list.splice(idx, 1);
  list.splice(newIndex, 0, item);
  list.forEach((d,i) => d.order = i);
  return deals;
}


/packages/board-adapter-vanilla/styles.css

:root { --gap: 12px; --card: #ffffff; --col-bg: #f5f5f7; --border: #e3e3e8; --title: #333; }
* { box-sizing: border-box; }
body { margin: 0; font-family: system-ui, sans-serif; background:#fff; }
.board { display: grid; grid-template-columns: repeat(4, 1fr); gap: var(--gap); padding: 16px; }
.col { background: var(--col-bg); border: 1px solid var(--border); border-radius: 10px; display:flex; flex-direction:column; min-height: 60vh; }
.col-title { padding: 12px 14px; font-weight: 600; color: var(--title); border-bottom: 1px solid var(--border); }
.col-body { padding: 12px; display: flex; flex-direction: column; gap: var(--gap); min-height: 50vh; }
.card { background: var(--card); border: 1px solid var(--border); border-radius: 10px; padding: 10px 12px; cursor: grab; }
.card:active { cursor: grabbing; }


/packages/board-adapter-vanilla/index.ts

import './styles.css';

type DealDTO   = { id: string; title: string; columnId: string; order: number };
type ColumnDTO = { id: string; name: string };

type Handlers = {
  onMove:    (cmd: { dealId: string; from: string; to: string; newIndex: number }) => Promise<void> | void;
  onReorder: (cmd: { dealId: string; columnId: string; newIndex: number }) => Promise<void> | void;
};

function getCardColumnId(cardEl: HTMLElement) {
  return cardEl.closest('.col')!.getAttribute('data-col-id')!;
}

export function initBoard(el: HTMLElement, props: { columns: ColumnDTO[]; deals: DealDTO[] } & Handlers) {
  const render = () => {
    el.innerHTML = '';
    props.columns.forEach(c => {
      const col = document.createElement('div');
      col.className = 'col';
      col.setAttribute('data-col-id', c.id);
      col.innerHTML = `<div class="col-title">${c.name}</div><div class="col-body" data-dropzone></div>`;
      el.appendChild(col);
    });
    props.deals.sort((a,b)=> a.order - b.order).forEach(d => {
      const card = document.createElement('div');
      card.className = 'card';
      card.draggable = true;
      card.setAttribute('data-deal-id', d.id);
      card.textContent = d.title;
      const body = el.querySelector(`.col[data-col-id="${d.columnId}"] .col-body`) as HTMLElement;
      body.appendChild(card);
    });

    el.querySelectorAll('.card').forEach(cardEl => {
      cardEl.addEventListener('dragstart', (e: DragEvent) => {
        e.dataTransfer?.setData('text/plain', (cardEl as HTMLElement).getAttribute('data-deal-id')!);
      });
    });

    el.querySelectorAll('[data-dropzone]').forEach(zone => {
      zone.addEventListener('dragover', (e: DragEvent) => e.preventDefault());
      zone.addEventListener('drop', async (e: DragEvent) => {
        e.preventDefault();
        const dealId = e.dataTransfer?.getData('text/plain')!;
        const dropZone = zone as HTMLElement;
        const to = (dropZone.closest('.col') as HTMLElement).getAttribute('data-col-id')!;
        const cardEl = document.querySelector(`[data-deal-id="${dealId}"]`) as HTMLElement;
        const from = getCardColumnId(cardEl);
        const newIndex = dropZone.querySelectorAll('.card').length; // simple: al final
        if (from === to) {
          await props.onReorder({ dealId, columnId: to, newIndex });
        } else {
          await props.onMove({ dealId, from, to, newIndex });
        }
      });
    });
  };

  render();
  return {
    rerender(next: { columns: ColumnDTO[]; deals: DealDTO[] }) {
      Object.assign(props, next);
      render();
    }
  };
}


/apps/demo-vanilla/state.ts

export const columns = [
  { id: 'nuevo', name: 'Nuevo' },
  { id: 'en-proceso', name: 'En Proceso' },
  { id: 'en-espera', name: 'En Espera' },
  { id: 'resuelto', name: 'Resuelto' }
];

export let deals = [
  { id: 'd1', title: 'Llamar a cliente', columnId: 'nuevo',       order: 0, version: 1 },
  { id: 'd2', title: 'Enviar propuesta', columnId: 'en-proceso',  order: 0, version: 2 },
  { id: 'd3', title: 'Revisión legal',   columnId: 'en-espera',   order: 0, version: 1 }
];


/apps/demo-vanilla/main.ts

import { initBoard } from '@board-adapter';
import { moveDeal, reorderDeal } from '@domain';
import { applyMove, applyReorder } from '@board-core';
import { columns, deals } from './state';

declare module '@domain' { export * from '../../packages/domain-core/index'; }
declare module '@board-core' { export * from '../../packages/board-core/index'; }
declare module '@board-adapter' { export * from '../../packages/board-adapter-vanilla/index'; }

const root = document.getElementById('app')!;
const board = initBoard(root, {
  columns, deals,
  async onMove(cmd) {
    const events = moveDeal({ ...cmd }, deals as any, columns as any);
    events.forEach(evt => { if (evt.type === 'DealMoved') applyMove(deals as any, evt.dealId, evt.from, evt.to, evt.newIndex); });
    board.rerender({ columns, deals });
    // TODO: POST Outbox → RabbitMQ (futuro)
  },
  async onReorder(cmd) {
    const events = reorderDeal({ ...cmd }, deals as any);
    events.forEach(evt => { if (evt.type === 'DealReordered') applyReorder(deals as any, evt.dealId, evt.columnId, evt.newIndex); });
    board.rerender({ columns, deals });
  }
});


/apps/demo-vanilla/index.html

<!doctype html>
<html lang="es">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <title>TicketFlow – Demo Tablero</title>
  <link rel="stylesheet" href="../../packages/board-adapter-vanilla/styles.css" />
  <style> body { background:#fff } </style>
</head>
<body>
  <div id="app" class="board"></div>
  <script type="module" src="./main.ts"></script>
</body>
</html>