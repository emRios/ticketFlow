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
      '@domain': resolve(__dirname, './packages/domain-core'),
      '@board-core': resolve(__dirname, './packages/board-core'),
      '@board-adapter': resolve(__dirname, './packages/board-adapter-vanilla')
    }
  }
});