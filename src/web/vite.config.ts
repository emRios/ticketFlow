import { defineConfig } from 'vite';
import { resolve } from 'path';

export default defineConfig({
  root: 'apps/demo-vanilla',
  server: { port: 5173 },
  build: {
    rollupOptions: {
      input: {
        index: resolve(__dirname, './apps/demo-vanilla/index.html'),
        ticket: resolve(__dirname, './apps/demo-vanilla/ticket.html'),
        admin: resolve(__dirname, './apps/demo-vanilla/admin.html'),
        activity: resolve(__dirname, './apps/demo-vanilla/activity.html'),
        login: resolve(__dirname, './apps/demo-vanilla/login.html'),
        'client-form': resolve(__dirname, './apps/demo-vanilla/client-form.html')
      }
    }
  },
  resolve: {
    alias: {
      '@ticketflow/domain-core': resolve(__dirname, './packages/domain-core'),
      '@ticketflow/board-core': resolve(__dirname, './packages/board-core'),
      '@ticketflow/board-adapter-vanilla': resolve(__dirname, './packages/board-adapter-vanilla')
    }
  }
});