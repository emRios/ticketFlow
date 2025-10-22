import './styles.css';

// ===== Debug Global =====
;(window as any).DEBUG_BOARD = (window as any).DEBUG_BOARD ?? true;
function dbg(...args: any[]) {
  if ((window as any).DEBUG_BOARD) console.log(...args);
}

// DTOs con datos de presentación (UI)
type TagDTO = { id: string; label: string; color: string };
type AssigneeDTO = { id: string; name: string; avatarUrl?: string };
type TicketDTO = { 
  ticketId: string; 
  title: string; 
  columnId: string; 
  order: number;
  tags?: TagDTO[];
  assignee?: AssigneeDTO;
  capabilities?: {
    canDrag?: boolean;
    canReorder?: boolean;
    canDropTo?: string[];
  };
};
type ColumnDTO = { id: string; name: string };

type Handlers = {
  onMove:    (cmd: { ticketId: string; from: string; to: string; newIndex: number }) => Promise<void> | void;
  onReorder: (cmd: { ticketId: string; columnId: string; newIndex: number }) => Promise<void> | void;
  onAssignTicket?: (cmd: { ticketId: string; assigneeId: string }) => Promise<void> | void;
  onAddTagToTicket?: (cmd: { ticketId: string; tagId: string }) => Promise<void> | void;
  onRemoveTagFromTicket?: (cmd: { ticketId: string; tagId: string }) => Promise<void> | void;
};

function getCardColumnId(cardEl: HTMLElement) {
  return cardEl.closest('.col')!.getAttribute('data-col-id')!;
}

const MAX_VISIBLE_TAGS = 3;

function renderTags(tags: TagDTO[] | undefined): string {
  if (!tags || tags.length === 0) return '';
  
  const visibleTags = tags.slice(0, MAX_VISIBLE_TAGS);
  const remainingCount = tags.length - MAX_VISIBLE_TAGS;
  
  return `<div class="card-tags" role="list" aria-label="Tags del ticket">
    ${visibleTags.map(tag => `
      <span 
        class="tag" 
        role="listitem"
        style="background-color: ${tag.color}" 
        data-tag-id="${tag.id}"
        aria-label="${tag.label}"
      >
        ${tag.label}
        <button 
          class="tag-remove" 
          data-tag-id="${tag.id}"
          aria-label="Remover ${tag.label}"
          title="Remover tag"
        >×</button>
      </span>
    `).join('')}
    ${remainingCount > 0 ? `<span class="tag-more" aria-label="${remainingCount} tags más">+${remainingCount}</span>` : ''}
  </div>`;
}

function renderAssignee(assignee: AssigneeDTO | undefined): string {
  if (!assignee) return '';
  const initials = assignee.name.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2);
  const avatarContent = assignee.avatarUrl 
    ? `<img src="${assignee.avatarUrl}" alt="${assignee.name}" class="avatar-img" />`
    : `<span class="avatar-initials">${initials}</span>`;
  return `<div class="card-assignee" data-assignee-id="${assignee.id}">
    <div 
      class="avatar" 
      role="img" 
      aria-label="Asignado a ${assignee.name}"
      title="${assignee.name}"
    >${avatarContent}</div>
  </div>`;
}

