import { defineConfig } from 'vite';
import { fileURLToPath } from 'url';
import { dirname, resolve } from 'path';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

export default defineConfig({
  root: 'apps/demo-vanilla',
  server: { port: 5173 },
  resolve: {
    alias: {
      '@ticketflow/domain-core': resolve(__dirname, './packages/domain-core'),
      '@ticketflow/board-core': resolve(__dirname, './packages/board-core'),
      '@ticketflow/board-adapter-vanilla': resolve(__dirname, './packages/board-adapter-vanilla')
    }
  }
});