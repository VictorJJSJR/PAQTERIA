import { useAuth } from '../auth/useAuth';

const operationsItems = [
    { id: 'summary', label: 'Resumen', icon: '▦' },
    { id: 'routes', label: 'Rutas', icon: '⌁' },
    { id: 'packages', label: 'Paquetes', icon: '▣' },
    { id: 'incidents', label: 'Incidencias', icon: '⚑' },
    { id: 'drivers', label: 'Repartidores', icon: '♙' },
];
const adminItems = [
    { id: 'reports', label: 'Reportes', icon: '▤' },
    { id: 'centers', label: 'Centros', icon: '⌂' },
    { id: 'users', label: 'Usuarios', icon: '♧' },
];

function Sidebar({ activeModule, onNavigate }) {
    const { user, logout } = useAuth();
    const isAdministrator = user.role === 'Administrator';
    const roleLabel = isAdministrator ? 'Administrador' : 'Encargado de almacén';
    const navigationItems = isAdministrator ? [...operationsItems, ...adminItems] : operationsItems;

    return <aside className="sidebar">
        <div className="brand-block">
            <span className="brand-name">RumboEnvíos</span>
            <span className="brand-caption">CONTROL TOWER</span>
        </div>
        <nav className="sidebar-nav" aria-label="Navegación principal">
            <span className="nav-section-label">OPERACIÓN</span>
            {navigationItems.map((item) => <button
                className={`nav-item ${activeModule === item.id ? 'active' : ''}`}
                key={item.id}
                onClick={() => onNavigate(item.id)}
                type="button">
                <span className="nav-icon" aria-hidden="true">{item.icon}</span>
                <span>{item.label}</span>
            </button>)}
        </nav>
        <div className="sidebar-account">
            <span className="sidebar-account-name">{user.name}</span>
            <span className="sidebar-account-role">{roleLabel}</span>
            <button className="logout-button" onClick={() => void logout()} type="button">Cerrar sesión</button>
        </div>
    </aside>;
}

export default Sidebar;
