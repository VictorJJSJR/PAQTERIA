import { useState } from 'react';
import { postJson } from '../api/operationsClient';
import { useLiveQuery } from '../hooks/useLiveQuery';
import { ErrorState, LoadingState, SuccessMessage } from '../components/DataStates';
import EmptyState from '../components/EmptyState';
import { PageHeader } from './SummaryPage';

const roles = [
    ['Administrator', 'Administrador'],
    ['Warehouse Manager', 'Encargado de almacén'],
    ['Driver', 'Repartidor'],
];
const roleLabels = {
    Administrator: 'Administrador',
    'Warehouse Manager': 'Encargado de almacén',
    Customer: 'Cliente',
    Driver: 'Repartidor',
};

function UsersPage() {
    const { data: users, loading, error, refresh } = useLiveQuery('/api/users', []);
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
            await postJson('/api/users', {
                name: form.get('name'),
                email: form.get('email'),
                phone: form.get('phone') || null,
                role: form.get('role'),
                password: form.get('password'),
            });
            formElement.reset();
            setMessage('La cuenta se creó correctamente.');
            await refresh().catch(() => setActionError('La cuenta se guardó, pero no se pudo actualizar la lista.'));
        } catch (cause) {
            setActionError(cause.message || 'No se pudo crear la cuenta.');
        } finally {
            setSaving(false);
        }
    }

    return <>
        <PageHeader title="Usuarios y roles" subtitle="Crea cuentas y asigna los permisos almacenados en SQL Server." />
        <section className="content-section">
            <form className="panel data-form" onSubmit={handleCreate}>
                <div className="form-heading"><h2>Nueva cuenta</h2><p>Las contraseñas se guardan con hash y nunca se muestran en la lista.</p></div>
                <div className="form-grid user-form-grid">
                    <label>Nombre completo<input name="name" maxLength="150" minLength="2" autoComplete="name" required /></label>
                    <label>Correo electrónico<input name="email" type="email" maxLength="150" autoComplete="email" required /></label>
                    <label>Teléfono<input name="phone" maxLength="20" autoComplete="tel" /></label>
                    <label>Rol<select name="role" defaultValue="Warehouse Manager">{roles.map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select></label>
                    <label>Contraseña inicial<input name="password" type="password" minLength="12" maxLength="128" autoComplete="new-password" required /><small>Mínimo 12 caracteres.</small></label>
                </div>
                {actionError && <ErrorState message={actionError} />}
                <div className="form-actions"><button className="primary-button" type="submit" disabled={saving}>{saving ? 'Guardando…' : 'Crear cuenta'}</button></div>
            </form>
            <SuccessMessage>{message}</SuccessMessage>
            {error && <ErrorState message={error} onRetry={() => refresh().catch(() => {})} />}
            {loading && users.length === 0 ? <LoadingState /> : <section className="panel table-panel">
                <div className="table-wrapper"><table><thead><tr><th>Nombre</th><th>Correo</th><th>Teléfono</th><th>Rol</th><th>Registro</th></tr></thead>
                    <tbody>{users.map((user) => <tr key={user.id}>
                        <td>{user.name}</td><td>{user.email}</td><td>{user.phone || '—'}</td>
                        <td>{roleLabels[user.role] || user.role}</td>
                        <td>{user.registeredAt ? new Intl.DateTimeFormat('es-MX', { dateStyle: 'short' }).format(new Date(user.registeredAt)) : '—'}</td>
                    </tr>)}</tbody></table></div>
                {users.length === 0 && <EmptyState title="Todavía no hay usuarios" description="El administrador inicial se crea con el comando seguro indicado en la guía del proyecto." />}
            </section>}
        </section>
    </>;
}

export default UsersPage;
