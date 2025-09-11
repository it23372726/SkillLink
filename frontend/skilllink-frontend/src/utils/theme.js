export function applyTheme(theme) {
    const root = document.documentElement;
    if (theme === 'dark') {
      root.classList.add('dark');
      localStorage.setItem('theme', 'dark');
    } else {
      root.classList.remove('dark');
      localStorage.setItem('theme', 'light');
    }
  }
  
  export function initTheme() {
    const stored = localStorage.getItem('theme');
    const systemPrefersDark =
      window.matchMedia &&
      window.matchMedia('(prefers-color-scheme: dark)').matches;
  
    if (stored === 'dark' || (!stored && systemPrefersDark)) {
      applyTheme('dark');
    } else {
      applyTheme('light');
    }
  }
  
  export function toggleTheme() {
    const isDark = document.documentElement.classList.contains('dark');
    applyTheme(isDark ? 'light' : 'dark');
  }