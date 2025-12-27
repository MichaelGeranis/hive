/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {
      colors: {
        primary: {
          50: '#fef7ec',
          100: '#fdecd3',
          200: '#f9d5a5',
          300: '#f5b86d',
          400: '#f09333',
          500: '#ec7511',
          600: '#dd5a08',
          700: '#b7400a',
          800: '#93330f',
          900: '#782c10',
          950: '#411405',
        },
        hive: {
          50: '#fffbeb',
          100: '#fff3c6',
          200: '#ffe588',
          300: '#ffd24a',
          400: '#ffbd20',
          500: '#f99b07',
          600: '#dd7302',
          700: '#b75006',
          800: '#943d0c',
          900: '#7a330d',
          950: '#461902',
        }
      }
    },
  },
  plugins: [],
}
