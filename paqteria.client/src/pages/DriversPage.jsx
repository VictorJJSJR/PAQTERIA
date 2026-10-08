import { useMemo, useState } from 'react';
import EmptyState from '../components/EmptyState';
import { ErrorState, LoadingState } from '../components/DataStates';
import { formatDateTime } from '../api/operationsClient';
import { useLiveQuery } from '../hooks/useLiveQuery';
import { useRealtime } from '../hooks/useRealtime';
import { PageHeader } from './SummaryPage';

function DriversPage() {
    const [filter, setFilter] = useState('Todos');
    const { connectionState } = useRealtime();
    const { data: drivers, loading, error, refresh } = useLiveQuery('/api/drivers', []);
    const counts = useMemo(() => ({
        Todos: drivers.length,
        'En ruta': drivers.filter((driver) => driver.status === 'En ruta').length,
        'En descanso': drivers.filter((driver) => driver.status === 'En descanso').length,
        Disponibles: drivers.filter((driver) => driver.status === 'Disponible').length,
    }), [drivers]);
    const filteredDrivers = filter === 'Todos' ? drivers : drivers.filter((driver) => driver.status === filter);

    return <>
        <PageHeader title="Repartidores" subtitle="Cuentas de repartidor y turnos registrados en SQL Server." status={connectionState} />
        <section className="content-section">
            <div className="status-filters" aria-label="Filtrar repartidores por estado">
                {Object.entries(counts).map(([status, count]) => <button className={`filter-chip ${filter === status ? 'selected' : ''}`} key={status} onClick={() => setFilter(status)} type="button">{status} ({count})</button>)}
            </div>
            {error && <ErrorState message={error} onRetry={() => refresh().catch(() => {})} />}
            {loading && drivers.length === 0 ? <LoadingState /> : <section className="panel">
                {filteredDrivers.length === 0 ? <EmptyState title={filter === 'Todos' ? 'Sin repartidores registrados' : `Sin repartidores ${filter.toLowerCase()}`} description="Las cuentas con rol Repartidor aparecerán aquí; el turno y el vehículo se consultan desde la base de datos." /> :
                    <div className="driver-grid">{filteredDrivers.map((driver) => <article className="driver-card" key={driver.id}>
                        <div><strong>{driver.name}</strong><p>{driver.vehicle || 'Sin unidad asignada'}</p></div>
                        <span className={`driver-status ${driver.status === 'En ruta' ? 'in-transit' : ''}`}>{driver.status}</span>
                        <small>{driver.updatedAt ? `Turno ${formatDateTime(driver.updatedAt)}` : 'Sin turno registrado'}</small>
                    </article>)}</div>}
            </section>}
        </section>
    </>;
}

export default DriversPage;
