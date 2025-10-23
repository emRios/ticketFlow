import { describe, it, expect, beforeEach } from 'vitest';
import {
  getToken,
  setToken,
  clearToken,
  getUserId,
  setUserId,
  clearUserId,
} from '../../state/session';

describe('session', () => {
  beforeEach(() => {
    localStorage.clear();
  });

  describe('Token Management', () => {
    it('getToken debería retornar null inicialmente', () => {
      expect(getToken()).toBeNull();
    });

    it('setToken debería guardar token en localStorage', () => {
      const token = 'test-jwt-token';
      setToken(token);
      
      expect(getToken()).toBe(token);
      expect(localStorage.getItem('jwt_token')).toBe(token);
    });

    it('clearToken debería eliminar token', () => {
      setToken('test-token');
      clearToken();
      
      expect(getToken()).toBeNull();
      expect(localStorage.getItem('jwt_token')).toBeNull();
    });

    it('debería manejar múltiples llamadas a setToken', () => {
      setToken('token1');
      expect(getToken()).toBe('token1');
      
      setToken('token2');
      expect(getToken()).toBe('token2');
      
      setToken('token3');
      expect(getToken()).toBe('token3');
    });
  });

  describe('User ID Management', () => {
    it('getUserId debería retornar null inicialmente', () => {
      expect(getUserId()).toBeNull();
    });

    it('setUserId debería guardar userId en localStorage', () => {
      const userId = 'user-123';
      setUserId(userId);
      
      expect(getUserId()).toBe(userId);
      expect(localStorage.getItem('user_id')).toBe(userId);
      expect(localStorage.getItem('ticketflow_userId')).toBe(userId);
    });

    it('getUserId debería leer de ticketflow_userId si user_id no existe', () => {
      localStorage.setItem('ticketflow_userId', 'user-from-ticketflow');
      
      expect(getUserId()).toBe('user-from-ticketflow');
    });

    it('getUserId debería preferir ticketflow_userId sobre user_id', () => {
      localStorage.setItem('user_id', 'user-old');
      localStorage.setItem('ticketflow_userId', 'user-new');
      
      expect(getUserId()).toBe('user-new');
    });

    it('clearUserId debería eliminar todos los datos de usuario', () => {
      localStorage.setItem('user_id', 'user-123');
      localStorage.setItem('ticketflow_userId', 'user-123');
      localStorage.setItem('ticketflow_role', 'ADMIN');
      localStorage.setItem('ticketflow_username', 'admin');
      localStorage.setItem('ticketflow_email', 'admin@test.com');
      
      clearUserId();
      
      expect(localStorage.getItem('user_id')).toBeNull();
      expect(localStorage.getItem('ticketflow_userId')).toBeNull();
      expect(localStorage.getItem('ticketflow_role')).toBeNull();
      expect(localStorage.getItem('ticketflow_username')).toBeNull();
      expect(localStorage.getItem('ticketflow_email')).toBeNull();
    });
  });

  describe('Session Persistence', () => {
    it('debería mantener token y userId independientemente', () => {
      setToken('jwt-token');
      setUserId('user-123');
      
      expect(getToken()).toBe('jwt-token');
      expect(getUserId()).toBe('user-123');
      
      clearToken();
      expect(getToken()).toBeNull();
      expect(getUserId()).toBe('user-123'); // userId no debe afectarse
    });

    it('clearUserId no debería afectar el token', () => {
      setToken('jwt-token');
      setUserId('user-123');
      
      clearUserId();
      
      expect(getToken()).toBe('jwt-token'); // Token debe permanecer
      expect(getUserId()).toBeNull();
    });

    it('debería sobrevivir múltiples operaciones', () => {
      // Simular flujo de login
      setToken('initial-token');
      setUserId('user-1');
      
      expect(getToken()).toBe('initial-token');
      expect(getUserId()).toBe('user-1');
      
      // Simular refresh de token
      setToken('refreshed-token');
      
      expect(getToken()).toBe('refreshed-token');
      expect(getUserId()).toBe('user-1');
      
      // Simular logout
      clearToken();
      clearUserId();
      
      expect(getToken()).toBeNull();
      expect(getUserId()).toBeNull();
    });
  });

  describe('Edge Cases', () => {
    it('setToken debería manejar strings vacíos', () => {
      setToken('');
      expect(getToken()).toBe('');
      expect(localStorage.getItem('jwt_token')).toBe('');
    });

    it('setUserId debería manejar strings vacíos', () => {
      setUserId('');
      expect(getUserId()).toBe('');
    });

    it('debería manejar valores con espacios', () => {
      setToken('  token-with-spaces  ');
      expect(getToken()).toBe('  token-with-spaces  ');
    });

    it('debería manejar caracteres especiales en token', () => {
      const specialToken = 'token.with.dots-and_underscores';
      setToken(specialToken);
      expect(getToken()).toBe(specialToken);
    });
  });
});
