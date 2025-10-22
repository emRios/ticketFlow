import { getBoard, BoardScope } from '../api/board';

/**
 * Resultado de la carga del tablero con datos transformados
 */
export type BoardData = {
  columns: any[];
  tickets: any[];
  rawTickets: any[];
};

/**
 * Carga el tablero desde la API y transforma los datos
 * para el formato esperado por el adapter vanilla
 */
export async function loadBoard(scope: BoardScope): Promise<BoardData> {
  const { columns, tickets } = await getBoard(scope);
  console.log('[board]', { 
    columns, 
    tickets, 
    colCount: columns.length, 
    ticketCount: tickets.length 
  });
  
  // Mapear tickets a DTOs del adapter con capabilities
  const ticketsForAdapter = tickets.map(t => ({
    ticketId: t.id,
    title: t.title,
    columnId: t.columnId,
    order: t.order,
    tags: t.tags,
    assignee: t.assignee,
    capabilities: {
      canDrag: t.capabilities.move,
      canDropTo: t.capabilities.allowedTransitions
    }
  }));
  
  return { 
    columns, 
    tickets: ticketsForAdapter,
    rawTickets: tickets // Devolvemos los tickets originales para cache
  };
}
