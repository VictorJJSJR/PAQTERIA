import { useCallback, useEffect, useMemo, useState } from 'react';
import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { checkDatabaseHealth } from '../api/operationsClient';
import { RealtimeContext } from './realtimeContext';

export function RealtimeProvider({ children }) {
    const [revision, setRevision] = useState(0);
    const [connectionState, setConnectionState] = useState('connecting');
    const [databaseAvailable, setDatabaseAvailable] = useState(false);

    const refreshDatabaseState = useCallback(async () => {
        try {
            setDatabaseAvailable(await checkDatabaseHealth());
        } catch {
            setDatabaseAvailable(false);
        }
    }, []);

    useEffect(() => {
        let active = true;
        let retryTimer;
        let retryDelay = 1000;
        let retrying = false;
        const connection = new HubConnectionBuilder()
            .withUrl('/hubs/operations')
            .withAutomaticReconnect([0, 2000, 10000, 30000])
            .configureLogging(import.meta.env.DEV ? LogLevel.Warning : LogLevel.Error)
            .build();

        connection.on('dataChanged', () => {
            if (active) setRevision((current) => current + 1);
        });
        connection.onreconnecting(() => {
            if (active) setConnectionState('reconnecting');
        });
        connection.onreconnected(() => {
            if (active) {
                retryDelay = 1000;
                setConnectionState('connected');
                setRevision((current) => current + 1);
                void refreshDatabaseState();
            }
        });
        connection.onclose(() => {
            if (active) {
                setConnectionState('reconnecting');
                scheduleRetry();
            }
        });

        function scheduleRetry() {
            if (!active || retrying) return;
            retrying = true;
            retryTimer = window.setTimeout(async () => {
                retrying = false;
                if (!active) return;
                try {
                    await connection.start();
                    retryDelay = 1000;
                    setConnectionState('connected');
                    setRevision((current) => current + 1);
                    void refreshDatabaseState();
                } catch {
                    scheduleRetry();
                    retryDelay = Math.min(retryDelay * 2, 30000);
                }
            }, retryDelay);
        }

        queueMicrotask(() => {
            if (active) void refreshDatabaseState();
        });
        const healthTimer = window.setInterval(() => {
            void refreshDatabaseState();
            // Reconsulta periódicamente por si una notificación se perdió durante una desconexión.
            setRevision((current) => current + 1);
        }, 15000);
        connection.start()
            .then(() => {
                if (active) setConnectionState('connected');
            })
            .catch(() => {
                if (active) {
                    setConnectionState('reconnecting');
                    scheduleRetry();
                }
            });

        return () => {
            active = false;
            window.clearInterval(healthTimer);
            window.clearTimeout(retryTimer);
            connection.off('dataChanged');
            void connection.stop();
        };
    }, [refreshDatabaseState]);

    const value = useMemo(() => ({
        revision,
        connectionState: databaseAvailable
            ? connectionState
            : (connectionState === 'connected' ? 'database-unavailable' : connectionState),
    }), [revision, connectionState, databaseAvailable]);

    return <RealtimeContext.Provider value={value}>{children}</RealtimeContext.Provider>;
}
