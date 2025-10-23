// API - Operaciones sobre tickets

import { apiFetch } from './apiClient';

/**
 * Actualiza el estado de un ticket
 */
export async function updateTicketStatus(
  ticketId: string,
  newStatus: string,
  comment?: string
): Promise<void> {
  try {
    // ticketId ya es un UUID directo desde el backend
    
    // Mapear columnId del frontend a status del backend (español canónico)
    const normalized = (newStatus || '').toLowerCase();
    const allowed = ['nuevo', 'en-proceso', 'en-espera', 'resuelto', 'cerrado'];
    const backendStatus = allowed.includes(normalized) ? normalized : 'nuevo';
    
    await apiFetch<void>(`/api/tickets/${ticketId}/status`, {
      method: 'PATCH',
      body: JSON.stringify({ status: backendStatus, comment })
    });
    
  console.log(`✅ Ticket ${ticketId} actualizado a ${backendStatus}`);
  } catch (error) {
    console.error(`Error actualizando ticket ${ticketId}:`, error);
    throw error;
  }
}

/**
 * Crea un nuevo ticket
 */
export async function createTicket(data: {
  title: string;
  description: string;
  priority?: string;
}): Promise<{ id: string }> {
  try {
    return await apiFetch<{ id: string }>('/api/tickets', {
      method: 'POST',
      body: JSON.stringify(data)
    });
  } catch (error) {
    console.error('Error creando ticket:', error);
    throw error;
  }
}
