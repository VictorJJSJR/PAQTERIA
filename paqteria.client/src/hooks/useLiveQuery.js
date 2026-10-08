import { useCallback, useEffect, useState } from 'react';
import { getJson } from '../api/operationsClient';
import { useRealtime } from './useRealtime';

export function useLiveQuery(url, fallbackValue) {
    const { revision } = useRealtime();
    const [result, setResult] = useState({ data: null, loading: true, error: null });

    const refresh = useCallback(async () => {
        try {
            const data = await getJson(url);
            setResult({ data, loading: false, error: null });
            return data;
        } catch (error) {
            setResult((current) => ({
                data: current.data,
                loading: false,
                error: error.message || 'No se pudieron cargar los datos.',
            }));
            throw error;
        }
    }, [url]);

    useEffect(() => {
        let active = true;
        getJson(url)
            .then((data) => {
                if (active) setResult({ data, loading: false, error: null });
            })
            .catch((error) => {
                if (active) setResult((current) => ({
                    data: current.data,
                    loading: false,
                    error: error.message || 'No se pudieron cargar los datos.',
                }));
            });
        return () => { active = false; };
    }, [url, revision]);

    return { ...result, data: result.data ?? fallbackValue, refresh };
}
