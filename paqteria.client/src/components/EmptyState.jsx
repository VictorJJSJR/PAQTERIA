function EmptyState({ title = 'Sin datos disponibles', description = 'La información aparecerá cuando esté disponible.' }) {
    return (
        <div className="empty-state">
            <div className="empty-icon" aria-hidden="true">—</div>
            <strong>{title}</strong>
            <p>{description}</p>
        </div>
    );
}

export default EmptyState;
