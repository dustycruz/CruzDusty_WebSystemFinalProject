'use strict';

const Auth = (() => {
    const LOGIN_URL = '/Account/Login';
    const DENIED_URL = '/Home/AccessDenied';
    const REMEMBER_KEY = 'ac-remember-username';
    const HOME_BY_ROLE = {
        Faculty: '/Faculty/Dashboard',
        Student: '/Student/MyGrades'
    };

    function homeFor(role) {
        return HOME_BY_ROLE[role] || LOGIN_URL;
    }

    // Returns the stored session, or null when there is none or it has expired.
    function currentSession() {
        const session = Session.get();
        if (!session) {
            return null;
        }
        if (Session.isExpired(session)) {
            Session.clear();
            return null;
        }
        return session;
    }

    function logout() {
        Session.clear();
        UI.flash('You have been signed out.', 'success');
        window.location.assign(LOGIN_URL);
    }

    // Used by every page script. Redirects and returns null when the role is wrong.
    function requireRole(role) {
        const hadSession = Session.get() !== null;
        const session = currentSession();

        if (!session) {
            if (hadSession) {
                UI.flash('Session expired. Please sign in again.', 'info');
            }
            window.location.replace(LOGIN_URL);
            return null;
        }

        if (session.role !== role) {
            window.location.replace(DENIED_URL);
            return null;
        }

        return session;
    }

    function fillUserInfo(session) {
        document.querySelectorAll('[data-user-name]').forEach(element => {
            element.textContent = session.fullName;
        });
        document.querySelectorAll('[data-user-role]').forEach(element => {
            element.textContent = session.role;
        });
        document.querySelectorAll('[data-user-initials]').forEach(element => {
            element.textContent = UI.initials(session.fullName);
        });
    }

    // Shows "Session expired" and redirects at the moment the JWT expires,
    // even if the user is idle and makes no request.
    function scheduleExpiry(session) {
        const remaining = new Date(session.expiresAt).getTime() - Date.now();
        if (remaining <= 0) {
            Api.handleUnauthorized();
            return;
        }
        setTimeout(() => Api.handleUnauthorized(), Math.min(remaining, 2147000000));
    }

    // Pages that use _AppShell.cshtml: check the role, then reveal the shell.
    function guardShell() {
        const shell = document.getElementById('app-shell');
        if (!shell) {
            return;
        }

        const session = requireRole(shell.dataset.requiredRole);
        if (!session) {
            return;
        }

        fillUserInfo(session);
        shell.classList.remove('shell-pending');
        scheduleExpiry(session);
    }

    // "/" : send the visitor where they belong.
    function redirectFromHome() {
        if (!document.querySelector('[data-page="home"]')) {
            return;
        }
        const session = currentSession();
        window.location.replace(session ? homeFor(session.role) : LOGIN_URL);
    }

    function bindCommonLinks() {
        document.querySelectorAll('[data-logout]').forEach(button => {
            button.addEventListener('click', logout);
        });

        const session = currentSession();
        document.querySelectorAll('[data-home-link]').forEach(link => {
            link.setAttribute('href', session ? homeFor(session.role) : LOGIN_URL);
        });
    }

    function initLoginPage() {
        const form = document.getElementById('login-form');
        if (!form) {
            return;
        }

        const existing = currentSession();
        if (existing) {
            window.location.replace(homeFor(existing.role));
            return;
        }

        const usernameInput = document.getElementById('login-username');
        const passwordInput = document.getElementById('login-password');
        const rememberBox = document.getElementById('remember-username');
        const submitButton = document.getElementById('login-submit');
        const toggleButton = document.getElementById('toggle-password');
        const alertBox = document.getElementById('login-alert');

        try {
            const remembered = localStorage.getItem(REMEMBER_KEY);
            if (remembered) {
                usernameInput.value = remembered;
                rememberBox.checked = true;
            }
        } catch (e) {
            // Storage unavailable: nothing to restore.
        }

        (usernameInput.value ? passwordInput : usernameInput).focus();
        UI.bindLiveValidation(form);

        toggleButton.addEventListener('click', () => {
            const show = passwordInput.type === 'password';
            passwordInput.type = show ? 'text' : 'password';
            toggleButton.setAttribute('aria-pressed', String(show));
            toggleButton.setAttribute('aria-label', show ? 'Hide password' : 'Show password');
            toggleButton.querySelector('i').className = show ? 'bi bi-eye-slash-fill' : 'bi bi-eye-fill';
        });

        form.addEventListener('submit', async event => {
            event.preventDefault();
            UI.clearFieldErrors(form);
            UI.hideFormAlert(alertBox);

            const username = usernameInput.value.trim();
            const password = passwordInput.value;
            const errors = {};

            if (!username) {
                errors.username = 'Please enter your username.';
            }
            if (!password) {
                errors.password = 'Please enter your password.';
            }
            if (Object.keys(errors).length > 0) {
                UI.showFieldErrors(form, errors);
                return;
            }

            UI.setButtonLoading(submitButton, true, 'Signing in');

            try {
                const data = await Api.post(
                    '/api/auth/login',
                    { username: username, password: password },
                    { anonymous: true });

                Session.save({
                    token: data.token,
                    role: data.role,
                    fullName: data.fullName,
                    studentId: data.studentId,
                    expiresAt: data.expiresAt
                });

                try {
                    if (rememberBox.checked) {
                        localStorage.setItem(REMEMBER_KEY, username);
                    } else {
                        localStorage.removeItem(REMEMBER_KEY);
                    }
                } catch (e) {
                    // Remembering the username is optional.
                }

                UI.flash(`Welcome, ${data.fullName}.`, 'success');
                window.location.assign(homeFor(data.role));
            } catch (error) {
                UI.setButtonLoading(submitButton, false);

                if (error.status === 400 && error.errors && UI.showFieldErrors(form, error.errors) > 0) {
                    return;
                }

                const message = error.status === 401
                    ? 'Invalid username or password. Please try again.'
                    : (error.message || 'Sign in failed. Please try again.');

                UI.showFormAlert(alertBox, message);
                UI.shake(alertBox);
                UI.toast(message, 'error', { title: 'Sign in failed' });
                passwordInput.focus();
                passwordInput.select();
            }
        });
    }

    // This script is loaded at the end of <body>, so the elements above already exist.
    guardShell();
    redirectFromHome();
    bindCommonLinks();
    initLoginPage();

    return {
        homeFor: homeFor,
        currentSession: currentSession,
        requireRole: requireRole,
        logout: logout
    };
})();