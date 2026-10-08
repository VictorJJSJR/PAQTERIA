import EmptyState from '../components/EmptyState';
import { ErrorState, LoadingState } from '../components/DataStates';
import { formatDateTime } from '../api/operationsClient';
import { useLiveQuery } from '../hooks/useLiveQuery';
import { useRealtime } from '../hooks/useRealtime';

const emptySummary = {
    totalPackages: 0,
    packagesInTransit: 0,
    pendingPackages: 0,
    deliveredToday: 0,
    packagesUpdatedToday: 0,
    availableDrivers: 0,
    driversInTransit: 0,
    openIncidents: 0,
    recentPackages: [],
};

function SummaryPage() {
    const { connectionState } = useRealtime();
    const { data: summary, loading, error, refresh } = useLiveQuery('/api/dashboard', emptySummary);
    const effectiveness = summary.packagesUpdatedToday === 0
        ? 0
        : (summary.deliveredToday / summary.packagesUpdatedToday) * 100;
    const activePackages = summary.recentPackages.filter((item) => item.status === 'En ruta');
    const kpis = [
        { label: 'Paquetes en ruta', value: summary.packagesInTransit, detail: `${summary.totalPackages} registrados`, tone: 'blue' },
        { label: 'Entregas de hoy', value: summary.deliveredToday, detail: `${effectiveness.toFixed(1)}% de los movimientos de hoy`, tone: 'green' },
        { label: 'Pendientes', value: summary.pendingPackages, detail: `${summary.driversInTransit} repartidores en ruta`, tone: 'orange' },
        { label: 'Incidencias abiertas', value: summary.openIncidents, detail: 'Requieren seguimiento', tone: 'red' },
    ];

    return <>
        <PageHeader title="Resumen operativo" subtitle="Indicadores que se actualizan con los cambios de SQL Server." status={connectionState} />
        <section className="content-section">
            <div className="section-heading"><h2>Operación actual</h2><p>Datos consultados directamente de la base de datos.</p></div>
            {error && <ErrorState message={error} onRetry={() => refresh().catch(() => {})} />}
            {loading && summary.totalPackages === 0 && <LoadingState />}
            <div className="kpi-grid">
                {kpis.map((kpi) => <article className={`kpi-card ${kpi.tone}`} key={kpi.label}><span>{kpi.label}</span><strong>{kpi.value}</strong><small>{kpi.detail}</small></article>)}
            </div>
            <div className="split-grid summary-grid">
                <section className="panel"><PanelTitle title="Cobertura de paquetes" subtitle={`${summary.totalPackages} paquetes · ${summary.availableDrivers} repartidores disponibles`} />
                    {summary.recentPackages.length === 0 ? <EmptyState title="Sin paquetes disponibles" description="Los paquetes registrados aparecerán en este resumen." /> :
                        <div className="recent-list">{summary.recentPackages.map((item) => <article className="recent-row" key={item.id}>
                            <div><strong>{item.trackingNumber}</strong><span>{item.senderName} · {item.zone}</span></div>
                            <div className="recent-meta"><span className={`status-pill ${item.status === 'Entregado' ? 'complete' : item.status === 'Incidencia' ? 'problem' : ''}`}>{item.status}</span><small>{formatDateTime(item.updatedAt)}</small></div>
                        </article>)}</div>}
                </section>
                <section className="panel"><PanelTitle title="Paquetes en movimiento" subtitle={`${summary.driversInTransit} repartidores marcados En ruta`} />
                    {activePackages.length === 0 ? <EmptyState title="No hay paquetes en ruta" description="Cuando actualices un paquete a En ruta aparecerá aquí." /> :
                        <div className="recent-list">{activePackages.map((item) => <article className="recent-row" key={item.id}>
                            <div><strong>{item.trackingNumber}</strong><span>{item.senderName}</span></div><small>{item.zone}</small>
                        </article>)}</div>}
                </section>
            </div>
        </section>
    </>;
}

export function PageHeader({ title, subtitle, action, status = null }) {
    const statuses = {
        connected: { label: 'En vivo', className: 'connected' },
        connecting: { label: 'Conectando', className: 'connecting' },
        reconnecting: { label: 'Reconectando', className: 'connecting' },
        'database-unavailable': { label: 'Base sin conexión', className: 'disconnected' },
    };
    const liveStatus = statuses[status];
    return <header className="page-header"><div><h1>{title}</h1><p>{subtitle}</p></div><div className="header-actions">
        {liveStatus && <span className={`live-status ${liveStatus.className}`}><i />{liveStatus.label}</span>}{action}
    </div></header>;
}

export function PanelTitle({ title, subtitle }) {
    return <div className="panel-title"><h2>{title}</h2>{subtitle && <p>{subtitle}</p>}</div>;
}

export default SummaryPage;
