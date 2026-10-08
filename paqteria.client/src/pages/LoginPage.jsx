import { useEffect, useState } from 'react';
import { getJson } from '../api/operationsClient';
import { useAuth } from '../auth/useAuth';

function LoginPage() {
    const { login } = useAuth();
    const [email, setEmail] = useState('');
    const [password, setPassword] = useState('');
    const [submitting, setSubmitting] = useState(false);
    const [error, setError] = useState('');
    const [databaseState, setDatabaseState] = useState('checking');

    useEffect(() => {
        let active = true;
        getJson('/api/health')
            .then((health) => { if (active) setDatabaseState(health.schema === 'ready' ? 'ready' : 'setup-required'); })
            .catch(() => { if (active) setDatabaseState('unavailable'); });
        return () => { active = false; };
    }, []);

    async function handleSubmit(event) {
        event.preventDefault();
        setSubmitting(true);
        setError('');
        try {
            await login(email.trim(), password);
        } catch (cause) {
            setError(cause.message || 'No se pudo iniciar sesión.');
        } finally {
            setSubmitting(false);
        }
    }

    const databaseMessages = {
        checking: 'Comprobando conexión…',
        ready: 'Base de datos conectada',
        'setup-required': 'Falta preparar el esquema de la aplicación',
        unavailable: 'No se pudo conectar con la base de datos',
    };

    return <main className="login-screen">
        <section className="login-card" aria-labelledby="login-title">
            <div className="login-brand"><span className="brand-name">RumboEnvíos</span><span className="brand-caption">CONTROL TOWER</span></div>
            <h1 id="login-title">Inicia sesión</h1>
            <p className="login-subtitle">Acceso para Administración y Encargados de almacén.</p>
            <div className={`login-database-state ${databaseState}`} role="status"><i />{databaseMessages[databaseState]}</div>
            <form className="login-form" onSubmit={handleSubmit}>
                <label>Correo electrónico<input type="email" name="email" autoComplete="username" maxLength="150" value={email} onChange={(event) => setEmail(event.target.value)} required /></label>
                <label>Contraseña<input type="password" name="password" autoComplete="current-password" value={password} onChange={(event) => setPassword(event.target.value)} required /></label>
                {error && <p className="login-error" role="alert">{error}</p>}
                <button className="primary-button login-button" type="submit" disabled={submitting || databaseState !== 'ready'}>
                    {submitting ? 'Validando…' : 'Entrar'}
                </button>
            </form>
            <small className="login-footnote">Si todavía no tienes una cuenta, solicita al administrador que la cree.</small>
        </section>
    </main>;
}

export default LoginPage;
