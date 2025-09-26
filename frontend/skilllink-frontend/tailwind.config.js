/** @type {import('tailwindcss').Config} */
module.exports = {
  darkMode: 'class',                              // allow toggle or system dark mode
  content: ['./src/**/*.{js,jsx,ts,tsx}'],
  theme: {
    extend: {
      fontFamily: {
        sans: [
          '-apple-system', 'BlinkMacSystemFont', 'Segoe UI', 
          'Inter', 'Roboto', 'Helvetica Neue', 'Arial', 'Noto Sans', 'Apple Color Emoji', 'Segoe UI Emoji', 'Segoe UI Symbol', 'sans-serif'
        ],
      },
      colors: {
        mac: {
          glassLight: 'rgba(255,255,255,0.55)',
          glassDark: 'rgba(17, 25, 40, 0.55)',
          strokeLight: 'rgba(255,255,255,0.35)',
          strokeDark: 'rgba(255,255,255,0.08)',
        },
        ink: {
          50: '#F8FAFC',
          100: '#F1F5F9',
          200: '#E2E8F0',
          300: '#CBD5E1',
          400: '#94A3B8',
          500: '#64748B',
          600: '#475569',
          700: '#334155',
          800: '#1E293B',
          900: '#0F172A',
        },
      },
      boxShadow: {
        glass: '0 10px 30px rgba(0,0,0,0.10)',
        focus: '0 0 0 5px rgba(59, 130, 246, 0.25)',
      },
      backdropBlur: {
        xs: '2px',
        sm: '4px',
        xl: '16px',
        '2xl': '24px',
      },
      borderRadius: {
        xl: '16px',
        '2xl': '24px',
      },
    },
  },
  plugins: [],
};
