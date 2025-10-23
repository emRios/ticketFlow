import { describe, it, expect, beforeEach, vi } from 'vitest';
import { getToken, setAuthToken, clearAuthToken, getUserId, ApiError, apiFetch } from '../../api/apiClient';

describe('apiClient', () => {
  beforeEach(() => {
    localStorage.clear();
    vi.restoreAllMocks();
  });

  describe('Token Management', () => {
    it('getToken debería retornar null si no hay token', () => {
      expect(getToken()).toBeNull();
    });

    it('setAuthToken debería guardar token en localStorage', () => {
      const token = 'test-jwt-token';
      setAuthToken(token);
      expect(localStorage.getItem('jwt_token')).toBe(token);
      expect(getToken()).toBe(token);
    });

    it('clearAuthToken debería eliminar token de localStorage', () => {
      setAuthToken('test-token');
      clearAuthToken();
      expect(getToken()).toBeNull();
      expect(localStorage.getItem('jwt_token')).toBeNull();
    });
  });

  describe('User ID Management', () => {
    it('getUserId debería retornar null si no hay userId', () => {
      expect(getUserId()).toBeNull();
    });

    it('getUserId debería retornar userId de localStorage', () => {
      localStorage.setItem('user_id', 'user-123');
      expect(getUserId()).toBe('user-123');
    });
  });

  describe('ApiError', () => {
    it('debería crear instancia de ApiError correctamente', () => {
      const error = new ApiError('Test error', 404, { message: 'Not found' });
      
      expect(error).toBeInstanceOf(Error);
      expect(error).toBeInstanceOf(ApiError);
      expect(error.message).toBe('Test error');
      expect(error.status).toBe(404);
      expect(error.body).toEqual({ message: 'Not found' });
      expect(error.name).toBe('ApiError');
    });
  });

  describe('apiFetch', () => {
    beforeEach(() => {
      global.fetch = vi.fn();
    });

    it('debería hacer GET request correctamente', async () => {
      const mockData = { id: '1', title: 'Test' };
      
      (global.fetch as any).mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => mockData,
      });

      const result = await apiFetch('/api/test');

      expect(global.fetch).toHaveBeenCalledWith(
        'http://localhost:5076/api/test',
        expect.objectContaining({
          headers: expect.any(Object),
        })
      );
      expect(result).toEqual(mockData);
    });

    it('debería incluir Authorization header si hay token', async () => {
      setAuthToken('test-jwt-token');
      
      (global.fetch as any).mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => ({}),
      });

      await apiFetch('/api/test');

      expect(global.fetch).toHaveBeenCalledWith(
        expect.any(String),
        expect.objectContaining({
          headers: expect.objectContaining({
            'Authorization': 'Bearer test-jwt-token',
          }),
        })
      );
    });

    it('debería incluir X-UserId header si hay userId', async () => {
      localStorage.setItem('user_id', 'user-123');
      
      (global.fetch as any).mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => ({}),
      });

      await apiFetch('/api/test');

      expect(global.fetch).toHaveBeenCalledWith(
        expect.any(String),
        expect.objectContaining({
          headers: expect.objectContaining({
            'X-UserId': 'user-123',
          }),
        })
      );
    });

    it('debería hacer POST request con body', async () => {
      const postData = { title: 'New item' };
      
      (global.fetch as any).mockResolvedValueOnce({
        ok: true,
        status: 201,
        json: async () => ({ id: '123', ...postData }),
      });

      await apiFetch('/api/items', {
        method: 'POST',
        body: JSON.stringify(postData),
      });

      expect(global.fetch).toHaveBeenCalledWith(
        'http://localhost:5076/api/items',
        expect.objectContaining({
          method: 'POST',
          body: JSON.stringify(postData),
        })
      );
    });

    it('debería lanzar ApiError en respuestas no exitosas', async () => {
      const errorBody = { error: 'Bad request' };
      
      (global.fetch as any).mockResolvedValueOnce({
        ok: false,
        status: 400,
        statusText: 'Bad Request',
        json: async () => errorBody,
      });

      await expect(apiFetch('/api/test')).rejects.toThrow(ApiError);
      
      try {
        // Necesitamos hacer otra llamada porque el mock se consume
        (global.fetch as any).mockResolvedValueOnce({
          ok: false,
          status: 400,
          statusText: 'Bad Request',
          json: async () => errorBody,
        });
        
        await apiFetch('/api/test');
      } catch (error) {
        expect(error).toBeInstanceOf(ApiError);
        if (error instanceof ApiError) {
          expect(error.status).toBe(400);
          expect(error.body).toEqual(errorBody);
        }
      }
    });

    it('debería manejar respuestas sin cuerpo (204, DELETE)', async () => {
      (global.fetch as any).mockResolvedValueOnce({
        ok: true,
        status: 204,
        text: async () => '',
      });

      const result = await apiFetch<void>('/api/items/1', {
        method: 'DELETE',
      });

      expect(result).toBeUndefined();
    });

    it('debería incluir Content-Type: application/json para POST', async () => {
      (global.fetch as any).mockResolvedValueOnce({
        ok: true,
        status: 201,
        json: async () => ({}),
      });

      await apiFetch('/api/test', { method: 'POST', body: JSON.stringify({}) });

      expect(global.fetch).toHaveBeenCalledWith(
        expect.any(String),
        expect.objectContaining({
          headers: expect.objectContaining({
            'Content-Type': 'application/json',
          }),
        })
      );
    });

    it('debería permitir sobrescribir headers', async () => {
      (global.fetch as any).mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => ({}),
      });

      await apiFetch('/api/test', {
        headers: {
          'Custom-Header': 'custom-value',
        },
      });

      expect(global.fetch).toHaveBeenCalledWith(
        expect.any(String),
        expect.objectContaining({
          headers: expect.objectContaining({
            'Custom-Header': 'custom-value',
          }),
        })
      );
    });
  });
});
