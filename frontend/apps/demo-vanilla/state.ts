export const columns = [
  { id: 'nuevo', name: 'Nuevo' },
  { id: 'en-proceso', name: 'En Proceso' },
  { id: 'en-espera', name: 'En Espera' },
  { id: 'resuelto', name: 'Resuelto' }
];

// ===== Catálogos de presentación =====
// Catálogo de tags disponibles (dominio solo conoce id y label)
export const availableTags = [
  { id: 't1', label: 'Urgente', color: '#ef4444' },
  { id: 't2', label: 'Bug', color: '#f97316' },
  { id: 't3', label: 'Feature', color: '#3b82f6' },
  { id: 't4', label: 'Documentación', color: '#8b5cf6' },
  { id: 't5', label: 'Mejora', color: '#10b981' },
  { id: 't6', label: 'Cliente VIP', color: '#f59e0b' }
];

// Catálogo de assignees (con avatarUrl opcional para proyección UI)
export const assignees = [
  { id: 'u1', name: 'Juan Pérez', avatarUrl: 'https://i.pravatar.cc/150?img=12' },
  { id: 'u2', name: 'María García', avatarUrl: undefined }, // Sin avatar, se usarán iniciales
  { id: 'u3', name: 'Carlos López', avatarUrl: 'https://i.pravatar.cc/150?img=33' },
  { id: 'u4', name: 'Ana Martínez', avatarUrl: 'https://i.pravatar.cc/150?img=45' }
];

// Estado de tickets (dominio puro: solo IDs)
export let tickets = [
  { 
    ticketId: 't1', 
    title: 'Llamar a cliente', 
    columnId: 'nuevo', 
    order: 0, 
    version: 1,
    tagIds: ['t1'],
    assigneeId: 'u1'
  },
  { 
    ticketId: 't2', 
    title: 'Enviar propuesta', 
    columnId: 'en-proceso', 
    order: 0, 
    version: 2,
    tagIds: ['t3'],
    assigneeId: 'u2'
  },
  { 
    ticketId: 't3', 
    title: 'Revisión legal', 
    columnId: 'en-espera', 
    order: 0, 
    version: 1,
    tagIds: ['t2', 't4'],
    assigneeId: 'u3'
  }
];