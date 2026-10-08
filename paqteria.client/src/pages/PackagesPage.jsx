import { useState } from 'react';
import EmptyState from '../components/EmptyState';
import { ErrorState, LoadingState, SuccessMessage } from '../components/DataStates';
import FileImportPanel from '../components/FileImportPanel';
import QrCodeScanner from '../components/QrCodeScanner';
import { formatDateTime, postJson, putJson } from '../api/operationsClient';
import { useLiveQuery } from '../hooks/useLiveQuery';
import { useRealtime } from '../hooks/useRealtime';
import { PageHeader } from './SummaryPage';

const packageStatuses = ['Pendiente', 'En ruta', 'Entregado', 'Incidencia'];
const emptyOptions = { centers: [] };
const emptyPackageForm = {
    trackingNumber: '',
    senderName: '',
    originCenterId: '',
    originAddress: '',
    destinationAddress: '',
    destinationCoordinates: '',
    weightKg: '',
    labelSize: '',
    isPriority: false,
    isFragile: false,
};

function parsePackageQr(content, centers) {
    const text = content.trim();
    if (!text) return { error: 'El QR está vacío.' };

    if (!text.startsWith('{') && !text.startsWith('[')) {
        return text.length <= 50
            ? { updates: { trackingNumber: text } }
            : { error: 'El folio del QR de texto plano no puede superar los 50 caracteres.' };
    }

    let data;
    try {
        data = JSON.parse(text);
    } catch {
        return { error: 'El QR no contiene un JSON válido.' };
    }
    if (data === null || typeof data !== 'object' || Array.isArray(data))
        return { error: 'El QR JSON debe contener un objeto de paquete.' };
    if (data.version !== 1)
        return { error: 'La versión del QR no es compatible. Se requiere version 1.' };

    const requiredText = (value, label, maxLength, minLength = 1) => {
        if (typeof value !== 'string') return { error: `${label}: debe ser texto.` };
        const normalized = value.trim();
        if (normalized.length < minLength || normalized.length > maxLength)
            return { error: `${label}: debe tener entre ${minLength} y ${maxLength} caracteres.` };
        return { value: normalized };
    };
    const optionalText = (value, label, maxLength) => {
        if (value === undefined || value === null) return { value: '' };
        if (typeof value !== 'string') return { error: `${label}: debe ser texto o null.` };
        const normalized = value.trim();
        if (normalized.length > maxLength) return { error: `${label}: el máximo es ${maxLength} caracteres.` };
        return { value: normalized };
    };

    const trackingNumber = requiredText(data.trackingNumber, 'Folio', 50);
    const senderName = requiredText(data.senderName, 'Remitente', 150, 2);
    const originAddress = requiredText(data.originAddress, 'Dirección de origen', 255);
    const destinationAddress = requiredText(data.destinationAddress, 'Dirección de destino', 255);
    const destinationCoordinates = optionalText(data.destinationCoordinates, 'Coordenadas', 100);
    const labelSize = optionalText(data.labelSize, 'Tamaño de etiqueta', 50);
    for (const field of [trackingNumber, senderName, originAddress, destinationAddress,
        destinationCoordinates, labelSize]) {
        if (field.error) return { error: field.error };
    }

    if (!Number.isInteger(data.originCenterId) || data.originCenterId < 1)
        return { error: 'Centro de origen: originCenterId debe ser un ID numérico positivo.' };
    if (!centers.some((center) => center.id === data.originCenterId))
        return { error: `El centro con ID ${data.originCenterId} no existe en esta base de datos.` };
    if (typeof data.weightKg !== 'number' || !Number.isFinite(data.weightKg)
        || data.weightKg < 0.01 || data.weightKg > 99999999.99
        || Math.abs(data.weightKg * 100 - Math.round(data.weightKg * 100)) > 1e-7)
        return { error: 'Peso: usa un número entre 0.01 y 99999999.99, con máximo 2 decimales.' };
    if (data.isPriority !== undefined && typeof data.isPriority !== 'boolean')
        return { error: 'Prioritario: el valor debe ser true o false.' };
    if (data.isFragile !== undefined && typeof data.isFragile !== 'boolean')
        return { error: 'Frágil: el valor debe ser true o false.' };

    return {
        updates: {
            trackingNumber: trackingNumber.value,
            senderName: senderName.value,
            originCenterId: String(data.originCenterId),
            originAddress: originAddress.value,
            destinationAddress: destinationAddress.value,
            destinationCoordinates: destinationCoordinates.value,
            weightKg: String(data.weightKg),
            labelSize: labelSize.value,
            isPriority: data.isPriority ?? false,
            isFragile: data.isFragile ?? false,
        },
    };
}

