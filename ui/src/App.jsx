import { BrowserRouter, Routes, Route, NavLink } from 'react-router-dom';
import Dashboard from './pages/Dashboard.jsx';
import MRDetail from './pages/MRDetail.jsx';
import Repositories from './pages/Repositories.jsx';
import Constitution from './pages/Constitution.jsx';
import Settings from './pages/Settings.jsx';

export default function App() {
  return (
    <BrowserRouter>
      <div className="app">
        <nav className="sidebar">
          <div className="sidebar-logo">Gloss</div>
          <NavLink to="/" end className={({ isActive }) => isActive ? 'nav-link active' : 'nav-link'}>
            Dashboard
          </NavLink>
          <NavLink to="/repositories" className={({ isActive }) => isActive ? 'nav-link active' : 'nav-link'}>
            Repositories
          </NavLink>
          <NavLink to="/constitution" className={({ isActive }) => isActive ? 'nav-link active' : 'nav-link'}>
            Constitution
          </NavLink>
          <NavLink to="/settings" className={({ isActive }) => isActive ? 'nav-link active' : 'nav-link'}>
            Settings
          </NavLink>
        </nav>
        <main className="content">
          <Routes>
            <Route path="/" element={<Dashboard />} />
            <Route path="/mr/:id" element={<MRDetail />} />
            <Route path="/repositories" element={<Repositories />} />
            <Route path="/constitution" element={<Constitution />} />
            <Route path="/settings" element={<Settings />} />
          </Routes>
        </main>
      </div>
    </BrowserRouter>
  );
}
