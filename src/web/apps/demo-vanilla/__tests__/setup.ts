// Test setup file
// Configuración global para todos los tests
import { beforeEach, vi } from 'vitest';

// Mock de import.meta.env para Vite
(globalThis as any).import = {
  meta: {
    env: {
      VITE_API_BASE_URL: 'http://localhost:5076',
    },
  },
};

// Mock de localStorage si no existe (happy-dom lo provee, pero por seguridad)
if (typeof localStorage === 'undefined') {
  const localStorageMock = (() => {
    let store: Record<string, string> = {};
    return {
      getItem: (key: string) => store[key] || null,
      setItem: (key: string, value: string) => {
        store[key] = value;
      },
      removeItem: (key: string) => {
        delete store[key];
      },
      clear: () => {
        store = {};
      },
    };
  })();
  Object.defineProperty(globalThis, 'localStorage', {
    value: localStorageMock,
  });
}

// Limpiar localStorage antes de cada test
beforeEach(() => {
  localStorage.clear();
});

// Mock de fetch global para tests que lo necesiten
globalThis.fetch = vi.fn();
