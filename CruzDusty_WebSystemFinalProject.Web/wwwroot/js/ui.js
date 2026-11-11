'use strict';

const UI = (() => {
    const THEME_KEY = 'ac-theme';
    const SIDEBAR_KEY = 'ac-sidebar';
    const SPLASH_KEY = 'ac-splash-shown';
    const FLASH_KEY = 'ac-flash';
    const LOGO_URL = '/images/logo.png';

    const TOAST_TYPES = {
        success: { icon: 'bi-check-circle-fill', title: 'Success' },
        error: { icon: 'bi-exclamation-octagon-fill', title: 'Something went wrong' },
        info: { icon: 'bi-info-circle-fill', title: 'Notice' }
    };

    const REMARK_STYLES = {
        Passed: { css: 'remark-passed', icon: 'bi-check-circle-fill' },
        Failed: { css: 'remark-failed', icon: 'bi-x-circle-fill' },
        Incomplete: { css: 'remark-incomplete', icon: 'bi-hourglass-split' }
    };

    // ------------------------------------------------------------------
    // Small helpers
    // ------------------------------------------------------------------
    function escapeHtml(value) {
        if (value === null || value === undefined) {
            return '';
        }
        return String(value)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;');
    }

    function debounce(fn, wait) {
        let timer = null;
        return function debounced(...args) {
            clearTimeout(timer);
            timer = setTimeout(() => fn.apply(this, args), wait);
        };
    }

    function prefersReducedMotion() {
        return window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    }

    // Raw value of a CSS custom property, e.g. cssVar('--ac-red').
    function cssVar(name) {
        return getComputedStyle(document.documentElement).getPropertyValue(name).trim();
    }

    // Fully resolved colour of a CSS custom property (works for color-mix tokens too).
    function resolveColor(name) {
        const probe = document.createElement('span');
        probe.style.display = 'none';
        probe.style.color = `var(${name})`;
        document.body.appendChild(probe);
        const value = getComputedStyle(probe).color;
        probe.remove();
        return value;
    }

    function formatScore(value) {
        if (value === null || value === undefined || value === '') {
            return '--';
        }
        const number = Number(value);
        return Number.isFinite(number) ? number.toFixed(2) : '--';
    }

    function formatDateTime(value) {
        if (!value) {
            return '--';
        }
        const date = new Date(value);
        if (Number.isNaN(date.getTime())) {
            return '--';
        }
        return date.toLocaleString(undefined, { dateStyle: 'medium', timeStyle: 'short' });
    }

    function initials(name) {
        const parts = String(name || '').replace(/[^A-Za-z\s]/g, ' ').trim().split(/\s+/).filter(Boolean);
        if (parts.length === 0) {
            return '?';
        }
        const first = parts[0][0];
        const last = parts.length > 1 ? parts[parts.length - 1][0] : '';
        return (first + last).toUpperCase();
    }

    function remarksBadge(remarks, pop) {
        const style = REMARK_STYLES[remarks] || REMARK_STYLES.Incomplete;
        const popClass = pop ? ' badge-pop' : '';
        return `<span class="remark-badge ${style.css}${popClass}">` +
            `<i class="bi ${style.icon}" aria-hidden="true"></i>${escapeHtml(remarks)}</span>`;
    }

    function emptyStateHtml(options) {
        const opts = options || {};
        let action = '';
        if (opts.actionLabel) {
            action = `<button type="button" class="btn btn-primary btn-sm" data-empty-action>` +
                `<i class="bi bi-plus-lg me-1" aria-hidden="true"></i>${escapeHtml(opts.actionLabel)}</button>`;
        } else if (opts.retry) {
            action = `<button type="button" class="btn btn-outline-primary btn-sm" data-retry>` +
                `<i class="bi bi-arrow-clockwise me-1" aria-hidden="true"></i>Try again</button>`;
        }
        return `<div class="empty-state">` +
            `<img src="${LOGO_URL}" alt="" class="empty-logo" width="72" height="72" />` +
            `<p class="empty-title">${escapeHtml(opts.title || 'Nothing to show')}</p>` +
            `<p class="empty-text">${escapeHtml(opts.text || '')}</p>${action}</div>`;
    }

    // rows: [[label, htmlValue], ...]  (values must already be escaped)
    function detailListHtml(rows) {
        return rows.map(row => `<dt>${escapeHtml(row[0])}</dt><dd>${row[1]}</dd>`).join('');
    }

    // ------------------------------------------------------------------
    // Toasts
    // ------------------------------------------------------------------
    function toast(message, type, options) {
        const region = document.getElementById('toast-region');
        if (!region) {
            return;
        }

        const kind = TOAST_TYPES[type] ? type : 'info';
        const meta = TOAST_TYPES[kind];
        const opts = options || {};

        const element = document.createElement('div');
        element.className = `ac-toast ac-toast-${kind}`;
        element.setAttribute('role', kind === 'error' ? 'alert' : 'status');
        element.innerHTML =
            `<i class="bi ${meta.icon} ac-toast-icon" aria-hidden="true"></i>` +
            `<div class="ac-toast-body"><span class="ac-toast-title">${escapeHtml(opts.title || meta.title)}</span>` +
            `<span>${escapeHtml(message)}</span></div>` +
            `<button type="button" class="ac-toast-close" aria-label="Dismiss notification">` +
            `<i class="bi bi-x-lg" aria-hidden="true"></i></button>`;

        let leaving = false;
        const remove = () => {
            if (leaving) {
                return;
            }
            leaving = true;
            element.classList.add('is-leaving');
            setTimeout(() => element.remove(), 260);
        };

        element.querySelector('.ac-toast-close').addEventListener('click', remove);
        region.appendChild(element);

        while (region.children.length > 4) {
            region.firstElementChild.remove();
        }

        const duration = opts.duration !== undefined ? opts.duration : (kind === 'error' ? 6000 : 4000);
        if (duration > 0) {
            setTimeout(remove, duration);
        }
    }

    // Shows an API error as a toast unless api.js already handled it (401 / 403).
    function toastError(error, fallback) {
        if (error && error.handled) {
            return;
        }
        const message = (error && error.message) || fallback || 'Something went wrong. Please try again.';
        toast(message, 'error');
    }

    // A message that survives one page navigation (login, logout, session expiry).
    function flash(message, type) {
        try {
            sessionStorage.setItem(FLASH_KEY, JSON.stringify({ message: message, type: type || 'info' }));
        } catch (e) {
            // Storage unavailable: the message is simply not shown.
        }
    }

    function showFlash() {
        try {
            const raw = sessionStorage.getItem(FLASH_KEY);
            if (!raw) {
                return;
            }
            sessionStorage.removeItem(FLASH_KEY);
            const data = JSON.parse(raw);
            if (data && data.message) {
                toast(data.message, data.type || 'info');
            }
        } catch (e) {
            // Ignore malformed flash data.
        }
    }

    // ------------------------------------------------------------------
    // Confirm modal (returns a Promise<boolean>)
    // ------------------------------------------------------------------
    function confirm(options) {
        const opts = options || {};
        const modalElement = document.getElementById('confirm-modal');

        if (!modalElement || !window.bootstrap) {
            return Promise.resolve(window.confirm(opts.message || 'Are you sure?'));
        }

        const okButton = document.getElementById('confirm-modal-ok');
        const cancelButton = document.getElementById('confirm-modal-cancel');
        const icon = document.getElementById('confirm-modal-icon');
        const danger = opts.danger !== false;

        document.getElementById('confirm-modal-title').textContent = opts.title || 'Are you sure?';
        document.getElementById('confirm-modal-message').textContent = opts.message || '';
        okButton.textContent = opts.confirmText || 'Delete';
        okButton.className = danger ? 'btn btn-danger' : 'btn btn-primary';
        cancelButton.textContent = opts.cancelText || 'Cancel';
        icon.classList.toggle('is-info', !danger);
        icon.innerHTML = danger
            ? '<i class="bi bi-exclamation-triangle-fill"></i>'
            : '<i class="bi bi-question-circle-fill"></i>';

        const modal = window.bootstrap.Modal.getOrCreateInstance(modalElement);

        return new Promise(resolve => {
            let confirmed = false;

            const onOk = () => {
                confirmed = true;
                modal.hide();
            };
            const onShown = () => cancelButton.focus();
            const onHidden = () => {
                okButton.removeEventListener('click', onOk);
                modalElement.removeEventListener('shown.bs.modal', onShown);
                modalElement.removeEventListener('hidden.bs.modal', onHidden);
                resolve(confirmed);
            };

            okButton.addEventListener('click', onOk);
            modalElement.addEventListener('shown.bs.modal', onShown);
            modalElement.addEventListener('hidden.bs.modal', onHidden);
            modal.show();
        });
    }

    // ------------------------------------------------------------------
    // Buttons and forms
    // ------------------------------------------------------------------
    function setButtonLoading(button, loading, label) {
        if (!button) {
            return;
        }
        if (loading) {
            if (button.dataset.originalHtml === undefined) {
                button.dataset.originalHtml = button.innerHTML;
            }
            button.disabled = true;
            button.setAttribute('aria-busy', 'true');
            button.innerHTML =
                `<span class="spinner-border spinner-border-sm me-2" aria-hidden="true"></span>` +
                escapeHtml(label || 'Please wait');
        } else {
            if (button.dataset.originalHtml !== undefined) {
                button.innerHTML = button.dataset.originalHtml;
                delete button.dataset.originalHtml;
            }
            button.disabled = false;
            button.removeAttribute('aria-busy');
        }
    }

    function shake(element) {
        if (!element) {
            return;
        }
        element.classList.remove('shake');
        void element.offsetWidth;
        element.classList.add('shake');
        element.addEventListener('animationend', () => element.classList.remove('shake'), { once: true });
    }

    function setFieldError(form, name, message) {
        const field = form.querySelector(`[name="${name}"]`);
        if (!field) {
            return false;
        }
        const feedback = form.querySelector(`[data-error-for="${name}"]`);
        field.classList.add('is-invalid');
        field.setAttribute('aria-invalid', 'true');
        if (feedback) {
            feedback.textContent = message;
        }
        shake(field);
        return true;
    }

    function clearFieldError(field) {
        if (!field || !field.classList.contains('is-invalid')) {
            return;
        }
        field.classList.remove('is-invalid');
        field.removeAttribute('aria-invalid');
        const form = field.form;
        if (form && field.name) {
            const feedback = form.querySelector(`[data-error-for="${field.name}"]`);
            if (feedback) {
                feedback.textContent = '';
            }
        }
    }

    function clearFieldErrors(form) {
        form.querySelectorAll('.is-invalid').forEach(field => {
            field.classList.remove('is-invalid');
            field.removeAttribute('aria-invalid');
        });
        form.querySelectorAll('[data-error-for]').forEach(feedback => {
            feedback.textContent = '';
        });
    }

    // errors: { fieldName: 'message' } or { fieldName: ['message', ...] }
    // aliases: { serverName: 'formFieldName' }
    function showFieldErrors(form, errors, aliases) {
        if (!errors) {
            return 0;
        }
        const map = aliases || {};
        let shown = 0;

        Object.keys(errors).forEach(key => {
            const value = errors[key];
            const message = Array.isArray(value) ? value[0] : String(value);
            if (setFieldError(form, map[key] || key, message)) {
                shown += 1;
            }
        });

        const firstInvalid = form.querySelector('.is-invalid:not(:disabled)');
        if (firstInvalid) {
            firstInvalid.focus();
        }
        return shown;
    }

    // Removes the red state from a field as soon as the user edits it.
    function bindLiveValidation(form) {
        const handler = event => clearFieldError(event.target);
        form.addEventListener('input', handler);
        form.addEventListener('change', handler);
    }

    function showFormAlert(element, message) {
        if (!element) {
            return;
        }
        element.textContent = message;
        element.hidden = false;
    }

    function hideFormAlert(element) {
        if (!element) {
            return;
        }
        element.textContent = '';
        element.hidden = true;
    }

    // Standard reaction to a failed save: inline field errors + form alert + toast.
    function handleFormError(form, alertElement, error, aliases) {
        const shown = error && error.errors ? showFieldErrors(form, error.errors, aliases) : 0;
        showFormAlert(alertElement, (error && error.message) || 'Something went wrong. Please try again.');
        toastError(error);
        return shown;
    }

    // ------------------------------------------------------------------
    // Skeletons, count-up, tooltips
    // ------------------------------------------------------------------
    function skeletonRows(tbody, columns, rows) {
        const count = rows || 6;
        let html = '';
        for (let r = 0; r < count; r += 1) {
            html += '<tr class="empty-row">';
            for (let c = 0; c < columns; c += 1) {
                html += '<td><span class="skeleton skeleton-line"></span></td>';
            }
            html += '</tr>';
        }
        tbody.setAttribute('aria-busy', 'true');
        tbody.innerHTML = html;
    }

    function countUp(element, target, options) {
        if (!element) {
            return;
        }
        const opts = options || {};
        const decimals = opts.decimals || 0;
        const suffix = opts.suffix || '';
        const duration = opts.duration || 900;
        const end = Number(target) || 0;
        const format = value => value.toFixed(decimals) + suffix;

        if (prefersReducedMotion() || end === 0) {
            element.textContent = format(end);
            return;
        }

        const start = performance.now();
        const frame = now => {
            const progress = Math.min(1, (now - start) / duration);
            const eased = 1 - Math.pow(1 - progress, 3);
            element.textContent = format(end * eased);
            if (progress < 1) {
                requestAnimationFrame(frame);
            } else {
                element.textContent = format(end);
            }
        };
        requestAnimationFrame(frame);
    }

    function initTooltips(root) {
        if (!window.bootstrap || !root) {
            return;
        }
        root.querySelectorAll('[data-bs-toggle="tooltip"]').forEach(element => {
            window.bootstrap.Tooltip.getOrCreateInstance(element, { trigger: 'hover focus' });
        });
    }

    function disposeTooltips(root) {
        if (!window.bootstrap || !root) {
            return;
        }
        root.querySelectorAll('[data-bs-toggle="tooltip"]').forEach(element => {
            const instance = window.bootstrap.Tooltip.getInstance(element);
            if (instance) {
                instance.dispose();
            }
        });
    }

    // ------------------------------------------------------------------
    // Data table: sorting, pagination, skeleton, empty and error states
    // options: { table, pager, info, pageSize, defaultSort, defaultDir,
    //            renderRow(row, index), emptyHtml(), onEmptyAction(), onRetry() }
    // ------------------------------------------------------------------
    function createDataTable(options) {
        const table = options.table;
        const tbody = table.querySelector('tbody');
        const headers = Array.from(table.querySelectorAll('thead th'));
        const columnCount = headers.length;
        const pager = options.pager;
        const info = options.info;
        const pageSize = options.pageSize || 10;

        let rows = [];
        let page = 1;
        let sortKey = options.defaultSort || null;
        let sortDir = options.defaultDir || 'asc';

        function isBlank(value) {
            return value === null || value === undefined || value === '';
        }

        function compare(a, b) {
            const left = a[sortKey];
            const right = b[sortKey];
            const leftBlank = isBlank(left);
            const rightBlank = isBlank(right);

            // Blank values always go last, whatever the direction.
            if (leftBlank && rightBlank) { return 0; }
            if (leftBlank) { return 1; }
            if (rightBlank) { return -1; }

            const result = (typeof left === 'number' && typeof right === 'number')
                ? left - right
                : String(left).localeCompare(String(right), undefined, { numeric: true, sensitivity: 'base' });

            return sortDir === 'asc' ? result : -result;
        }

        function sortedRows() {
            return sortKey ? rows.slice().sort(compare) : rows.slice();
        }

        function updateHeaders() {
            headers.forEach(th => {
                const key = th.dataset.sort;
                if (!key) {
                    return;
                }
                const icon = th.querySelector('.sort-icon');
                if (key === sortKey) {
                    th.setAttribute('aria-sort', sortDir === 'asc' ? 'ascending' : 'descending');
                    if (icon) {
                        icon.className = `bi ${sortDir === 'asc' ? 'bi-sort-up' : 'bi-sort-down'} sort-icon`;
                    }
                } else {
                    th.setAttribute('aria-sort', 'none');
                    if (icon) {
                        icon.className = 'bi bi-arrow-down-up sort-icon';
                    }
                }
            });
        }

        function clearFooter() {
            if (pager) { pager.innerHTML = ''; }
            if (info) { info.textContent = ''; }
        }

        function renderPager(totalPages) {
            if (!pager) {
                return;
            }
            if (totalPages <= 1) {
                pager.innerHTML = '';
                return;
            }

            const items = [];
            const addButton = (label, target, state) => {
                const s = state || {};
                items.push(
                    `<li class="page-item${s.disabled ? ' disabled' : ''}${s.active ? ' active' : ''}">` +
                    `<button type="button" class="page-link" data-page="${target}"` +
                    `${s.disabled ? ' disabled' : ''}${s.active ? ' aria-current="page"' : ''}` +
                    ` aria-label="${s.aria || 'Page ' + label}">${label}</button></li>`);
            };

            addButton('&lsaquo;', page - 1, { disabled: page === 1, aria: 'Previous page' });

            let last = 0;
            for (let p = 1; p <= totalPages; p += 1) {
                if (p === 1 || p === totalPages || Math.abs(p - page) <= 1) {
                    if (last && p - last > 1) {
                        items.push('<li class="page-item disabled"><span class="page-link">&hellip;</span></li>');
                    }
                    addButton(String(p), p, { active: p === page });
                    last = p;
                }
            }

            addButton('&rsaquo;', page + 1, { disabled: page === totalPages, aria: 'Next page' });
            pager.innerHTML = items.join('');
        }

        function render() {
            disposeTooltips(tbody);
            tbody.removeAttribute('aria-busy');

            const total = rows.length;
            if (total === 0) {
                const html = options.emptyHtml ? options.emptyHtml() : emptyStateHtml({});
                tbody.innerHTML = `<tr class="empty-row"><td colspan="${columnCount}">${html}</td></tr>`;
                clearFooter();
                return;
            }

            const totalPages = Math.ceil(total / pageSize);
            page = Math.min(Math.max(page, 1), totalPages);

            const startIndex = (page - 1) * pageSize;
            const pageRows = sortedRows().slice(startIndex, startIndex + pageSize);

            tbody.innerHTML = pageRows.map((row, index) => options.renderRow(row, index)).join('');

            Array.from(tbody.rows).forEach((tr, index) => {
                tr.classList.add('row-enter');
                tr.style.setProperty('--i', index);
            });

            if (info) {
                info.textContent = `Showing ${startIndex + 1} to ${startIndex + pageRows.length} of ${total}`;
            }
            renderPager(totalPages);
            initTooltips(tbody);
        }

        headers.forEach(th => {
            const key = th.dataset.sort;
            const button = th.querySelector('.th-sort');
            if (!key || !button) {
                return;
            }
            button.addEventListener('click', () => {
                if (sortKey === key) {
                    sortDir = sortDir === 'asc' ? 'desc' : 'asc';
                } else {
                    sortKey = key;
                    sortDir = 'asc';
                }
                page = 1;
                updateHeaders();
                render();
            });
        });

        if (pager) {
            pager.addEventListener('click', event => {
                const button = event.target.closest('[data-page]');
                if (!button || button.disabled) {
                    return;
                }
                page = Number(button.dataset.page);
                render();
            });
        }

        tbody.addEventListener('click', event => {
            if (event.target.closest('[data-empty-action]') && options.onEmptyAction) {
                options.onEmptyAction();
            } else if (event.target.closest('[data-retry]') && options.onRetry) {
                options.onRetry();
            }
        });

        updateHeaders();

        return {
            setRows(newRows, keepPage) {
                rows = newRows || [];
                if (!keepPage) {
                    page = 1;
                }
                render();
            },
            // Jumps to the page that contains the first row matching the predicate.
            showRow(predicate) {
                const index = sortedRows().findIndex(predicate);
                if (index >= 0) {
                    page = Math.floor(index / pageSize) + 1;
                    render();
                }
            },
            showSkeleton(count) {
                disposeTooltips(tbody);
                skeletonRows(tbody, columnCount, count || 6);
                clearFooter();
            },
            showError(message) {
                disposeTooltips(tbody);
                tbody.removeAttribute('aria-busy');
                const html = emptyStateHtml({ title: 'Could not load the data', text: message, retry: true });
                tbody.innerHTML = `<tr class="empty-row"><td colspan="${columnCount}">${html}</td></tr>`;
                clearFooter();
            },
            getRows() {
                return rows;
            },
            refresh: render
        };
    }

    // ------------------------------------------------------------------
    // Theme, splash, sidebar
    // ------------------------------------------------------------------
    function currentTheme() {
        return document.documentElement.getAttribute('data-theme') === 'dark' ? 'dark' : 'light';
    }

    function syncThemeButtons(theme) {
        const dark = theme === 'dark';
        document.querySelectorAll('[data-theme-toggle]').forEach(button => {
            button.setAttribute('aria-label', dark ? 'Switch to light mode' : 'Switch to dark mode');
            button.setAttribute('aria-pressed', String(dark));
            const icon = button.querySelector('i');
            if (icon) {
                icon.className = dark ? 'bi bi-sun-fill' : 'bi bi-moon-stars-fill';
            }
        });
    }

    function applyTheme(theme) {
        const root = document.documentElement;
        root.setAttribute('data-theme', theme);
        root.setAttribute('data-bs-theme', theme);
        try {
            localStorage.setItem(THEME_KEY, theme);
        } catch (e) {
            // Storage unavailable: the theme still applies for this page.
        }
        syncThemeButtons(theme);
        document.dispatchEvent(new CustomEvent('ac:themechange', { detail: { theme: theme } }));
    }

    function initTheme() {
        syncThemeButtons(currentTheme());
        document.querySelectorAll('[data-theme-toggle]').forEach(button => {
            button.addEventListener('click', () => {
                applyTheme(currentTheme() === 'dark' ? 'light' : 'dark');
            });
        });
    }

    function initSplash() {
        const splash = document.getElementById('splash');
        try {
            sessionStorage.setItem(SPLASH_KEY, '1');
        } catch (e) {
            // Ignore: the splash will simply show again on the next page.
        }
        if (splash) {
            setTimeout(() => splash.classList.add('is-hidden'), 2100);
        }
    }

    function initSidebar() {
        const shell = document.getElementById('app-shell');
        const toggle = document.getElementById('sidebar-toggle');
        const backdrop = document.getElementById('sidebar-backdrop');
        if (!shell || !toggle) {
            return;
        }

        const isMobile = () => window.matchMedia('(max-width: 991.98px)').matches;

        try {
            if (localStorage.getItem(SIDEBAR_KEY) === 'collapsed') {
                shell.classList.add('sidebar-collapsed');
            }
        } catch (e) {
            // Ignore.
        }

        const sync = () => {
            const expanded = isMobile()
                ? shell.classList.contains('sidebar-open')
                : !shell.classList.contains('sidebar-collapsed');
            toggle.setAttribute('aria-expanded', String(expanded));
        };

        const closeMobile = () => {
            shell.classList.remove('sidebar-open');
            sync();
        };

        toggle.addEventListener('click', () => {
            if (isMobile()) {
                shell.classList.toggle('sidebar-open');
            } else {
                const collapsed = shell.classList.toggle('sidebar-collapsed');
                try {
                    localStorage.setItem(SIDEBAR_KEY, collapsed ? 'collapsed' : 'expanded');
                } catch (e) {
                    // Ignore.
                }
            }
            sync();
        });

        if (backdrop) {
            backdrop.addEventListener('click', closeMobile);
        }

        document.addEventListener('keydown', event => {
            if (event.key === 'Escape' && shell.classList.contains('sidebar-open')) {
                closeMobile();
                toggle.focus();
            }
        });

        window.addEventListener('resize', debounce(() => {
            if (!isMobile()) {
                shell.classList.remove('sidebar-open');
            }
            sync();
        }, 150));

        sync();
    }

    function init() {
        initTheme();
        initSplash();
        initSidebar();
        showFlash();
    }

    // This script is loaded at the end of <body>, so the elements above already exist.
    init();

    return {
        escapeHtml: escapeHtml,
        debounce: debounce,
        prefersReducedMotion: prefersReducedMotion,
        cssVar: cssVar,
        resolveColor: resolveColor,
        formatScore: formatScore,
        formatDateTime: formatDateTime,
        initials: initials,
        remarksBadge: remarksBadge,
        emptyStateHtml: emptyStateHtml,
        detailListHtml: detailListHtml,
        toast: toast,
        toastError: toastError,
        flash: flash,
        confirm: confirm,
        setButtonLoading: setButtonLoading,
        shake: shake,
        setFieldError: setFieldError,
        clearFieldErrors: clearFieldErrors,
        showFieldErrors: showFieldErrors,
        bindLiveValidation: bindLiveValidation,
        showFormAlert: showFormAlert,
        hideFormAlert: hideFormAlert,
        handleFormError: handleFormError,
        skeletonRows: skeletonRows,
        countUp: countUp,
        initTooltips: initTooltips,
        disposeTooltips: disposeTooltips,
        createDataTable: createDataTable
    };
})();