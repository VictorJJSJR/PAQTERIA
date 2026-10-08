import EmptyState from '../components/EmptyState';
import { ErrorState, LoadingState } from '../components/DataStates';
import { useLiveQuery } from '../hooks/useLiveQuery';
import { useRealtime } from '../hooks/useRealtime';
import { PageHeader, PanelTitle } from './SummaryPage';

const emptySummary = {
    totalPackages: 0,
    packagesInTransit: 0,
    pendingPackages: 0,
    deliveredToday: 0,
    packagesUpdatedToday: 0,
    driversInTransit: 0,
    openIncidents: 0,
};

function ReportsPage() {
    const { connectionState } = useRealtime();
    const { data: summary, loading, error, refresh } = useLiveQuery('/api/dashboard/reports', emptySummary);
    const effectiveness = summary.packagesUpdatedToday === 0
        ? 0
        : (summary.deliveredToday / summary.packagesUpdatedToday) * 100;

    return <>
        <PageHeader title="Corte del día y reportes" subtitle="Consulta los totales disponibles en la base de datos." status={connectionState} />
        <section className="content-section">
            {error && <ErrorState message={error} onRetry={() => refresh().catch(() => {})} />}
            {loading && summary.totalPackages === 0 && <LoadingState />}
            <div className="kpi-grid report-kpis">
                <article className="kpi-card blue"><span>Paquetes registrados</span><strong>{summary.totalPackages}</strong><small>Todos los estados</small></article>
                <article className="kpi-card green"><span>Entregas de hoy</span><strong>{summary.deliveredToday}</strong><small>{effectiveness.toFixed(1)}% de movimientos de hoy</small></article>
                <article className="kpi-card orange"><span>Paquetes en ruta</span><strong>{summary.packagesInTransit}</strong><small>{summary.driversInTransit} repartidores en ruta</small></article>
                <article className="kpi-card red"><span>Incidencias abiertas</span><strong>{summary.openIncidents}</strong><small>Requieren seguimiento</small></article>
            </div>
            <div className="split-grid reports-grid">
                <section className="panel"><PanelTitle title="Resumen de estados" subtitle="Conteos actuales, actualizados en vivo" />
                    {summary.totalPackages === 0 ? <EmptyState title="Sin datos de paquetes" description="Los reportes se completarán al registrar paquetes." /> : <div className="report-bars">
                        <div><span>Pendientes</span><strong>{summary.pendingPackages}</strong><i><b style={{ width: `${summary.pendingPackages / summary.totalPackages * 100}%` }} /></i></div>
                        <div><span>En ruta</span><strong>{summary.packagesInTransit}</strong><i><b style={{ width: `${summary.packagesInTransit / summary.totalPackages * 100}%` }} /></i></div>
                        <div><span>Entregados hoy</span><strong>{summary.deliveredToday}</strong><i><b style={{ width: `${Math.min(summary.deliveredToday / summary.totalPackages * 100, 100)}%` }} /></i></div>
                    </div>}
                </section>
                <section className="panel"><PanelTitle title="Alcance del corte" />
                    <div className="report-note"><strong>Datos operativos actuales</strong><p>Este corte resume el estado vigente de paquetes, repartidores e incidencias. La app aún no conserva historial diario para comparar periodos anteriores.</p></div>
                </section>
            </div>
        </section>
    </>;
}

export default ReportsPage;
