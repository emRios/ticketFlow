// API - Operaciones sobre tickets

import { apiFetch } from './apiClient';

/**
 * Actualiza el estado de un ticket
 */
export async function updateTicketStatus(
  ticketId: string,
  newStatus: string
): Promise<void> {
  try {
    // Extraer el ID numérico del formato TF-123
    const id = ticketId.replace('TF-', '');
    
    // Mapear columnId del frontend a status del backend
    const statusMapping: Record<string, string> = {
      'nuevo': 'OPEN',
      'en-proceso': 'IN_PROGRESS',
      'en-espera': 'WAITING',
      'resuelto': 'RESOLVED',
      'cerrado': 'CLOSED'
    };
    
    const backendStatus = statusMapping[newStatus] || 'OPEN';
    
    await apiFetch<void>(`/api/tickets/${id}/status`, {
      method: 'PATCH',
      body: JSON.stringify({ status: backendStatus })
    });
    
    console.log(`✅ Ticket ${ticketId} actualizado a ${newStatus}`);
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
}): Promise<{ id: number }> {
  try {
    return await apiFetch<{ id: number }>('/api/tickets', {
      method: 'POST',
      body: JSON.stringify(data)
    });
  } catch (error) {
    console.error('Error creando ticket:', error);
    throw error;
  }
}
