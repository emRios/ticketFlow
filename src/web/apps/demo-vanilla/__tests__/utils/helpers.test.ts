import { describe, it, expect } from 'vitest';

describe('Mappers and Utilities', () => {
  describe('Status Normalization', () => {
    it('debería normalizar estados de español a formato canónico', () => {
      const normalizeStatus = (status: string): string => {
        const statusMap: Record<string, string> = {
          'nuevo': 'nuevo',
          'en-proceso': 'en-proceso',
          'enproceso': 'en-proceso',
          'en_proceso': 'en-proceso',
          'en-espera': 'en-espera',
          'enespera': 'en-espera',
          'resuelto': 'resuelto',
          'cerrado': 'cerrado',
        };
        return statusMap[status.toLowerCase()] || 'nuevo';
      };

      expect(normalizeStatus('NUEVO')).toBe('nuevo');
      expect(normalizeStatus('en-proceso')).toBe('en-proceso');
      expect(normalizeStatus('EnProceso')).toBe('en-proceso');
      expect(normalizeStatus('en_proceso')).toBe('en-proceso');
      expect(normalizeStatus('invalid')).toBe('nuevo');
    });
  });

  describe('Priority Validation', () => {
    it('debería validar prioridades correctamente', () => {
      const isValidPriority = (priority: string): boolean => {
        const validPriorities = ['LOW', 'MEDIUM', 'HIGH', 'CRITICAL'];
        return validPriorities.includes(priority.toUpperCase());
      };

      expect(isValidPriority('LOW')).toBe(true);
      expect(isValidPriority('low')).toBe(true);
      expect(isValidPriority('MEDIUM')).toBe(true);
      expect(isValidPriority('HIGH')).toBe(true);
      expect(isValidPriority('CRITICAL')).toBe(true);
      expect(isValidPriority('INVALID')).toBe(false);
      expect(isValidPriority('')).toBe(false);
    });
  });

  describe('Role Validation', () => {
    it('debería validar roles de usuario', () => {
      const isValidRole = (role: string): boolean => {
        const validRoles = ['ADMIN', 'AGENT', 'CLIENT'];
        return validRoles.includes(role.toUpperCase());
      };

      expect(isValidRole('ADMIN')).toBe(true);
      expect(isValidRole('admin')).toBe(true);
      expect(isValidRole('AGENT')).toBe(true);
      expect(isValidRole('CLIENT')).toBe(true);
      expect(isValidRole('SUPERADMIN')).toBe(false);
      expect(isValidRole('GUEST')).toBe(false);
    });
  });

  describe('Email Validation', () => {
    it('debería validar emails correctamente', () => {
      const isValidEmail = (email: string): boolean => {
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        return emailRegex.test(email);
      };

      expect(isValidEmail('user@example.com')).toBe(true);
      expect(isValidEmail('test.user@domain.co.uk')).toBe(true);
      expect(isValidEmail('invalid-email')).toBe(false);
      expect(isValidEmail('@domain.com')).toBe(false);
      expect(isValidEmail('user@')).toBe(false);
      expect(isValidEmail('user@domain')).toBe(false);
      expect(isValidEmail('')).toBe(false);
    });
  });

  describe('Date Formatting', () => {
    it('debería formatear fechas a formato legible', () => {
      const formatDate = (date: Date | string): string => {
        const d = typeof date === 'string' ? new Date(date) : date;
        return d.toLocaleString('es-ES');
      };

      const testDate = new Date('2025-01-15T10:30:00Z');
      const formatted = formatDate(testDate);
      
      expect(formatted).toContain('2025');
      expect(formatted).toBeTruthy();
    });

    it('debería formatear fechas ISO string', () => {
      const formatDate = (isoString: string): string => {
        const date = new Date(isoString);
        return date.toLocaleString('es-ES');
      };

      const isoDate = '2025-10-23T14:30:00Z';
      const formatted = formatDate(isoDate);
      
      expect(formatted).toBeTruthy();
      expect(formatted).toContain('2025');
    });
  });

  describe('String Utilities', () => {
    it('debería truncar strings largos', () => {
      const truncate = (str: string, maxLength: number): string => {
        if (str.length <= maxLength) return str;
        return str.substring(0, maxLength) + '...';
      };

      expect(truncate('Short text', 20)).toBe('Short text');
      expect(truncate('This is a very long text that needs truncation', 20)).toBe('This is a very long ...');
      expect(truncate('', 10)).toBe('');
    });

    it('debería capitalizar primera letra', () => {
      const capitalize = (str: string): string => {
        if (!str) return str;
        return str.charAt(0).toUpperCase() + str.slice(1).toLowerCase();
      };

      expect(capitalize('hello')).toBe('Hello');
      expect(capitalize('WORLD')).toBe('World');
      expect(capitalize('test')).toBe('Test');
      expect(capitalize('')).toBe('');
    });
  });

  describe('URL Parameter Extraction', () => {
    it('debería extraer parámetros de URL', () => {
      const getUrlParam = (url: string, param: string): string | null => {
        const urlObj = new URL(url);
        return urlObj.searchParams.get(param);
      };

      expect(getUrlParam('http://localhost/page?id=123', 'id')).toBe('123');
      expect(getUrlParam('http://localhost/page?name=test&id=456', 'id')).toBe('456');
      expect(getUrlParam('http://localhost/page?name=test', 'id')).toBeNull();
    });
  });

  describe('Array Utilities', () => {
    it('debería verificar si array está vacío', () => {
      const isEmpty = (arr: any[]): boolean => arr.length === 0;

      expect(isEmpty([])).toBe(true);
      expect(isEmpty([1])).toBe(false);
      expect(isEmpty([1, 2, 3])).toBe(false);
    });

    it('debería obtener último elemento de array', () => {
      const last = <T>(arr: T[]): T | undefined => arr[arr.length - 1];

      expect(last([1, 2, 3])).toBe(3);
      expect(last(['a'])).toBe('a');
      expect(last([])).toBeUndefined();
    });
  });

  describe('Object Utilities', () => {
    it('debería verificar si objeto está vacío', () => {
      const isEmptyObject = (obj: object): boolean => {
        return Object.keys(obj).length === 0;
      };

      expect(isEmptyObject({})).toBe(true);
      expect(isEmptyObject({ key: 'value' })).toBe(false);
      expect(isEmptyObject({ a: 1, b: 2 })).toBe(false);
    });

    it('debería hacer deep copy de objeto simple', () => {
      const deepCopy = <T>(obj: T): T => JSON.parse(JSON.stringify(obj));

      const original = { name: 'Test', data: { value: 123 } };
      const copy = deepCopy(original);
      
      copy.data.value = 456;
      
      expect(original.data.value).toBe(123);
      expect(copy.data.value).toBe(456);
    });
  });
});
