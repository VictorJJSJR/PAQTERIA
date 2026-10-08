import { useMemo, useState } from 'react';
import EmptyState from '../components/EmptyState';
import { ErrorState, LoadingState, SuccessMessage } from '../components/DataStates';
import { formatDateTime, postJson, putJson } from '../api/operationsClient';
import { useLiveQuery } from '../hooks/useLiveQuery';
import { useRealtime } from '../hooks/useRealtime';
import { PageHeader, PanelTitle } from './SummaryPage';

const incidentStatuses = ['Abierta', 'En atención', 'Resuelta'];
const incidentSeverities = ['Baja', 'Media', 'Alta', 'Crítica'];
const severityRank = { Crítica: 0, Alta: 1, Media: 2, Baja: 3 };

function IncidentsPage() {
    const [formOpen, setFormOpen] = useState(false);
    const [selectedId, setSelectedId] = useState(null);
    const [saving, setSaving] = useState(false);
    const [actionError, setActionError] = useState('');
    const [message, setMessage] = useState('');
    const { connectionState } = useRealtime();
    const { data: incidents, loading, error, refresh } = useLiveQuery('/api/incidents', []);
    const { data: packages, error: packageError } = useLiveQuery('/api/packages', []);
    const { data: drivers, error: driverError } = useLiveQuery('/api/drivers', []);
    const orderedIncidents = useMemo(() => [...incidents].sort((left, right) => {
        const statusOrder = Number(left.status === 'Resuelta') - Number(right.status === 'Resuelta');
        return statusOrder || severityRank[left.severity] - severityRank[right.severity]
            || new Date(right.updatedAt || right.createdAt) - new Date(left.updatedAt || left.createdAt);
    }), [incidents]);
    const selectedIncident = orderedIncidents.find((incident) => incident.id === selectedId) || null;

    async function handleCreate(event) {
        event.preventDefault();
        setSaving(true);
        setActionError('');
        setMessage('');
        const formElement = event.currentTarget;
        const form = new FormData(formElement);
        try {
            await postJson('/api/incidents', {
                packageId: Number(form.get('packageId')),
                driverId: Number(form.get('driverId')),
                title: form.get('title'),
                description: form.get('description') || null,
                severity: form.get('severity'),
            });
            formElement.reset();
            setFormOpen(false);
            setMessage('La incidencia se guardó en SQL Server.');
            await refresh().catch(() => setActionError('La incidencia se guardó, pero no se pudo actualizar la bandeja.'));
        } catch (cause) {
            setActionError(cause.message || 'No se pudo guardar la incidencia.');
        } finally {
            setSaving(false);
        }
    }

    async function handleStatusChange(incident, status) {
        setActionError('');
        try {
            await putJson(`/api/incidents/${incident.id}/status`, { status, rowVersion: incident.rowVersion });
            setMessage(`Se actualizó la incidencia «${incident.title}».`);
            await refresh().catch(() => setActionError('El cambio se guardó, pero no se pudo actualizar la bandeja.'));
        } catch (cause) {
            setActionError(cause.message || 'No se pudo actualizar la incidencia.');
        }
    }

    return <>
        <PageHeader title="Control de excepciones" subtitle="Incidencias de entrega y seguimiento almacenados en SQL Server." status={connectionState} action={
            <button className="primary-button" onClick={() => setFormOpen((open) => !open)} type="button">{formOpen ? 'Cancelar' : '+ Registrar incidencia'}</button>
        } />
        <section className="content-section">
            {formOpen && <form className="panel data-form" onSubmit={handleCreate}>
                <div className="form-heading"><h2>Nueva incidencia</h2><p>La tabla registra el paquete, repartidor, estado, severidad y detalle.</p></div>
                {(packageError || driverError) && <ErrorState message={`No se pudieron cargar paquetes o repartidores: ${packageError || driverError}`} />}
                <div className="form-grid incident-form-grid">
                    <label>Descripción breve<input name="title" maxLength="100" required placeholder="Ej. Domicilio incompleto" /></label>
                    <label>Severidad<select name="severity" defaultValue="Media">{incidentSeverities.map((severity) => <option key={severity}>{severity}</option>)}</select></label>
                    <label>Paquete<select name="packageId" defaultValue="" required><option value="" disabled>Selecciona un paquete</option>{packages.map((item) => <option key={item.id} value={item.id}>{item.trackingNumber} · {item.senderName}</option>)}</select></label>
                    <label>Repartidor<select name="driverId" defaultValue="" required><option value="" disabled>Selecciona un repartidor</option>{drivers.map((driver) => <option key={driver.id} value={driver.id}>{driver.name}</option>)}</select></label>
                    <label className="wide-field">Detalle<textarea name="description" maxLength="2000" rows="3" placeholder="Agrega contexto para el equipo (opcional)" /></label>
                </div>
                {packages.length === 0 && <p className="setup-note">Registra un paquete antes de crear una incidencia.</p>}
                {drivers.length === 0 && <p className="setup-note">Crea una cuenta con rol Repartidor antes de crear una incidencia.</p>}
                {actionError && <ErrorState message={actionError} />}
                <div className="form-actions"><button className="primary-button" disabled={saving || packages.length === 0 || drivers.length === 0} type="submit">{saving ? 'Guardando…' : 'Guardar incidencia'}</button></div>
            </form>}
            <SuccessMessage>{message}</SuccessMessage>
            {error && <ErrorState message={error} onRetry={() => refresh().catch(() => {})} />}
            {actionError && !formOpen && <ErrorState message={actionError} />}
            {loading && incidents.length === 0 ? <LoadingState /> : <div className="split-grid incidents-grid">
                <section className="panel incident-list-panel"><PanelTitle title="Bandeja de incidencias" subtitle="Las abiertas de mayor severidad aparecen primero." />
                    {orderedIncidents.length === 0 ? <EmptyState title="No hay incidencias registradas" description="Los registros de dbo.INCIDENCIAS_ENTREGA aparecerán aquí." /> :
                        <div className="incident-list">{orderedIncidents.map((incident) => <button className={`incident-item ${selectedId === incident.id ? 'selected' : ''}`} key={incident.id} onClick={() => setSelectedId(incident.id)} type="button">
                            <span className={`severity-dot severity-${incident.severity.toLowerCase()}`} />
                            <span className="incident-item-copy"><strong>{incident.title}</strong><small>{incident.severity} · {incident.status} · {incident.trackingNumber}</small></span>
                            <span className="incident-chevron">›</span>
                        </button>)}</div>}
                </section>
                <section className="panel detail-panel"><PanelTitle title="Detalle de incidencia" />
                    {selectedIncident === null ? <EmptyState title="Selecciona una incidencia" description="El detalle estará disponible al seleccionar un registro." /> : <article className="incident-detail">
                        <span className={`severity-badge severity-badge-${selectedIncident.severity.toLowerCase()}`}>{selectedIncident.severity}</span>
                        <h3>{selectedIncident.title}</h3>
                        <p>{selectedIncident.description || 'Sin detalle adicional.'}</p>
                        <dl><div><dt>Paquete</dt><dd>{selectedIncident.trackingNumber}</dd></div><div><dt>Repartidor</dt><dd>{selectedIncident.driverName}</dd></div><div><dt>Creada</dt><dd>{formatDateTime(selectedIncident.createdAt)}</dd></div><div><dt>Última actualización</dt><dd>{formatDateTime(selectedIncident.updatedAt || selectedIncident.createdAt)}</dd></div></dl>
                        <label>Estado<select className="status-select" value={selectedIncident.status} onChange={(event) => void handleStatusChange(selectedIncident, event.target.value)}>
                            {[...new Set([...incidentStatuses, selectedIncident.status])].map((status) => <option key={status} value={status}>{status}</option>)}
                        </select></label>
                    </article>}
                </section>
            </div>}
        </section>
    </>;
}

export default IncidentsPage;
