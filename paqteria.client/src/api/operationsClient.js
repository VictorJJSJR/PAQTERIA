async function requestJson(url, options = {}) {
    const response = await fetch(url, {
        ...options,
        credentials: 'same-origin',
        headers: {
            ...(options.body ? { 'Content-Type': 'application/json' } : {}),
            ...options.headers,
        },
        cache: 'no-store',
    });

    if (response.status === 204) return null;
    const contentType = response.headers.get('content-type') || '';
    const body = contentType.includes('application/json') ? await response.json() : await response.text();

    if (!response.ok) {
        const detail = typeof body === 'string'
            ? body
            : body?.detail || body?.title || (Array.isArray(body?.errors)
                ? body.errors.map((item) => `Fila ${item.row}: ${item.message}`).join(' ')
                : Object.values(body?.errors || {}).flat().join(' '));
        const error = new Error(detail || `La solicitud falló (${response.status}).`);
        error.importErrors = Array.isArray(body?.errors) ? body.errors : [];
        error.truncated = body?.truncated === true;
        if (response.status === 401 && !url.startsWith('/api/auth/'))
            window.dispatchEvent(new Event('paqteria:session-expired'));
        if (response.status === 403 && !url.startsWith('/api/auth/'))
            window.dispatchEvent(new Event('paqteria:permissions-changed'));
        throw error;
    }

    return body;
}

export function getJson(url, options) {
    return requestJson(url, options);
}

export function postJson(url, body) {
    return requestJson(url, { method: 'POST', body: JSON.stringify(body) });
}

export function putJson(url, body) {
    return requestJson(url, { method: 'PUT', body: JSON.stringify(body) });
}

export async function postForm(url, formData) {
    const response = await fetch(url, { method: 'POST', body: formData, cache: 'no-store', credentials: 'same-origin' });
    if (response.status === 204) return null;
    const contentType = response.headers.get('content-type') || '';
    const body = contentType.includes('application/json') ? await response.json() : await response.text();
    if (!response.ok) {
        const details = Array.isArray(body?.errors)
            ? body.errors.map((item) => `Fila ${item.row}: ${item.message}`).join(' ')
            : '';
        const error = new Error(details || body?.detail || body?.title || `La carga falló (${response.status}).`);
        error.importErrors = Array.isArray(body?.errors) ? body.errors : [];
        error.truncated = body?.truncated === true;
        if (response.status === 401)
            window.dispatchEvent(new Event('paqteria:session-expired'));
        if (response.status === 403 && !url.startsWith('/api/auth/'))
            window.dispatchEvent(new Event('paqteria:permissions-changed'));
        throw error;
    }
    return body;
}

export async function checkDatabaseHealth() {
    const response = await fetch('/api/health', { cache: 'no-store' });
    if (!response.ok) return false;
    const result = await response.json();
    return result.database === 'connected';
}

export function formatDateTime(value) {
    if (!value) return '—';
    return new Intl.DateTimeFormat('es-MX', {
        dateStyle: 'short',
        timeStyle: 'short',
    }).format(new Date(value));
}
