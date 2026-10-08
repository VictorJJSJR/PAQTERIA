import { useContext } from 'react';
import { RealtimeContext } from '../realtime/realtimeContext';

export function useRealtime() {
    const value = useContext(RealtimeContext);
    if (value === null) throw new Error('useRealtime debe usarse dentro de RealtimeProvider.');
    return value;
}