export function initBoard(el: HTMLElement, props: { columns: ColumnDTO[]; tickets: TicketDTO[] } & Handlers) {
  const render = () => {
    el.innerHTML = '';
    props.columns.forEach(c => {
      const col = document.createElement('div');
      col.className = 'col';
      col.setAttribute('data-col-id', c.id);
      col.innerHTML = `<div class="col-title">${c.name}</div><div class="col-body" data-dropzone></div>`;
      el.appendChild(col);
    });
    props.tickets.sort((a,b)=> a.order - b.order).forEach(ticket => {
      const card = document.createElement('div');
      card.className = 'card';
      
      // Capabilities con defaults seguros
      const caps = ticket.capabilities ?? {};
      const canDrag = caps.canDrag !== false; // default: true
      
      card.draggable = canDrag;
      card.setAttribute('data-ticket-id', ticket.ticketId);
      
      // Clase visual para tickets no arrastrables
      if (!canDrag) {
        card.classList.add('is-disabled');
      }
      
      card.innerHTML = `
        <div class="card-id">#${ticket.ticketId}</div>
        <div class="card-header">
          <div class="card-title">${ticket.title}</div>
          ${renderAssignee(ticket.assignee)}
        </div>
        ${ticket.assignee ? `<div class="card-assignee-name">Asignado a: ${ticket.assignee.name}</div>` : ''}
        ${renderTags(ticket.tags)}
      `;
      const body = el.querySelector(`.col[data-col-id="${ticket.columnId}"] .col-body`) as HTMLElement;
      body.appendChild(card);
    });

    el.querySelectorAll('.card').forEach(cardEl => {
      cardEl.addEventListener('dragstart', (e) => {
        const dragEvent = e as DragEvent;
        const ticketId = (cardEl as HTMLElement).getAttribute('data-ticket-id')!;
        dragEvent.dataTransfer?.setData('text/plain', ticketId);
        dbg('[adapter:dragstart]', { ticketId });
      });
      
      // Event listeners para remover tags (solo el botón "x")
      if (props.onRemoveTagFromTicket) {
        cardEl.querySelectorAll('.tag-remove').forEach(removeBtn => {
          removeBtn.addEventListener('click', async (e) => {
            e.stopPropagation();
            e.preventDefault();
            const ticketId = (cardEl as HTMLElement).getAttribute('data-ticket-id')!;
            const tagId = (removeBtn as HTMLElement).getAttribute('data-tag-id')!;
            await props.onRemoveTagFromTicket?.({ ticketId, tagId });
          });
        });
      }
      
      // Event listener para avatar (prevenir drag accidental)
      const avatarEl = cardEl.querySelector('.card-assignee');
      if (avatarEl) {
        avatarEl.addEventListener('mousedown', (e) => {
          e.stopPropagation();
        });
        avatarEl.addEventListener('click', (e) => {
          e.stopPropagation();
          e.preventDefault(); // Prevenir interferencia con drag
          // TODO: Abrir modal para cambiar assignee
        });
      }
      
      // Event listener para tags (prevenir drag accidental en tags)
      cardEl.querySelectorAll('.tag').forEach(tagEl => {
        tagEl.addEventListener('mousedown', (e) => {
          // Solo prevenir drag en el tag, el botón remove ya tiene su handler
          const target = e.target as HTMLElement;
          if (!target.classList.contains('tag-remove')) {
            e.stopPropagation();
          }
        });
      });
    });

    el.querySelectorAll('[data-dropzone]').forEach(zone => {
      zone.addEventListener('dragover', (e) => {
        e.preventDefault();
      });
      zone.addEventListener('drop', async (e) => {
        e.preventDefault();
        const dragEvent = e as DragEvent;
        const ticketId = dragEvent.dataTransfer?.getData('text/plain')!;
        const dropZone = zone as HTMLElement;
        const to = (dropZone.closest('.col') as HTMLElement).getAttribute('data-col-id')!;
        const cardEl = document.querySelector(`[data-ticket-id="${ticketId}"]`) as HTMLElement;
        const from = getCardColumnId(cardEl);
        const newIndex = dropZone.querySelectorAll('.card').length; // simple: al final
        
        // Buscar el ticket para revisar capabilities
        const ticket = props.tickets.find(t => t.ticketId === ticketId);
        const caps = ticket?.capabilities ?? {};
        const canDropTo = caps.canDropTo;
        
        // Si existe canDropTo y el destino NO está permitido, bloquear
        if (canDropTo && !canDropTo.includes(to)) {
          dbg('[adapter] drop blocked', { ticketId, from, to, allowed: canDropTo });
          return;
        }
        
        // Debug log antes de invocar callbacks
        if (from === to) {
          dbg('[adapter:drop]', { ticketId, from, to: from, newIndex, action: 'reorder' });
          await props.onReorder({ ticketId, columnId: to, newIndex });
        } else {
          dbg('[adapter:drop]', { ticketId, from, to, newIndex, action: 'move' });
          await props.onMove({ ticketId, from, to, newIndex });
        }
      });
    });
  };

  render();
  return {
    rerender(next: { columns: ColumnDTO[]; tickets: TicketDTO[] }) {
      Object.assign(props, next);
      render();
    }
  };
}