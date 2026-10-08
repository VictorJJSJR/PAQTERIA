import { useState } from 'react';
import { postForm } from '../api/operationsClient';
import { ErrorState, SuccessMessage } from './DataStates';

const templateLabels = { csv: 'Descargar CSV', xlsx: 'Descargar Excel' };

function FileImportPanel({ entity, label, templateFiles = ['csv', 'xlsx'], onImported }) {
    const [file, setFile] = useState(null);
    const [saving, setSaving] = useState(false);
    const [error, setError] = useState('');
    const [rowErrors, setRowErrors] = useState([]);
    const [truncated, setTruncated] = useState(false);
    const [success, setSuccess] = useState('');
    const [inputVersion, setInputVersion] = useState(0);

    async function handleImport(event) {
        event.preventDefault();
        if (!file) {
            setError('Selecciona un archivo CSV o XLSX.');
            return;
        }

        setSaving(true);
        setError('');
        setRowErrors([]);
        setSuccess('');
        setTruncated(false);
        const formData = new FormData();
        formData.append('file', file);
        try {
            const result = await postForm(`/api/import/${entity}`, formData);
            setSuccess(`Se importaron ${result.imported} ${label.toLowerCase()} correctamente.`);
            setFile(null);
            setInputVersion((version) => version + 1);
            if (onImported) {
                try { await onImported(); }
                catch { setError('La importación se guardó, pero la vista no se pudo actualizar.'); }
            }
        } catch (cause) {
            setError(cause.importErrors?.length
                ? 'El archivo tiene filas con errores; no se importó ningún registro.'
                : (cause.message || 'No se pudo importar el archivo.'));
            setRowErrors(cause.importErrors || []);
            setTruncated(cause.truncated || false);
            setInputVersion((version) => version + 1);
        } finally {
            setSaving(false);
        }
    }

    return <section className="panel data-form file-import-panel">
        <div className="form-heading"><h2>Importar {label.toLowerCase()}</h2><p>Usa un CSV UTF-8 o Excel XLSX. La primera fila debe contener encabezados y se admiten hasta 5 MB y 5,000 filas.</p></div>
        <div className="template-links">{templateFiles.map((format) => <a key={format} href={`/api/import/templates/${entity}/${format}`}>{templateLabels[format]}</a>)}</div>
        <form onSubmit={handleImport}>
            <label className="file-picker">Archivo de datos<input key={inputVersion} type="file" accept=".csv,.xlsx,text/csv,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" onChange={(event) => { setFile(event.target.files?.[0] || null); setError(''); setRowErrors([]); setSuccess(''); }} /></label>
            {file && <p className="selected-file">{file.name} · {(file.size / 1024).toFixed(0)} KB</p>}
            {error && <ErrorState message={error} />}
            {rowErrors.length > 0 && <ul className="import-errors">{rowErrors.map((item, index) => <li key={`${item.row}-${index}`}><strong>Fila {item.row}:</strong> {item.message}</li>)}</ul>}
            {truncated && <p className="import-error-note">Se muestran los primeros errores; corrige los que aparecen y vuelve a importar para ver los demás.</p>}
            <SuccessMessage>{success}</SuccessMessage>
            <div className="form-actions"><button className="primary-button" disabled={saving || !file} type="submit">{saving ? 'Validando y guardando…' : `Importar ${label.toLowerCase()}`}</button></div>
        </form>
    </section>;
}

export default FileImportPanel;
