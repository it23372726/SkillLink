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
  
  export const toggleTheme = () => {
    const el = document.documentElement;
    const dark = el.classList.toggle('dark');
    try { localStorage.setItem('theme', dark ? 'dark' : 'light'); } catch {}
    return dark;
  };
  export const ensureThemeFromStorage = () => {
    try {
      const t = localStorage.getItem('theme');
      if (t === 'dark') document.documentElement.classList.add('dark');
      else document.documentElement.classList.remove('dark');
    } catch {}
  };
  
