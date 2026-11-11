'use strict';

(() => {
    const session = Auth.requireRole('Faculty');
    if (!session) {
        return;
    }

    const esc = UI.escapeHtml;
    const YEAR_LABELS = ['', '1st Year', '2nd Year', '3rd Year', '4th Year', '5th Year', '6th Year'];
    const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    const USERNAME_PATTERN = /^[A-Za-z0-9._-]{3,50}$/;

    const searchInput = document.getElementById('student-search');
    const countLabel = document.getElementById('students-count');
    const addButton = document.getElementById('btn-add-student');
    const tableElement = document.getElementById('students-table');

    const modalElement = document.getElementById('student-modal');
    const modal = bootstrap.Modal.getOrCreateInstance(modalElement);
    const form = document.getElementById('student-form');
    const formAlert = document.getElementById('student-form-alert');
    const modalTitle = document.getElementById('student-modal-title');
    const saveButton = document.getElementById('student-save');

    const fields = {
        studentNumber: document.getElementById('student-number'),
        email: document.getElementById('student-email'),
        firstName: document.getElementById('student-first-name'),
        middleName: document.getElementById('student-middle-name'),
        lastName: document.getElementById('student-last-name'),
        course: document.getElementById('student-course'),
        yearLevel: document.getElementById('student-year-level'),
        username: document.getElementById('student-username'),
        password: document.getElementById('student-password')
    };
    const usernameRequired = document.getElementById('student-username-req');
    const usernameHelp = document.getElementById('student-username-help');
    const passwordLabel = document.getElementById('student-password-label');
    const passwordRequired = document.getElementById('student-password-req');
    const passwordHelp = document.getElementById('student-password-help');

    const viewModalElement = document.getElementById('student-view-modal');
    const viewModal = bootstrap.Modal.getOrCreateInstance(viewModalElement);
    const viewBody = document.getElementById('student-view-body');
    const viewGradesLink = document.getElementById('student-view-grades');
    const viewEditButton = document.getElementById('student-view-edit');

    let editingId = null;
    let editingHasAccount = false;
    let viewingStudent = null;
    let lastSavedId = null;
    let currentSearch = '';
    let requestSequence = 0;

    function yearLabel(level) {
        return YEAR_LABELS[level] || `Year ${level}`;
    }

    function renderRow(student) {
        const id = student.studentId;
        const name = esc(student.fullName);
        const selected = id === lastSavedId ? ' class="row-selected"' : '';
        const email = student.email ? `<span class="cell-sub">${esc(student.email)}</span>` : '';
        const username = student.username
            ? esc(student.username)
            : '<span class="text-muted-ac">No login</span>';

        return `<tr data-id="${id}"${selected}>` +
            `<td><span class="code-chip">${esc(student.studentNumber)}</span></td>` +
            `<td><span class="cell-main">${name}</span>${email}</td>` +
            `<td>${esc(student.course)}</td>` +
            `<td>${esc(yearLabel(student.yearLevel))}</td>` +
            `<td>${username}</td>` +
            `<td class="col-actions"><div class="row-actions">` +
            `<button type="button" class="action-btn" data-action="view" data-id="${id}" data-bs-toggle="tooltip" title="View details" aria-label="View ${name}"><i class="bi bi-eye" aria-hidden="true"></i></button>` +
            `<button type="button" class="action-btn" data-action="edit" data-id="${id}" data-bs-toggle="tooltip" title="Edit" aria-label="Edit ${name}"><i class="bi bi-pencil-square" aria-hidden="true"></i></button>` +
            `<button type="button" class="action-btn action-btn-danger" data-action="delete" data-id="${id}" data-bs-toggle="tooltip" title="Delete" aria-label="Delete ${name}"><i class="bi bi-trash3" aria-hidden="true"></i></button>` +
            `</div></td></tr>`;
    }

    function emptyHtml() {
        if (currentSearch) {
            return UI.emptyStateHtml({
                title: `No students match "${currentSearch}"`,
                text: 'Try a different name or student number.'
            });
        }
        return UI.emptyStateHtml({
            title: 'No students yet',
            text: 'Add the first student to get started.',
            actionLabel: 'Add Student'
        });
    }

    const table = UI.createDataTable({
        table: tableElement,
        pager: document.getElementById('students-pager'),
        info: document.getElementById('students-info'),
        pageSize: 10,
        defaultSort: 'fullName',
        renderRow: renderRow,
        emptyHtml: emptyHtml,
        onEmptyAction: openCreate,
        onRetry: load
    });

    async function load() {
        const sequence = ++requestSequence;
        table.showSkeleton(6);

        try {
            const students = await Api.get('/api/students', { search: currentSearch });
            if (sequence !== requestSequence) {
                return;
            }

            table.setRows(students);
            countLabel.textContent = `${students.length} student${students.length === 1 ? '' : 's'}`;

            if (lastSavedId !== null) {
                const savedId = lastSavedId;
                table.showRow(row => row.studentId === savedId);
                lastSavedId = null;
            }
        } catch (error) {
            if (sequence !== requestSequence) {
                return;
            }
            countLabel.textContent = '';
            table.showError(error.message);
            UI.toastError(error, 'Could not load students.');
        }
    }

    function findStudent(id) {
        return table.getRows().find(row => row.studentId === id) || null;
    }

    // ------------------------------------------------------------------
    // Create / edit form
    // ------------------------------------------------------------------
    function resetForm() {
        form.reset();
        UI.clearFieldErrors(form);
        UI.hideFormAlert(formAlert);
        UI.setButtonLoading(saveButton, false);
    }

    function openCreate() {
        editingId = null;
        editingHasAccount = false;
        resetForm();

        modalTitle.textContent = 'Add Student';
        fields.username.disabled = false;
        usernameRequired.hidden = false;
        usernameHelp.textContent = '3 to 50 characters: letters, numbers, dot, underscore or hyphen.';
        passwordLabel.textContent = 'Password';
        passwordRequired.hidden = false;
        passwordHelp.textContent = 'At least 8 characters.';

        modal.show();
    }

    function openEdit(student) {
        editingId = student.studentId;
        editingHasAccount = Boolean(student.hasAccount);
        resetForm();

        modalTitle.textContent = 'Edit Student';
        fields.studentNumber.value = student.studentNumber;
        fields.email.value = student.email || '';
        fields.firstName.value = student.firstName;
        fields.middleName.value = student.middleName || '';
        fields.lastName.value = student.lastName;
        fields.course.value = student.course;
        fields.yearLevel.value = String(student.yearLevel);
        fields.username.value = student.username || '';
        fields.username.disabled = true;
        usernameRequired.hidden = true;
        passwordLabel.textContent = 'New password';
        passwordRequired.hidden = true;

        if (editingHasAccount) {
            usernameHelp.textContent = 'The username cannot be changed.';
            passwordHelp.textContent = 'Leave blank to keep the current password.';
        } else {
            usernameHelp.textContent = 'This student has no login yet.';
            passwordHelp.textContent = 'Enter a password to create a login. The username will be the student number.';
        }

        modal.show();
    }

    function collect() {
        const errors = {};
        const value = name => fields[name].value.trim();

        const payload = {
            studentNumber: value('studentNumber'),
            firstName: value('firstName'),
            middleName: value('middleName') || null,
            lastName: value('lastName'),
            email: value('email') || null,
            course: value('course'),
            yearLevel: Number(fields.yearLevel.value) || 0
        };

        if (!payload.studentNumber) {
            errors.studentNumber = 'Student number is required.';
        } else if (payload.studentNumber.length < 3) {
            errors.studentNumber = 'Student number must be at least 3 characters.';
        }
        if (!payload.firstName) {
            errors.firstName = 'First name is required.';
        }
        if (!payload.lastName) {
            errors.lastName = 'Last name is required.';
        }
        if (payload.email && !EMAIL_PATTERN.test(payload.email)) {
            errors.email = 'Enter a valid email address.';
        }
        if (!payload.course) {
            errors.course = 'Course is required.';
        }
        if (payload.yearLevel < 1 || payload.yearLevel > 6) {
            errors.yearLevel = 'Select a year level.';
        }

        const password = fields.password.value;

        if (editingId === null) {
            payload.username = value('username');
            payload.password = password;

            if (!payload.username) {
                errors.username = 'Username is required.';
            } else if (!USERNAME_PATTERN.test(payload.username)) {
                errors.username = 'Use 3 to 50 letters, numbers, dots, underscores or hyphens.';
            }
            if (!password) {
                errors.password = 'Password is required.';
            } else if (password.length < 8) {
                errors.password = 'Password must be at least 8 characters.';
            }
        } else {
            payload.newPassword = password || null;
            if (password && password.length < 8) {
                errors.password = 'New password must be at least 8 characters.';
            }
        }

        return { payload: payload, errors: errors };
    }

    form.addEventListener('submit', async event => {
        event.preventDefault();
        UI.clearFieldErrors(form);
        UI.hideFormAlert(formAlert);

        const { payload, errors } = collect();
        if (Object.keys(errors).length > 0) {
            UI.showFieldErrors(form, errors);
            return;
        }

        const isEdit = editingId !== null;
        UI.setButtonLoading(saveButton, true, 'Saving');

        try {
            const saved = isEdit
                ? await Api.put(`/api/students/${editingId}`, payload)
                : await Api.post('/api/students', payload);

            lastSavedId = saved.studentId;
            modal.hide();

            UI.toast(
                isEdit
                    ? `${saved.fullName} was updated.`
                    : `${saved.fullName} was added with the login "${saved.username}".`,
                'success');

            await load();
        } catch (error) {
            UI.handleFormError(form, formAlert, error, { newPassword: 'password' });

            if (error.status === 409) {
                if (/student number/i.test(error.message)) {
                    UI.setFieldError(form, 'studentNumber', error.message);
                } else if (/username/i.test(error.message) && !fields.username.disabled) {
                    UI.setFieldError(form, 'username', error.message);
                }
            }
        } finally {
            UI.setButtonLoading(saveButton, false);
        }
    });

    modalElement.addEventListener('shown.bs.modal', () => fields.studentNumber.focus());
    UI.bindLiveValidation(form);

    // ------------------------------------------------------------------
    // View details
    // ------------------------------------------------------------------
    async function openView(student) {
        viewingStudent = student;
        viewGradesLink.setAttribute('href', `/Faculty/Grades?studentId=${student.studentId}`);
        viewBody.innerHTML =
            '<dt><span class="skeleton skeleton-line"></span></dt><dd><span class="skeleton skeleton-line"></span></dd>' +
            '<dt><span class="skeleton skeleton-line"></span></dt><dd><span class="skeleton skeleton-line"></span></dd>' +
            '<dt><span class="skeleton skeleton-line"></span></dt><dd><span class="skeleton skeleton-line"></span></dd>';
        viewModal.show();

        try {
            const fresh = await Api.get(`/api/students/${student.studentId}`);
            viewingStudent = fresh;

            viewBody.innerHTML = UI.detailListHtml([
                ['Student number', `<span class="code-chip">${esc(fresh.studentNumber)}</span>`],
                ['Name', esc(fresh.fullName)],
                ['Email', fresh.email ? esc(fresh.email) : '--'],
                ['Course', esc(fresh.course)],
                ['Year level', esc(yearLabel(fresh.yearLevel))],
                ['Username', fresh.username ? esc(fresh.username) : 'No login'],
                ['Created', esc(UI.formatDateTime(fresh.createdAt))],
                ['Last updated', esc(UI.formatDateTime(fresh.updatedAt))]
            ]);
        } catch (error) {
            viewModal.hide();
            UI.toastError(error, 'Could not load the student.');
            if (error.status === 404) {
                load();
            }
        }
    }

    viewEditButton.addEventListener('click', () => {
        const student = viewingStudent;
        if (!student) {
            return;
        }
        viewModalElement.addEventListener('hidden.bs.modal', () => openEdit(student), { once: true });
        viewModal.hide();
    });

    // ------------------------------------------------------------------
    // Delete
    // ------------------------------------------------------------------
    async function remove(student, button) {
        const confirmed = await UI.confirm({
            title: 'Delete student?',
            message: `${student.fullName} (${student.studentNumber}) will be removed together with the login account ` +
                'and all grade records. This cannot be undone.',
            confirmText: 'Delete student'
        });
        if (!confirmed) {
            return;
        }

        if (button) {
            button.disabled = true;
        }

        try {
            await Api.del(`/api/students/${student.studentId}`);
            UI.toast(`${student.fullName} was deleted.`, 'success');
            await load();
        } catch (error) {
            if (button) {
                button.disabled = false;
            }
            UI.toastError(error, 'Could not delete the student.');
            if (error.status === 404) {
                load();
            }
        }
    }

    // ------------------------------------------------------------------
    // Events
    // ------------------------------------------------------------------
    tableElement.querySelector('tbody').addEventListener('click', event => {
        const button = event.target.closest('[data-action]');
        if (!button) {
            return;
        }
        const student = findStudent(Number(button.dataset.id));
        if (!student) {
            return;
        }

        if (button.dataset.action === 'view') {
            openView(student);
        } else if (button.dataset.action === 'edit') {
            openEdit(student);
        } else if (button.dataset.action === 'delete') {
            remove(student, button);
        }
    });

    addButton.addEventListener('click', openCreate);

    searchInput.addEventListener('input', UI.debounce(() => {
        currentSearch = searchInput.value.trim();
        load();
    }, 300));

    load();
})();