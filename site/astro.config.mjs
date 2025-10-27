/** @type {import('astro').AstroUserConfig} */
import { defineConfig } from 'astro/config';
import mdx from '@astrojs/mdx';
import sitemap from '@astrojs/sitemap';

export default defineConfig({
  site: 'https://www.maenifold.com/',
  base: '/',
  integrations: [mdx(), sitemap()],
  output: 'static',
  markdown: {
    shikiConfig: {
      theme: 'github-dark'
    }
  },
  vite: {
    build: { target: 'es2020' }
  }
});
