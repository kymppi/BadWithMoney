import { defineConfig } from 'astro/config';

// https://astro.build/config
import tailwind from '@astrojs/tailwind';

// https://astro.build/config
import preact from '@astrojs/preact';

// https://astro.build/config
import node from '@astrojs/node';

// https://astro.build/config
export default defineConfig({
  integrations: [
    tailwind(),
    preact({
      compat: true,
    }),
  ],
  output: 'server',
  vite: {
    server: {
      proxy: {
        '/api': {
          target: 'http://localhost:5000',
          changeOrigin: true,
          secure: false,
        },
      },
    },
  },
  adapter: node({
    mode: 'standalone',
  }),
});
