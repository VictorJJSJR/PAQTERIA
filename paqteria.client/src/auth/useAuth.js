import { useContext } from 'react';
import { AuthContext } from './authContext';

export function useAuth() {
    const auth = useContext(AuthContext);
    if (!auth) throw new Error('useAuth requiere AuthProvider.');
    return auth;
}
