const plugin = require('tailwindcss/plugin');

/** @type {import('tailwindcss').Config} */
module.exports = {
  content: ['./src/**/*.{astro,html,js,jsx,md,mdx,svelte,ts,tsx,vue}'],
  theme: {
    extend: {},
    gridArea: {
      sidebar: 'sidebar',
      header: 'header',
      content: 'content',
      logo: 'logo',
    },
  },
  plugins: [
    plugin(function ({ matchUtilities, theme }) {
      matchUtilities(
        {
          ga: (value) => ({
            gridArea: value,
          }),
        },
        { values: theme('gridArea') }
      );
    }),
  ],
};
