import { useCallback, useEffect, useMemo, useState } from 'react';
import { getJson, postJson } from '../api/operationsClient';
import { AuthContext } from './authContext';

export function AuthProvider({ children }) {
    const [user, setUser] = useState(null);
    const [loading, setLoading] = useState(true);

    const reloadSession = useCallback(async () => {
        try {
            const current = await getJson('/api/auth/me');
            setUser(current);
            return current;
        } catch {
            setUser(null);
            return null;
        } finally {
            setLoading(false);
        }
    }, []);

    useEffect(() => {
        let active = true;
        getJson('/api/auth/me')
            .then((current) => { if (active) setUser(current); })
            .catch(() => { if (active) setUser(null); })
            .finally(() => { if (active) setLoading(false); });
        const handleExpiredSession = () => setUser(null);
        const handlePermissionsChanged = () => { void reloadSession(); };
        window.addEventListener('paqteria:session-expired', handleExpiredSession);
        window.addEventListener('paqteria:permissions-changed', handlePermissionsChanged);
        return () => {
            active = false;
            window.removeEventListener('paqteria:session-expired', handleExpiredSession);
            window.removeEventListener('paqteria:permissions-changed', handlePermissionsChanged);
        };
    }, [reloadSession]);

    const login = useCallback(async (email, password) => {
        const authenticatedUser = await postJson('/api/auth/login', { email, password });
        setUser(authenticatedUser);
        return authenticatedUser;
    }, []);

    const logout = useCallback(async () => {
        try { await postJson('/api/auth/logout', {}); }
        finally { setUser(null); }
    }, []);

    const value = useMemo(() => ({ user, loading, login, logout, reloadSession }),
        [user, loading, login, logout, reloadSession]);

    return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
