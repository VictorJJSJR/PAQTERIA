import EmptyState from '../components/EmptyState';
import { ErrorState, LoadingState } from '../components/DataStates';
import { useLiveQuery } from '../hooks/useLiveQuery';
import { useRealtime } from '../hooks/useRealtime';
import { PageHeader, PanelTitle } from './SummaryPage';

const emptySummary = { totalPackages: 0, packagesInTransit: 0, pendingPackages: 0, availableDrivers: 0, driversInTransit: 0 };

function RoutesPage() {
    const { connectionState } = useRealtime();
    const { data: summary, loading, error, refresh } = useLiveQuery('/api/dashboard', emptySummary);
    const { data: packages } = useLiveQuery('/api/packages', []);
    const { data: drivers } = useLiveQuery('/api/drivers', []);
    const movingPackages = packages.filter((item) => item.status === 'En ruta');
    const movingDrivers = drivers.filter((item) => item.status === 'En ruta');

    return <>
        <PageHeader title="Planificador de rutas" subtitle="Consulta la demanda de paquetes y la disponibilidad del equipo." status={connectionState} />
        <section className="content-section">
            <div className="section-heading"><h2>Estado de la operación</h2><p>La información se lee de SQL Server y se sincroniza con las demás sesiones.</p></div>
            {error && <ErrorState message={error} onRetry={() => refresh().catch(() => {})} />}
            {loading && summary.totalPackages === 0 && <LoadingState />}
            <div className="split-grid routes-grid">
                <section className="panel coverage-panel"><PanelTitle title="Cobertura actual" subtitle={`${summary.totalPackages} paquetes · ${drivers.length} repartidores registrados`} />
                    <div className="route-metrics"><article><span>Paquetes pendientes</span><strong>{summary.pendingPackages}</strong></article><article><span>Paquetes en ruta</span><strong>{summary.packagesInTransit}</strong></article><article><span>Repartidores disponibles</span><strong>{summary.availableDrivers}</strong></article></div>
                    <div className="route-notice"><strong>Asignación de rutas pendiente</strong><p>El proyecto todavía no guarda una relación entre paquetes, repartidores y rutas. Los cambios de estado se reflejan aquí en tiempo real.</p></div>
                </section>
                <section className="panel"><PanelTitle title="Elementos en ruta" subtitle={`${movingPackages.length} paquetes · ${movingDrivers.length} repartidores`} />
                    {movingPackages.length === 0 && movingDrivers.length === 0 ? <EmptyState title="No hay elementos en ruta" description="Actualiza el estado de un paquete o repartidor para mostrarlo aquí." /> : <div className="recent-list">
                        {movingPackages.map((item) => <article className="recent-row" key={item.id}><div><strong>{item.trackingNumber}</strong><span>{item.senderName}</span></div><small>{item.zone}</small></article>)}
                        {movingDrivers.map((driver) => <article className="recent-row" key={driver.id}><div><strong>{driver.name}</strong><span>Repartidor en ruta</span></div><small>{driver.vehicle || 'Vehículo sin especificar'}</small></article>)}
                    </div>}
                </section>
            </div>
        </section>
    </>;
}

export default RoutesPage;
