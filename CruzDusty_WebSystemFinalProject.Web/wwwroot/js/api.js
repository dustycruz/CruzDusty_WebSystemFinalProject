'use strict';

// Login state lives in sessionStorage: it disappears when the tab is closed.
const Session = (() => {
    const KEY = 'ac-session';

    function get() {
        try {
            const raw = sessionStorage.getItem(KEY);
            if (!raw) {
                return null;
            }
            const data = JSON.parse(raw);
            return data && data.token && data.role ? data : null;
        } catch (e) {
            return null;
        }
    }

    function save(data) {
        sessionStorage.setItem(KEY, JSON.stringify(data));
    }

    function clear() {
        try {
            sessionStorage.removeItem(KEY);
        } catch (e) {
            // Nothing to clear.
        }
    }

    function isExpired(session) {
        if (!session || !session.expiresAt) {
            return true;
        }
        const time = new Date(session.expiresAt).getTime();
        return Number.isNaN(time) || time <= Date.now();
    }

    return { get: get, save: save, clear: clear, isExpired: isExpired };
})();

const Api = (() => {
    const LOGIN_URL = '/Account/Login';

    // The base URL is rendered once by _Layout.cshtml from appsettings.json (ApiSettings:BaseUrl).
    const meta = document.querySelector('meta[name="api-base-url"]');
    const baseUrl = ((meta && meta.content) || '').replace(/\/+$/, '');

    const STATUS_MESSAGES = {
        0: 'Cannot reach the server. Check that the API is running, then try again.',
        400: 'Some of the information is not valid. Please check it and try again.',
        401: 'You need to sign in to continue.',
        403: 'Access denied. You do not have permission to do that.',
        404: 'The record could not be found. It may have been deleted.',
        409: 'This conflicts with an existing record.',
        500: 'The server ran into a problem. Please try again in a moment.'
    };

    class ApiError extends Error {
        constructor(message, status, errors) {
            super(message);
            this.name = 'ApiError';
            this.status = status;
            this.errors = errors || null;
            this.handled = false;
        }
    }

    function messageForStatus(status) {
        if (STATUS_MESSAGES[status]) {
            return STATUS_MESSAGES[status];
        }
        return status >= 500 ? STATUS_MESSAGES[500] : 'The request could not be completed. Please try again.';
    }

    let redirecting = false;

    // Called on any 401 from a signed-in request and by the expiry timer in auth.js.
    function handleUnauthorized() {
        if (redirecting) {
            return;
        }
        redirecting = true;
        Session.clear();
        UI.flash('Session expired. Please sign in again.', 'info');
        UI.toast('Session expired. Taking you to the sign-in page.', 'info', { title: 'Session expired' });
        setTimeout(() => window.location.assign(LOGIN_URL), 1500);
    }

    function buildQuery(params) {
        if (!params) {
            return '';
        }
        const search = new URLSearchParams();
        Object.keys(params).forEach(key => {
            const value = params[key];
            if (value !== null && value !== undefined && value !== '') {
                search.append(key, value);
            }
        });
        const text = search.toString();
        return text ? `?${text}` : '';
    }

    // options.anonymous = true: do not attach the token and do not treat 401/403 as a session problem.
    async function request(method, path, body, options) {
        const opts = options || {};
        const headers = { Accept: 'application/json' };

        const session = Session.get();
        if (session && !opts.anonymous) {
            headers.Authorization = `Bearer ${session.token}`;
        }

        const init = { method: method, headers: headers };
        if (body !== undefined) {
            headers['Content-Type'] = 'application/json';
            init.body = JSON.stringify(body);
        }

        let response;
        try {
            response = await fetch(baseUrl + path, init);
        } catch (networkError) {
            throw new ApiError(messageForStatus(0), 0);
        }

        if (response.status === 204) {
            return null;
        }

        let data = null;
        const text = await response.text();
        if (text) {
            try {
                data = JSON.parse(text);
            } catch (parseError) {
                data = null;
            }
        }

        if (response.ok) {
            return data;
        }

        const serverMessage = data && typeof data.message === 'string' ? data.message : '';
        const message = response.status >= 500 || !serverMessage
            ? messageForStatus(response.status)
            : serverMessage;

        const error = new ApiError(message, response.status, data && data.errors);

        if (!opts.anonymous) {
            if (response.status === 401) {
                error.handled = true;
                handleUnauthorized();
            } else if (response.status === 403) {
                error.handled = true;
                UI.toast(messageForStatus(403), 'error', { title: 'Access denied' });
            }
        }

        throw error;
    }

    return {
        baseUrl: baseUrl,
        ApiError: ApiError,
        messageForStatus: messageForStatus,
        handleUnauthorized: handleUnauthorized,
        get: (path, params, options) => request('GET', path + buildQuery(params), undefined, options),
        post: (path, body, options) => request('POST', path, body, options),
        put: (path, body, options) => request('PUT', path, body, options),
        del: (path, options) => request('DELETE', path, undefined, options)
    };
})();