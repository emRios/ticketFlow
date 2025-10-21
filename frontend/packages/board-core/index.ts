export function applyMove(
  tickets: { ticketId: string; columnId: string; order: number; version: number }[],
  ticketId: string, from: string, to: string, newIndex: number
) {
  const moving = tickets.find(t => t.ticketId === ticketId);
  if (!moving) return tickets;
  
  // Actualizar la columna del ticket que se mueve
  moving.columnId = to;
  
  // Recalcular order en columna origen
  const srcTickets = tickets.filter(t => t.columnId === from && t.ticketId !== ticketId).sort((a,b) => a.order - b.order);
  srcTickets.forEach((t, i) => t.order = i);
  
  // Recalcular order en columna destino
  const dstTickets = tickets.filter(t => t.columnId === to).sort((a,b) => a.order - b.order);
  dstTickets.splice(newIndex, 0, moving);
  dstTickets.forEach((t, i) => t.order = i);
  
  return tickets;
}

export function applyReorder(
  tickets: { ticketId: string; columnId: string; order: number; version: number }[],
  ticketId: string, columnId: string, newIndex: number
) {
  const list = tickets.filter(t => t.columnId === columnId).sort((a,b) => a.order - b.order);
  const idx = list.findIndex(t => t.ticketId === ticketId);
  const [item] = list.splice(idx, 1);
  list.splice(newIndex, 0, item);
  list.forEach((t,i) => t.order = i);
  return tickets;
}

export function applyAssign(
  tickets: { ticketId: string; assigneeId?: string; version: number }[],
  ticketId: string, assigneeId: string, version: number
) {
  const ticket = tickets.find(t => t.ticketId === ticketId);
  if (ticket) {
    ticket.assigneeId = assigneeId;
    ticket.version = version;
  }
  return tickets;
}

export function applyAddTag(
  tickets: { ticketId: string; tagIds?: string[]; version: number }[],
  ticketId: string, tagId: string, version: number
) {
  const ticket = tickets.find(t => t.ticketId === ticketId);
  if (ticket) {
    ticket.tagIds = [...(ticket.tagIds || []), tagId];
    ticket.version = version;
  }
  return tickets;
}

export function applyRemoveTag(
  tickets: { ticketId: string; tagIds?: string[]; version: number }[],
  ticketId: string, tagId: string, version: number
) {
  const ticket = tickets.find(t => t.ticketId === ticketId);
  if (ticket && ticket.tagIds) {
    ticket.tagIds = ticket.tagIds.filter(id => id !== tagId);
    ticket.version = version;
  }
  return tickets;
}