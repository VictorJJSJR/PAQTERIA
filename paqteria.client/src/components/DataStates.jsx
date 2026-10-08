export function LoadingState({ label = 'Cargando información…' }) {
    return <div className="data-state" role="status">{label}</div>;
}

export function ErrorState({ message, onRetry }) {
    return (
        <div className="data-error" role="alert">
            <span>{message}</span>
            {onRetry && <button className="text-button" onClick={onRetry} type="button">Reintentar</button>}
        </div>
    );
}

export function SuccessMessage({ children }) {
    return children ? <p className="form-success" role="status">{children}</p> : null;
}
