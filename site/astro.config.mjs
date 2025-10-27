/** @type {import('astro').AstroUserConfig} */
import { defineConfig } from 'astro/config';
import mdx from '@astrojs/mdx';
import sitemap from '@astrojs/sitemap';

export default defineConfig({
  site: 'https://msbrett.github.io/maenifold/',
  base: '/maenifold/',
  integrations: [mdx(), sitemap()],
  output: 'static',
  vite: {
    build: { target: 'es2020' }
  }
});
