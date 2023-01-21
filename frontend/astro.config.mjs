import { defineConfig } from 'astro/config';

// https://astro.build/config
import tailwind from '@astrojs/tailwind';

// https://astro.build/config
export default defineConfig({
  integrations: [tailwind()],
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
});
