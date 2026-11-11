'use strict';

(() => {
    const session = Auth.requireRole('Faculty');
    if (!session) {
        return;
    }

    const esc = UI.escapeHtml;
    const CODE_PATTERN = /^[A-Za-z0-9 -]{2,20}$/;

    const searchInput = document.getElementById('subject-search');
    const countLabel = document.getElementById('subjects-count');
    const addButton = document.getElementById('btn-add-subject');
    const tableElement = document.getElementById('subjects-table');

    const modalElement = document.getElementById('subject-modal');
    const modal = bootstrap.Modal.getOrCreateInstance(modalElement);
    const form = document.getElementById('subject-form');
    const formAlert = document.getElementById('subject-form-alert');
    const modalTitle = document.getElementById('subject-modal-title');
    const saveButton = document.getElementById('subject-save');

    const codeInput = document.getElementById('subject-code');
    const nameInput = document.getElementById('subject-name');
    const unitsInput = document.getElementById('subject-units');
    const descriptionInput = document.getElementById('subject-description');

    let allSubjects = [];
    let currentSearch = '';
    let editingId = null;
    let lastSavedId = null;

    function renderRow(subject) {
        const id = subject.subjectId;
        const code = esc(subject.subjectCode);
        const selected = id === lastSavedId ? ' class="row-selected"' : '';
        const description = subject.description
            ? `<span class="cell-truncate" title="${esc(subject.description)}">${esc(subject.description)}</span>`
            : '<span class="text-muted-ac">--</span>';

        return `<tr data-id="${id}"${selected}>` +
            `<td><span class="code-chip">${code}</span></td>` +
            `<td><span class="cell-main">${esc(subject.subjectName)}</span></td>` +
            `<td class="col-num">${subject.units}</td>` +
            `<td>${description}</td>` +
            `<td class="col-actions"><div class="row-actions">` +
            `<button type="button" class="action-btn" data-action="edit" data-id="${id}" data-bs-toggle="tooltip" title="Edit" aria-label="Edit ${code}"><i class="bi bi-pencil-square" aria-hidden="true"></i></button>` +
            `<button type="button" class="action-btn action-btn-danger" data-action="delete" data-id="${id}" data-bs-toggle="tooltip" title="Delete" aria-label="Delete ${code}"><i class="bi bi-trash3" aria-hidden="true"></i></button>` +
            `</div></td></tr>`;
    }

    function emptyHtml() {
        if (currentSearch) {
            return UI.emptyStateHtml({
                title: `No subjects match "${currentSearch}"`,
                text: 'Try a different code or name.'
            });
        }
        return UI.emptyStateHtml({
            title: 'No subjects yet',
            text: 'Add the first subject so grades can be recorded.',
            actionLabel: 'Add Subject'
        });
    }

    const table = UI.createDataTable({
        table: tableElement,
        pager: document.getElementById('subjects-pager'),
        info: document.getElementById('subjects-info'),
        pageSize: 10,
        defaultSort: 'subjectCode',
        renderRow: renderRow,
        emptyHtml: emptyHtml,
        onEmptyAction: openCreate,
        onRetry: load
    });

    function applyFilter() {
        const term = currentSearch.toLowerCase();
        const filtered = term
            ? allSubjects.filter(subject =>
                subject.subjectCode.toLowerCase().includes(term) ||
                subject.subjectName.toLowerCase().includes(term) ||
                (subject.description || '').toLowerCase().includes(term))
            : allSubjects;

        table.setRows(filtered);

        countLabel.textContent = term
            ? `${filtered.length} of ${allSubjects.length} subjects`
            : `${allSubjects.length} subject${allSubjects.length === 1 ? '' : 's'}`;
    }

    async function load() {
        table.showSkeleton(6);

        try {
            allSubjects = await Api.get('/api/subjects');
            applyFilter();

            if (lastSavedId !== null) {
                const savedId = lastSavedId;
                table.showRow(row => row.subjectId === savedId);
                lastSavedId = null;
            }
        } catch (error) {
            countLabel.textContent = '';
            table.showError(error.message);
            UI.toastError(error, 'Could not load subjects.');
        }
    }

    function resetForm() {
        form.reset();
        UI.clearFieldErrors(form);
        UI.hideFormAlert(formAlert);
        UI.setButtonLoading(saveButton, false);
    }

    function openCreate() {
        editingId = null;
        resetForm();
        modalTitle.textContent = 'Add Subject';
        unitsInput.value = '3';
        modal.show();
    }

    function openEdit(subject) {
        editingId = subject.subjectId;
        resetForm();
        modalTitle.textContent = 'Edit Subject';
        codeInput.value = subject.subjectCode;
        nameInput.value = subject.subjectName;
        unitsInput.value = String(subject.units);
        descriptionInput.value = subject.description || '';
        modal.show();
    }

    function collect() {
        const errors = {};
        const units = Number(unitsInput.value);

        const payload = {
            subjectCode: codeInput.value.trim().toUpperCase(),
            subjectName: nameInput.value.trim(),
            units: Number.isInteger(units) ? units : 0,
            description: descriptionInput.value.trim() || null
        };

        if (!payload.subjectCode) {
            errors.subjectCode = 'Subject code is required.';
        } else if (!CODE_PATTERN.test(payload.subjectCode)) {
            errors.subjectCode = 'Use 2 to 20 letters, numbers, spaces or hyphens.';
        }
        if (!payload.subjectName) {
            errors.subjectName = 'Subject name is required.';
        }
        if (unitsInput.value.trim() === '' || !Number.isInteger(units) || units < 1 || units > 10) {
            errors.units = 'Units must be a whole number from 1 to 10.';
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
                ? await Api.put(`/api/subjects/${editingId}`, payload)
                : await Api.post('/api/subjects', payload);

            lastSavedId = saved.subjectId;
            modal.hide();
            UI.toast(`${saved.subjectCode} was ${isEdit ? 'updated' : 'added'}.`, 'success');
            await load();
        } catch (error) {
            UI.handleFormError(form, formAlert, error);
            if (error.status === 409) {
                UI.setFieldError(form, 'subjectCode', error.message);
            }
        } finally {
            UI.setButtonLoading(saveButton, false);
        }
    });

    modalElement.addEventListener('shown.bs.modal', () => codeInput.focus());
    UI.bindLiveValidation(form);

    async function remove(subject, button) {
        const confirmed = await UI.confirm({
            title: 'Delete subject?',
            message: `${subject.subjectCode} (${subject.subjectName}) will be removed. ` +
                'A subject that still has grade records cannot be deleted.',
            confirmText: 'Delete subject'
        });
        if (!confirmed) {
            return;
        }

        if (button) {
            button.disabled = true;
        }

        try {
            await Api.del(`/api/subjects/${subject.subjectId}`);
            UI.toast(`${subject.subjectCode} was deleted.`, 'success');
            await load();
        } catch (error) {
            if (button) {
                button.disabled = false;
            }
            UI.toastError(error, 'Could not delete the subject.');
            if (error.status === 404) {
                load();
            }
        }
    }

    tableElement.querySelector('tbody').addEventListener('click', event => {
        const button = event.target.closest('[data-action]');
        if (!button) {
            return;
        }
        const subject = allSubjects.find(item => item.subjectId === Number(button.dataset.id));
        if (!subject) {
            return;
        }

        if (button.dataset.action === 'edit') {
            openEdit(subject);
        } else if (button.dataset.action === 'delete') {
            remove(subject, button);
        }
    });

    addButton.addEventListener('click', openCreate);

    searchInput.addEventListener('input', UI.debounce(() => {
        currentSearch = searchInput.value.trim();
        applyFilter();
    }, 300));

    load();
})();