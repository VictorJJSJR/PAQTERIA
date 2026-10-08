import { useState } from 'react';
import './App.css';
import Sidebar from './components/Sidebar';
import SummaryPage from './pages/SummaryPage';
import RoutesPage from './pages/RoutesPage';
import PackagesPage from './pages/PackagesPage';
import IncidentsPage from './pages/IncidentsPage';
import DriversPage from './pages/DriversPage';
import ReportsPage from './pages/ReportsPage';
import UsersPage from './pages/UsersPage';
import CentersPage from './pages/CentersPage';
import LoginPage from './pages/LoginPage';
import { AuthProvider } from './auth/AuthProvider';
import { useAuth } from './auth/useAuth';
import { RealtimeProvider } from './realtime/RealtimeProvider';

function AppContent() {
    const { user } = useAuth();
    const [activeModule, setActiveModule] = useState('summary');
    const isAdministrator = user.role === 'Administrator';
    const pages = {
        summary: <SummaryPage />,
        routes: <RoutesPage />,
        packages: <PackagesPage />,
        incidents: <IncidentsPage />,
        drivers: <DriversPage />,
        ...(isAdministrator ? { reports: <ReportsPage />, centers: <CentersPage />, users: <UsersPage /> } : {}),
    };

    const visibleModule = !isAdministrator && ['reports', 'centers', 'users'].includes(activeModule)
        ? 'summary'
        : activeModule;

    return <div className="app-shell">
        <Sidebar activeModule={visibleModule} onNavigate={setActiveModule} />
        <main className="main-content">{pages[visibleModule]}</main>
    </div>;
}

function AuthenticatedApp() {
    const { user, loading } = useAuth();
    if (loading) return <main className="login-screen"><div className="login-loading">Validando sesión…</div></main>;
    if (!user) return <LoginPage />;
    return <RealtimeProvider><AppContent /></RealtimeProvider>;
}

function App() {
    return <AuthProvider><AuthenticatedApp /></AuthProvider>;
}

export default App;
