import { useState } from 'react';
import { postJson } from '../api/operationsClient';
import { useLiveQuery } from '../hooks/useLiveQuery';
import { ErrorState, LoadingState, SuccessMessage } from '../components/DataStates';
import EmptyState from '../components/EmptyState';
import { PageHeader } from './SummaryPage';

function CentersPage() {
    const { data: centers, loading, error, refresh } = useLiveQuery('/api/centers', []);
    const [saving, setSaving] = useState(false);
    const [actionError, setActionError] = useState('');
    const [message, setMessage] = useState('');

    async function handleCreate(event) {
        event.preventDefault();
        const formElement = event.currentTarget;
        const form = new FormData(formElement);
        setSaving(true);
        setActionError('');
        setMessage('');
        try {
            await postJson('/api/centers', {
                name: form.get('name'),
                city: form.get('city'),
                address: form.get('address'),
            });
            formElement.reset();
            setMessage('El centro quedó guardado en la base de datos.');
            await refresh().catch(() => setActionError('El centro se guardó, pero no se pudo actualizar la lista.'));
        } catch (cause) {
            setActionError(cause.message || 'No se pudo crear el centro.');
        } finally {
            setSaving(false);
        }
    }

    return <>
        <PageHeader title="Centros de distribución" subtitle="Los centros se usan al registrar o importar paquetes." />
        <section className="content-section">
            <form className="panel data-form" onSubmit={handleCreate}>
                <div className="form-heading"><h2>Agregar centro</h2><p>Solo los Administradores pueden cambiar este catálogo.</p></div>
                <div className="form-grid">
                    <label>Nombre<input name="name" maxLength="150" minLength="2" required /></label>
                    <label>Ciudad<input name="city" maxLength="100" minLength="2" required /></label>
                    <label>Dirección<input name="address" maxLength="255" minLength="2" required /></label>
                </div>
                {actionError && <ErrorState message={actionError} />}
                <div className="form-actions"><button className="primary-button" type="submit" disabled={saving}>{saving ? 'Guardando…' : 'Guardar centro'}</button></div>
            </form>
            <SuccessMessage>{message}</SuccessMessage>
            {error && <ErrorState message={error} onRetry={() => refresh().catch(() => {})} />}
            {loading && centers.length === 0 ? <LoadingState /> : <section className="panel table-panel">
                <div className="table-wrapper"><table><thead><tr><th>Centro</th><th>Ciudad</th><th>Dirección</th></tr></thead>
                    <tbody>{centers.map((center) => <tr key={center.id}><td>{center.name}</td><td>{center.city}</td><td>{center.address}</td></tr>)}</tbody>
                </table></div>
                {centers.length === 0 && <EmptyState title="No hay centros registrados" description="Agrega el centro de origen antes de registrar o importar paquetes." />}
            </section>}
        </section>
    </>;
}

export default CentersPage;
