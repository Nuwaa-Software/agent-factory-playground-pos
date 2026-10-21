import { defineConfig } from 'vitest/config';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  test: {
    environment: 'jsdom',
    globals: true, // Testing Library's automatic cleanup after each test needs it
    setupFiles: './src/test/setup.ts',
    // The agents' sandbox shares the CPU with a Postgres sidecar and the .NET build.
    testTimeout: 15_000,
  },
});
