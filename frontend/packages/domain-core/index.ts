import { 
  Ticket, 
  Column, 
  Tag,
  Assignee,
  MoveTicketCmd, 
  ReorderTicketCmd, 
  AssignTicketCmd,
  AddTagToTicketCmd,
  RemoveTagFromTicketCmd,
  DomainEvent 
} from './types';

export * from './types';

// Helper para generar eventos con metadata
function createEvent<T extends DomainEvent>(
  type: T['type'],
  data: Omit<T, 'type' | 'eventId' | 'occurredAt' | 'byUserId'>,
  byUserId?: string
): T {
  return {
    type,
    eventId: crypto.randomUUID(), // Web Crypto API (disponible en navegador)
    occurredAt: new Date().toISOString(),
    byUserId,
    ...data
  } as T;
}

export function moveTicket(cmd: MoveTicketCmd, tickets: Ticket[], columns: Column[]): DomainEvent[] {
  const ticket = tickets.find(x => x.ticketId === cmd.ticketId);
  if (!ticket) throw new Error('Ticket not found');
  if (!columns.some(c => c.id === cmd.to)) throw new Error('Target column not found');
  
  // TODO: Validar permisos por rol (Admin/Agente puede mover, Cliente solo sus propios tickets)
  
  const nextVersion = ticket.version + 1;
  return [
    createEvent<Extract<DomainEvent, { type: 'TicketMoved' }>>('TicketMoved', {
      ticketId: ticket.ticketId,
      from: cmd.from,
      to: cmd.to,
      newIndex: cmd.newIndex,
      version: nextVersion
    }, cmd.byUserId)
  ];
}

export function reorderTicket(cmd: ReorderTicketCmd, tickets: Ticket[]): DomainEvent[] {
  const ticket = tickets.find(x => x.ticketId === cmd.ticketId && x.columnId === cmd.columnId);
  if (!ticket) throw new Error('Ticket not found in column');
  
  // TODO: Validar permisos por rol
  
  return [
    createEvent<Extract<DomainEvent, { type: 'TicketReordered' }>>('TicketReordered', {
      ticketId: ticket.ticketId,
      columnId: cmd.columnId,
      newIndex: cmd.newIndex,
      version: ticket.version + 1
    }, cmd.byUserId)
  ];
}

export function assignTicket(cmd: AssignTicketCmd, tickets: Ticket[], assignees: Assignee[]): DomainEvent[] {
  const ticket = tickets.find(x => x.ticketId === cmd.ticketId);
  if (!ticket) throw new Error('Ticket not found');
  if (!assignees.some(a => a.id === cmd.assigneeId)) throw new Error('Assignee not found');
  
  // TODO: Validar permisos - Solo Admin/Agente puede asignar tickets
  
  return [
    createEvent<Extract<DomainEvent, { type: 'TicketAssigned' }>>('TicketAssigned', {
      ticketId: ticket.ticketId,
      assigneeId: cmd.assigneeId,
      version: ticket.version + 1
    }, cmd.byUserId)
  ];
}

export function addTagToTicket(cmd: AddTagToTicketCmd, tickets: Ticket[], tags: Tag[]): DomainEvent[] {
  const ticket = tickets.find(x => x.ticketId === cmd.ticketId);
  if (!ticket) throw new Error('Ticket not found');
  if (!tags.some(t => t.id === cmd.tagId)) throw new Error('Tag not found in catalog');
  if (ticket.tagIds?.includes(cmd.tagId)) throw new Error('Tag already added to ticket');
  
  // TODO: Validar permisos - Admin/Agente puede agregar tags
  
  return [
    createEvent<Extract<DomainEvent, { type: 'TicketTagAdded' }>>('TicketTagAdded', {
      ticketId: ticket.ticketId,
      tagId: cmd.tagId,
      version: ticket.version + 1
    }, cmd.byUserId)
  ];
}

export function removeTagFromTicket(cmd: RemoveTagFromTicketCmd, tickets: Ticket[]): DomainEvent[] {
  const ticket = tickets.find(x => x.ticketId === cmd.ticketId);
  if (!ticket) throw new Error('Ticket not found');
  if (!ticket.tagIds?.includes(cmd.tagId)) throw new Error('Tag not found on ticket');
  
  // TODO: Validar permisos - Admin/Agente puede remover tags
  
  return [
    createEvent<Extract<DomainEvent, { type: 'TicketTagRemoved' }>>('TicketTagRemoved', {
      ticketId: ticket.ticketId,
      tagId: cmd.tagId,
      version: ticket.version + 1
    }, cmd.byUserId)
  ];
}