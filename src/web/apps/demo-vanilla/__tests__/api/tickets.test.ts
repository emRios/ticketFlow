import { describe, it, expect, beforeEach, vi } from 'vitest';
import { updateTicketStatus, createTicket } from '../../api/tickets';
import * as apiClient from '../../api/apiClient';

// Mock del módulo apiClient
vi.mock('../../api/apiClient', () => ({
  apiFetch: vi.fn(),
}));

describe('tickets API', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('updateTicketStatus', () => {
    it('debería actualizar estado del ticket correctamente', async () => {
      const mockApiFetch = vi.mocked(apiClient.apiFetch);
      mockApiFetch.mockResolvedValueOnce(undefined);

      const ticketId = 'ticket-123';
      const newStatus = 'en-proceso';
      const comment = 'Iniciando trabajo';

      await updateTicketStatus(ticketId, newStatus, comment);

      expect(mockApiFetch).toHaveBeenCalledWith(
        `/api/tickets/${ticketId}/status`,
        expect.objectContaining({
          method: 'PATCH',
          body: JSON.stringify({ status: 'en-proceso', comment }),
        })
      );
    });

    it('debería normalizar estados al formato backend', async () => {
      const mockApiFetch = vi.mocked(apiClient.apiFetch);
      mockApiFetch.mockResolvedValueOnce(undefined);

      const ticketId = 'ticket-123';
      
      // Test diferentes variantes de estado
      await updateTicketStatus(ticketId, 'NUEVO');
      expect(mockApiFetch).toHaveBeenLastCalledWith(
        expect.any(String),
        expect.objectContaining({
          body: JSON.stringify({ status: 'nuevo', comment: undefined }),
        })
      );

      await updateTicketStatus(ticketId, 'EN-PROCESO');
      expect(mockApiFetch).toHaveBeenLastCalledWith(
        expect.any(String),
        expect.objectContaining({
          body: JSON.stringify({ status: 'en-proceso', comment: undefined }),
        })
      );

      await updateTicketStatus(ticketId, 'RESUELTO');
      expect(mockApiFetch).toHaveBeenLastCalledWith(
        expect.any(String),
        expect.objectContaining({
          body: JSON.stringify({ status: 'resuelto', comment: undefined }),
        })
      );
    });

    it('debería usar "nuevo" para estados no reconocidos', async () => {
      const mockApiFetch = vi.mocked(apiClient.apiFetch);
      mockApiFetch.mockResolvedValueOnce(undefined);

      const ticketId = 'ticket-123';
      
      await updateTicketStatus(ticketId, 'invalid-status');

      expect(mockApiFetch).toHaveBeenCalledWith(
        expect.any(String),
        expect.objectContaining({
          body: JSON.stringify({ status: 'nuevo', comment: undefined }),
        })
      );
    });

    it('debería propagar errores de la API', async () => {
      const mockApiFetch = vi.mocked(apiClient.apiFetch);
      const error = new Error('Network error');
      mockApiFetch.mockRejectedValueOnce(error);

      await expect(
        updateTicketStatus('ticket-123', 'en-proceso')
      ).rejects.toThrow('Network error');
    });

    it('debería incluir comentario cuando se provee', async () => {
      const mockApiFetch = vi.mocked(apiClient.apiFetch);
      mockApiFetch.mockResolvedValueOnce(undefined);

      const ticketId = 'ticket-123';
      const status = 'resuelto';
      const comment = 'Problema solucionado';

      await updateTicketStatus(ticketId, status, comment);

      expect(mockApiFetch).toHaveBeenCalledWith(
        expect.any(String),
        expect.objectContaining({
          body: JSON.stringify({ status: 'resuelto', comment: 'Problema solucionado' }),
        })
      );
    });
  });

  describe('createTicket', () => {
    it('debería crear ticket con datos mínimos', async () => {
      const mockApiFetch = vi.mocked(apiClient.apiFetch);
      const mockResponse = { id: 'new-ticket-123' };
      mockApiFetch.mockResolvedValueOnce(mockResponse);

      const ticketData = {
        title: 'Nuevo problema',
        description: 'Descripción del problema',
      };

      const result = await createTicket(ticketData);

      expect(mockApiFetch).toHaveBeenCalledWith(
        '/api/tickets',
        expect.objectContaining({
          method: 'POST',
          body: JSON.stringify(ticketData),
        })
      );
      expect(result).toEqual(mockResponse);
    });

    it('debería crear ticket con prioridad especificada', async () => {
      const mockApiFetch = vi.mocked(apiClient.apiFetch);
      const mockResponse = { id: 'new-ticket-456' };
      mockApiFetch.mockResolvedValueOnce(mockResponse);

      const ticketData = {
        title: 'Problema urgente',
        description: 'Descripción',
        priority: 'HIGH',
      };

      const result = await createTicket(ticketData);

      expect(mockApiFetch).toHaveBeenCalledWith(
        '/api/tickets',
        expect.objectContaining({
          body: JSON.stringify(ticketData),
        })
      );
      expect(result).toEqual(mockResponse);
    });

    it('debería propagar errores de creación', async () => {
      const mockApiFetch = vi.mocked(apiClient.apiFetch);
      const error = new Error('Validation error');
      mockApiFetch.mockRejectedValueOnce(error);

      await expect(
        createTicket({ title: 'Test', description: 'Test' })
      ).rejects.toThrow('Validation error');
    });

    it('debería retornar ID del ticket creado', async () => {
      const mockApiFetch = vi.mocked(apiClient.apiFetch);
      const expectedId = 'ticket-789';
      mockApiFetch.mockResolvedValueOnce({ id: expectedId });

      const result = await createTicket({
        title: 'Test',
        description: 'Test description',
      });

      expect(result.id).toBe(expectedId);
    });
  });
});
