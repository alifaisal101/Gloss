import { useState, useEffect } from 'react';
import { BrowserRouter, Routes, Route, NavLink, useLocation } from 'react-router-dom';
import { Toaster } from 'sonner';
import { GitPullRequest, FolderGit2, ScrollText, Settings as SettingsIcon, Sun, Moon } from 'lucide-react';
import Dashboard from './pages/Dashboard.jsx';
import MRDetail from './pages/MRDetail.jsx';
import Repositories from './pages/Repositories.jsx';
import Constitution from './pages/Constitution.jsx';
import Settings from './pages/Settings.jsx';
import NotFound from './pages/NotFound.jsx';
import ErrorBoundary from './components/ErrorBoundary.jsx';

const navLinkClass = ({ isActive }) => (isActive ? 'nav-link active' : 'nav-link');

function useTheme() {
  const [theme, setTheme] = useState(() => localStorage.getItem('gloss-theme') || 'dark');
  useEffect(() => {
    document.documentElement.dataset.theme = theme;
    localStorage.setItem('gloss-theme', theme);
  }, [theme]);
  return [theme, setTheme];
}

function Shell() {
  const location = useLocation();
  const [theme, setTheme] = useTheme();
  return (
    <div className="app">
      <nav className="sidebar">
        <div className="sidebar-logo">Gloss</div>
        <NavLink to="/" end className={navLinkClass}><GitPullRequest size={16} /><span>Merge Requests</span></NavLink>
        <NavLink to="/repositories" className={navLinkClass}><FolderGit2 size={16} /><span>Repositories</span></NavLink>
        <NavLink to="/constitution" className={navLinkClass}><ScrollText size={16} /><span>Constitution</span></NavLink>
        <NavLink to="/settings" className={navLinkClass}><SettingsIcon size={16} /><span>Settings</span></NavLink>
        <div className="sidebar-spacer" />
        <div className="sidebar-bottom">
          <button
            className="theme-toggle"
            onClick={() => setTheme((t) => (t === 'dark' ? 'light' : 'dark'))}
            aria-label="Toggle color theme"
          >
            {theme === 'dark' ? <Sun size={16} /> : <Moon size={16} />}
            <span>{theme === 'dark' ? 'Light mode' : 'Dark mode'}</span>
          </button>
        </div>
      </nav>
      <main className="content">
        <ErrorBoundary key={location.pathname}>
          <Routes>
            <Route path="/" element={<Dashboard />} />
            <Route path="/mr/:id" element={<MRDetail />} />
            <Route path="/repositories" element={<Repositories />} />
            <Route path="/constitution" element={<Constitution />} />
            <Route path="/settings" element={<Settings />} />
            <Route path="*" element={<NotFound />} />
          </Routes>
        </ErrorBoundary>
      </main>
      <Toaster position="bottom-right" theme={theme} richColors closeButton />
    </div>
  );
}

export default function App() {
  return (
    <BrowserRouter>
      <Shell />
    </BrowserRouter>
  );
}
