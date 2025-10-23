import { defineConfig } from 'vitest/config';
import path from 'path';

export default defineConfig({
  test: {
    globals: true,
    environment: 'happy-dom',
    setupFiles: ['./apps/demo-vanilla/__tests__/setup.ts'],
    coverage: {
      provider: 'v8',
      reporter: ['text', 'json', 'html'],
      exclude: [
        'node_modules/',
        'apps/demo-vanilla/__tests__/',
        'apps/demo-vanilla/dist/',
        'apps/demo-vanilla/legacy/',
        '**/*.config.*',
        '**/types.ts',
      ],
    },
    include: ['apps/demo-vanilla/__tests__/**/*.test.ts'],
  },
  resolve: {
    alias: {
      '@ticketflow/board-adapter-vanilla': path.resolve(__dirname, './packages/board-adapter-vanilla/index.ts'),
    },
  },
});
