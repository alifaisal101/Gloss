import { BrowserRouter, Routes, Route, NavLink, useLocation } from 'react-router-dom';
import Dashboard from './pages/Dashboard.jsx';
import MRDetail from './pages/MRDetail.jsx';
import Repositories from './pages/Repositories.jsx';
import Constitution from './pages/Constitution.jsx';
import Settings from './pages/Settings.jsx';
import NotFound from './pages/NotFound.jsx';
import ErrorBoundary from './components/ErrorBoundary.jsx';

const navLinkClass = ({ isActive }) => isActive ? 'nav-link active' : 'nav-link';

function Shell() {
  const location = useLocation();
  return (
    <div className="app">
      <nav className="sidebar">
        <div className="sidebar-logo">Gloss</div>
        <NavLink to="/" end className={navLinkClass}>Merge Requests</NavLink>
        <NavLink to="/repositories" className={navLinkClass}>Repositories</NavLink>
        <NavLink to="/constitution" className={navLinkClass}>Constitution</NavLink>
        <NavLink to="/settings" className={navLinkClass}>Settings</NavLink>
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