function PackagesPage() {
    const [search, setSearch] = useState('');
    const [formOpen, setFormOpen] = useState(false);
    const [scannerOpen, setScannerOpen] = useState(false);
    const [packageForm, setPackageForm] = useState(emptyPackageForm);
    const [importOpen, setImportOpen] = useState(false);
    const [saving, setSaving] = useState(false);
    const [actionError, setActionError] = useState('');
    const [message, setMessage] = useState('');
    const { connectionState } = useRealtime();
    const { data: packages, loading, error, refresh } = useLiveQuery(
        `/api/packages?search=${encodeURIComponent(search)}`,
        [],
    );
    const { data: options, error: optionsError } = useLiveQuery('/api/lookups/package-options', emptyOptions);

    async function handleCreate(event) {
        event.preventDefault();
        setSaving(true);
        setActionError('');
        setMessage('');
        try {
            await postJson('/api/packages', {
                trackingNumber: packageForm.trackingNumber.trim(),
                senderName: packageForm.senderName.trim(),
                originCenterId: Number(packageForm.originCenterId),
                originAddress: packageForm.originAddress.trim(),
                destinationAddress: packageForm.destinationAddress.trim(),
                destinationCoordinates: packageForm.destinationCoordinates.trim() || null,
                weightKg: Number(packageForm.weightKg),
                labelSize: packageForm.labelSize.trim() || null,
                isPriority: packageForm.isPriority,
                isFragile: packageForm.isFragile,
            });
            setPackageForm(emptyPackageForm);
            setScannerOpen(false);
            setFormOpen(false);
            setMessage('El paquete se guardó en la base de datos.');
            await refresh().catch(() => setActionError('El paquete se guardó, pero no se pudo actualizar la lista.'));
        } catch (cause) {
            setActionError(cause.message || 'No se pudo guardar el paquete.');
        } finally {
            setSaving(false);
        }
    }

    function handleQrDetected(content) {
        const result = parsePackageQr(content, options.centers);
        if (result.error) return result.error;
        setPackageForm((current) => ({ ...current, ...result.updates }));
        setActionError('');
        setMessage('Datos cargados desde el QR. Revísalos antes de guardar el paquete.');
        setScannerOpen(false);
        return null;
    }

    function handleFormChange(event) {
        const { name, type, checked, value } = event.target;
        setPackageForm((current) => ({ ...current, [name]: type === 'checkbox' ? checked : value }));
    }

    function toggleForm() {
        const nextOpen = !formOpen;
        if (!nextOpen) setPackageForm(emptyPackageForm);
        setFormOpen(nextOpen);
        setScannerOpen(false);
    }

    async function handleStatusChange(item, status) {
        setActionError('');
        try {
            await putJson(`/api/packages/${item.id}/status`, { status, rowVersion: item.rowVersion });
            setMessage(`Se actualizó el estado del paquete ${item.trackingNumber}.`);
            await refresh().catch(() => setActionError('El cambio se guardó, pero no se pudo actualizar la lista.'));
        } catch (cause) {
            setActionError(cause.message || 'No se pudo actualizar el paquete.');
        }
    }

    return <>
        <PageHeader title="Gestión de paquetes" subtitle="Consulta y actualiza paquetes guardados en dbo.PAQUETES." status={connectionState} action={
            <div className="header-button-group">
                <button className="secondary-button" onClick={() => setImportOpen((open) => !open)} type="button">{importOpen ? 'Cerrar importación' : 'Importar CSV / Excel'}</button>
                <button className="primary-button" onClick={toggleForm} type="button">{formOpen ? 'Cancelar' : '+ Registrar paquete'}</button>
            </div>
        } />
        <section className="content-section">
            {importOpen && <FileImportPanel entity="packages" label="paquetes" onImported={refresh} />}
            {formOpen && <form className="panel data-form" onSubmit={handleCreate}>
                <div className="form-heading"><h2>Registrar paquete</h2><p>Escribe el nombre de quien envía el paquete y selecciona el centro de origen. El QR puede contener el folio o los datos completos del paquete.</p></div>
                {optionsError && <ErrorState message={`No se pudieron cargar los centros: ${optionsError}`} />}
                {options.centers.length === 0
                    ? <p className="setup-note">Para registrar paquetes, el Administrador debe agregar al menos un centro de distribución.</p>
                    : <>
                        <div className="form-grid package-form-grid">
                            <div className="package-tracking-field"><label htmlFor="package-tracking-number">Folio</label><div className="tracking-input-actions">
                                <input id="package-tracking-number" name="trackingNumber" value={packageForm.trackingNumber} onChange={handleFormChange} maxLength="50" required placeholder="Ej. PQ-2026-0001" />
                                <button className="secondary-button" onClick={() => setScannerOpen(true)} type="button">Escanear QR</button>
                            </div>{scannerOpen && <QrCodeScanner onDetected={handleQrDetected} onClose={() => setScannerOpen(false)} />}</div>
                            <label>Remitente<input name="senderName" value={packageForm.senderName} onChange={handleFormChange} maxLength="150" minLength="2" required placeholder="Persona o empresa que envía" /></label>
                            <label>Centro de origen<select name="originCenterId" value={packageForm.originCenterId} onChange={handleFormChange} required><option value="" disabled>Selecciona un centro</option>{options.centers.map((center) => <option key={center.id} value={center.id}>{center.name} · {center.city}</option>)}</select></label>
                            <label>Dirección de origen<input name="originAddress" value={packageForm.originAddress} onChange={handleFormChange} maxLength="255" required /></label>
                            <label>Dirección de destino<input name="destinationAddress" value={packageForm.destinationAddress} onChange={handleFormChange} maxLength="255" required /></label>
                            <label>Coordenadas<input name="destinationCoordinates" value={packageForm.destinationCoordinates} onChange={handleFormChange} maxLength="100" placeholder="Opcional" /></label>
                            <label>Peso (kg)<input name="weightKg" value={packageForm.weightKg} onChange={handleFormChange} type="number" min="0.01" max="99999999.99" step="0.01" required /></label>
                            <label>Tamaño de etiqueta<input name="labelSize" value={packageForm.labelSize} onChange={handleFormChange} maxLength="50" placeholder="Opcional" /></label>
                            <label className="checkbox-field"><input name="isPriority" checked={packageForm.isPriority} onChange={handleFormChange} type="checkbox" /> Prioritario</label>
                            <label className="checkbox-field"><input name="isFragile" checked={packageForm.isFragile} onChange={handleFormChange} type="checkbox" /> Frágil</label>
                        </div>
                        {actionError && <ErrorState message={actionError} />}
                        <div className="form-actions"><button className="primary-button" disabled={saving} type="submit">{saving ? 'Guardando…' : 'Guardar paquete'}</button></div>
                    </>}
            </form>}
            <SuccessMessage>{message}</SuccessMessage>
            <div className="search-box"><span>⌕</span><input value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Buscar por folio, remitente o dirección" aria-label="Buscar paquetes" /></div>
            {error && <ErrorState message={error} onRetry={() => refresh().catch(() => {})} />}
            {actionError && !formOpen && <ErrorState message={actionError} />}
            {loading && packages.length === 0 ? <LoadingState /> : <section className="panel table-panel">
                <div className="table-wrapper"><table><thead><tr><th>Folio</th><th>Remitente</th><th>Centro</th><th>Destino</th><th>Peso</th><th>Estado</th><th>Último cambio</th></tr></thead>
                    <tbody>{packages.map((item) => <tr key={item.id}>
                        <td>{item.trackingNumber}</td><td>{item.senderName}</td><td>{item.originCenterName}</td>
                        <td className="destination-cell">{item.destinationAddress}</td><td>{item.weightKg} kg</td>
                        <td><select className="status-select" aria-label={`Estado de ${item.trackingNumber}`} value={item.status} onChange={(event) => void handleStatusChange(item, event.target.value)}>
                            {[...new Set([...packageStatuses, item.status])].map((status) => <option key={status} value={status}>{status}</option>)}
                        </select></td><td>{formatDateTime(item.updatedAt || item.createdAt)}</td>
                    </tr>)}</tbody></table></div>
                {packages.length === 0 && <EmptyState title={search ? 'Sin resultados' : 'Sin paquetes disponibles'} description={search ? 'No se encontraron paquetes con esa búsqueda.' : 'Los registros de dbo.PAQUETES aparecerán aquí.'} />}
            </section>}
        </section>
    </>;
}

export default PackagesPage;
