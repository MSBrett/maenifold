// @ts-check
import { defineConfig } from 'astro/config';

import tailwindcss from '@tailwindcss/vite';

// https://astro.build/config
export default defineConfig({
  site: 'https://msbrett.github.io',
  base: '/maenifold',
  vite: {
    plugins: [tailwindcss()]
  }
});