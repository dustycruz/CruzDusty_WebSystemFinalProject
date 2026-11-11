'use strict';

(() => {
    const session = Auth.requireRole('Faculty');
    if (!session) {
        return;
    }

    const esc = UI.escapeHtml;
    const SCHOOL_YEAR_PATTERN = /^(\d{4})-(\d{4})$/;
    const PREVIEW_IDLE_TEXT =
        'Enter scores to see a preview. This is a preview only: the saved value is always recomputed by the server.';

    const filterStudent = document.getElementById('filter-student');
    const filterSubject = document.getElementById('filter-subject');
    const filterSchoolYear = document.getElementById('filter-school-year');
    const filterSemester = document.getElementById('filter-semester');
    const clearFiltersButton = document.getElementById('btn-clear-filters');
    const searchInput = document.getElementById('grade-search');
    const countLabel = document.getElementById('grades-count');
    const addButton = document.getElementById('btn-add-grade');
    const tableElement = document.getElementById('grades-table');

    const modalElement = document.getElementById('grade-modal');
    const modal = bootstrap.Modal.getOrCreateInstance(modalElement);
    const form = document.getElementById('grade-form');
    const formAlert = document.getElementById('grade-form-alert');
    const modalTitle = document.getElementById('grade-modal-title');
    const saveButton = document.getElementById('grade-save');

    const studentSelect = document.getElementById('grade-student');
    const subjectSelect = document.getElementById('grade-subject');
    const schoolYearInput = document.getElementById('grade-school-year');
    const schoolYearOptions = document.getElementById('school-year-options');
    const semesterSelect = document.getElementById('grade-semester');
    const scoreInputs = {
        prelim: document.getElementById('grade-prelim'),
        midterm: document.getElementById('grade-midterm'),
        finals: document.getElementById('grade-finals')
    };
    const SCORE_LABELS = { prelim: 'Prelim', midterm: 'Midterm', finals: 'Finals' };

    const previewBox = document.getElementById('grade-preview');
    const previewFinal = document.getElementById('preview-final');
    const previewRemarks = document.getElementById('preview-remarks');
    const previewStatus = document.getElementById('preview-status');

    const viewModalElement = document.getElementById('grade-view-modal');
    const viewModal = bootstrap.Modal.getOrCreateInstance(viewModalElement);
    const viewBody = document.getElementById('grade-view-body');
    const viewEditButton = document.getElementById('grade-view-edit');

    let students = [];
    let subjects = [];
    let grades = [];
    let currentSearch = '';
    let editingId = null;
    let viewingGrade = null;
    let lastSavedId = null;
    let loadSequence = 0;
    let previewSequence = 0;

    // ------------------------------------------------------------------
    // School years
    // ------------------------------------------------------------------
    function currentSchoolYearStart() {
        const now = new Date();
        // A school year is taken to start in June.
        return now.getMonth() >= 5 ? now.getFullYear() : now.getFullYear() - 1;
    }

    function defaultSchoolYear() {
        const start = currentSchoolYearStart();
        return `${start}-${start + 1}`;
    }

    function fillSchoolYears() {
        const start = currentSchoolYearStart();
        const years = new Set();
        for (let year = start + 1; year >= start - 4; year -= 1) {
            years.add(`${year}-${year + 1}`);
        }
        grades.forEach(grade => years.add(grade.schoolYear));

        const sorted = Array.from(years).sort().reverse();
        const selected = filterSchoolYear.value;

        filterSchoolYear.innerHTML = '<option value="">All years</option>' +
            sorted.map(year => `<option value="${esc(year)}">${esc(year)}</option>`).join('');
        if (selected && sorted.includes(selected)) {
            filterSchoolYear.value = selected;
        }

        schoolYearOptions.innerHTML = sorted.map(year => `<option value="${esc(year)}"></option>`).join('');
    }

    // ------------------------------------------------------------------
    // Table
    // ------------------------------------------------------------------
    function renderRow(grade) {
        const id = grade.gradeRecordId;
        const justSaved = id === lastSavedId;
        const label = `${esc(grade.subjectCode)} for ${esc(grade.studentName)}`;

        return `<tr data-id="${id}"${justSaved ? ' class="row-selected"' : ''}>` +
            `<td><span class="cell-main">${esc(grade.studentName)}</span><span class="cell-sub">${esc(grade.studentNumber)}</span></td>` +
            `<td><span class="code-chip">${esc(grade.subjectCode)}</span><span class="cell-sub">${esc(grade.subjectName)}</span></td>` +
            `<td>${esc(grade.schoolYear)}</td>` +
            `<td>${esc(grade.semester)}</td>` +
            `<td class="col-num">${UI.formatScore(grade.prelim)}</td>` +
            `<td class="col-num">${UI.formatScore(grade.midterm)}</td>` +
            `<td class="col-num">${UI.formatScore(grade.finals)}</td>` +
            `<td class="col-num"><strong>${UI.formatScore(grade.finalGrade)}</strong></td>` +
            `<td>${UI.remarksBadge(grade.remarks, justSaved)}</td>` +
            `<td class="col-actions"><div class="row-actions">` +
            `<button type="button" class="action-btn" data-action="view" data-id="${id}" data-bs-toggle="tooltip" title="View details" aria-label="View ${label}"><i class="bi bi-eye" aria-hidden="true"></i></button>` +
            `<button type="button" class="action-btn" data-action="edit" data-id="${id}" data-bs-toggle="tooltip" title="Edit" aria-label="Edit ${label}"><i class="bi bi-pencil-square" aria-hidden="true"></i></button>` +
            `<button type="button" class="action-btn action-btn-danger" data-action="delete" data-id="${id}" data-bs-toggle="tooltip" title="Delete" aria-label="Delete ${label}"><i class="bi bi-trash3" aria-hidden="true"></i></button>` +
            `</div></td></tr>`;
    }

    function hasActiveFilters() {
        return Boolean(filterStudent.value || filterSubject.value || filterSchoolYear.value ||
            filterSemester.value || currentSearch);
    }

    function emptyHtml() {
        if (hasActiveFilters()) {
            return UI.emptyStateHtml({
                title: 'No grade records match these filters',
                text: 'Change or clear the filters to see more records.'
            });
        }
        return UI.emptyStateHtml({
            title: 'No grade records yet',
            text: 'Add the first one.',
            actionLabel: 'Add Grade Record'
        });
    }

    const table = UI.createDataTable({
        table: tableElement,
        pager: document.getElementById('grades-pager'),
        info: document.getElementById('grades-info'),
        pageSize: 10,
        renderRow: renderRow,
        emptyHtml: emptyHtml,
        onEmptyAction: openCreate,
        onRetry: loadGrades
    });

    function applySearch() {
        const term = currentSearch.toLowerCase();
        const filtered = term
            ? grades.filter(grade => [
                grade.studentName, grade.studentNumber, grade.subjectCode, grade.subjectName,
                grade.schoolYear, grade.semester, grade.remarks
            ].some(value => String(value).toLowerCase().includes(term)))
            : grades;

        table.setRows(filtered);

        countLabel.textContent = term
            ? `${filtered.length} of ${grades.length} records`
            : `${grades.length} record${grades.length === 1 ? '' : 's'}`;
    }

    // Filtering by student, subject, school year and semester is done by the API.
    async function loadGrades() {
        const sequence = ++loadSequence;
        table.showSkeleton(6);

        try {
            const result = await Api.get('/api/grades', {
                studentId: filterStudent.value,
                subjectId: filterSubject.value,
                schoolYear: filterSchoolYear.value,
                semester: filterSemester.value
            });
            if (sequence !== loadSequence) {
                return;
            }

            grades = result;
            fillSchoolYears();
            applySearch();

            if (lastSavedId !== null) {
                const savedId = lastSavedId;
                table.showRow(row => row.gradeRecordId === savedId);
                lastSavedId = null;
            }
        } catch (error) {
            if (sequence !== loadSequence) {
                return;
            }
            countLabel.textContent = '';
            table.showError(error.message);
            UI.toastError(error, 'Could not load grade records.');
        }
    }

    // ------------------------------------------------------------------
    // Lookups for the dropdowns
    // ------------------------------------------------------------------
    function fillSelect(select, items, valueKey, labelOf, placeholder) {
        const selected = select.value;
        select.innerHTML = `<option value="">${esc(placeholder)}</option>` +
            items.map(item => `<option value="${item[valueKey]}">${esc(labelOf(item))}</option>`).join('');
        if (selected) {
            select.value = selected;
        }
    }

    async function loadLookups() {
        const [studentList, subjectList] = await Promise.all([
            Api.get('/api/students'),
            Api.get('/api/subjects')
        ]);

        students = studentList;
        subjects = subjectList;

        const studentLabel = student => `${student.fullName} (${student.studentNumber})`;
        const subjectLabel = subject => `${subject.subjectCode} - ${subject.subjectName}`;

        fillSelect(filterStudent, students, 'studentId', studentLabel, 'All students');
        fillSelect(studentSelect, students, 'studentId', studentLabel, 'Select a student');
        fillSelect(filterSubject, subjects, 'subjectId', subjectLabel, 'All subjects');
        fillSelect(subjectSelect, subjects, 'subjectId', subjectLabel, 'Select a subject');
    }

    // ------------------------------------------------------------------
    // Scores and the server-computed preview
    // ------------------------------------------------------------------
    function readScore(name) {
        const input = scoreInputs[name];
        const label = SCORE_LABELS[name];
        const raw = input.value.trim();

        if (raw === '') {
            if (input.validity && input.validity.badInput) {
                return { value: null, error: `${label} must be a number.` };
            }
            return { value: null, error: null };
        }

        const number = Number(raw);
        if (!Number.isFinite(number)) {
            return { value: null, error: `${label} must be a number.` };
        }
        if (number < 0 || number > 100) {
            return { value: number, error: `${label} must be between 0 and 100.` };
        }
        return { value: Math.round(number * 100) / 100, error: null };
    }

    function readScores() {
        const values = {};
        const errors = {};
        Object.keys(scoreInputs).forEach(name => {
            const result = readScore(name);
            values[name] = result.value;
            if (result.error) {
                errors[name] = result.error;
            }
        });
        return { values: values, errors: errors };
    }

    function setPreview(finalText, remarksHtml, statusText) {
        previewFinal.textContent = finalText;
        previewRemarks.innerHTML = remarksHtml;
        previewStatus.textContent = statusText;
    }

    function resetPreview() {
        previewSequence += 1;
        previewBox.classList.remove('is-loading');
        setPreview('--', '--', PREVIEW_IDLE_TEXT);
    }

    async function requestPreview() {
        const { values, errors } = readScores();

        if (Object.keys(errors).length > 0) {
            previewSequence += 1;
            previewBox.classList.remove('is-loading');
            setPreview('--', '--', 'Fix the highlighted scores to see a preview.');
            return;
        }

        if (values.prelim === null && values.midterm === null && values.finals === null) {
            resetPreview();
            return;
        }

        const sequence = ++previewSequence;
        previewBox.classList.add('is-loading');
        previewStatus.textContent = 'Asking the server to compute the preview.';

        try {
            const result = await Api.post('/api/grades/preview', values);
            if (sequence !== previewSequence) {
                return;
            }
            setPreview(
                UI.formatScore(result.finalGrade),
                UI.remarksBadge(result.remarks, true),
                'Preview computed by the server. The saved value is recomputed by the server when you save.');
        } catch (error) {
            if (sequence !== previewSequence) {
                return;
            }
            setPreview('--', '--', error.message || 'The preview is not available right now.');
        } finally {
            if (sequence === previewSequence) {
                previewBox.classList.remove('is-loading');
            }
        }
    }

    const requestPreviewDebounced = UI.debounce(requestPreview, 400);

    Object.keys(scoreInputs).forEach(name => {
        scoreInputs[name].addEventListener('input', () => {
            const result = readScore(name);
            if (result.error) {
                UI.setFieldError(form, name, result.error);
            }
            requestPreviewDebounced();
        });
    });

    // ------------------------------------------------------------------
    // Create / edit form
    // ------------------------------------------------------------------
    function resetForm() {
        form.reset();
        UI.clearFieldErrors(form);
        UI.hideFormAlert(formAlert);
        UI.setButtonLoading(saveButton, false);
        resetPreview();
    }

    function openCreate() {
        if (students.length === 0 || subjects.length === 0) {
            UI.toast('Add at least one student and one subject before recording grades.', 'info');
            return;
        }

        editingId = null;
        resetForm();
        modalTitle.textContent = 'Add Grade Record';
        studentSelect.value = filterStudent.value;
        subjectSelect.value = filterSubject.value;
        schoolYearInput.value = filterSchoolYear.value || defaultSchoolYear();
        semesterSelect.value = filterSemester.value;
        modal.show();
    }

    function openEdit(grade) {
        editingId = grade.gradeRecordId;
        resetForm();
        modalTitle.textContent = 'Edit Grade Record';
        studentSelect.value = String(grade.studentId);
        subjectSelect.value = String(grade.subjectId);
        schoolYearInput.value = grade.schoolYear;
        semesterSelect.value = grade.semester;
        scoreInputs.prelim.value = grade.prelim === null ? '' : String(grade.prelim);
        scoreInputs.midterm.value = grade.midterm === null ? '' : String(grade.midterm);
        scoreInputs.finals.value = grade.finals === null ? '' : String(grade.finals);
        modal.show();
        requestPreview();
    }

    function collect() {
        const scores = readScores();
        const errors = Object.assign({}, scores.errors);

        // There is no finalGrade and no remarks here: the server computes both.
        const payload = {
            studentId: Number(studentSelect.value) || 0,
            subjectId: Number(subjectSelect.value) || 0,
            schoolYear: schoolYearInput.value.trim(),
            semester: semesterSelect.value,
            prelim: scores.values.prelim,
            midterm: scores.values.midterm,
            finals: scores.values.finals
        };

        if (!payload.studentId) {
            errors.studentId = 'Select a student.';
        }
        if (!payload.subjectId) {
            errors.subjectId = 'Select a subject.';
        }

        const match = SCHOOL_YEAR_PATTERN.exec(payload.schoolYear);
        if (!payload.schoolYear) {
            errors.schoolYear = 'School year is required.';
        } else if (!match) {
            errors.schoolYear = 'School year must look like 2026-2027.';
        } else if (Number(match[2]) !== Number(match[1]) + 1) {
            errors.schoolYear = 'Use two consecutive years, for example 2026-2027.';
        }

        if (!payload.semester) {
            errors.semester = 'Select a semester.';
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
                ? await Api.put(`/api/grades/${editingId}`, payload)
                : await Api.post('/api/grades', payload);

            lastSavedId = saved.gradeRecordId;
            modal.hide();

            const result = saved.finalGrade === null
                ? `Remarks: ${saved.remarks}`
                : `Final grade ${UI.formatScore(saved.finalGrade)}, ${saved.remarks}`;
            UI.toast(
                `Grade record ${isEdit ? 'updated' : 'added'}. ${result} (computed by the server).`,
                'success');

            await loadGrades();
        } catch (error) {
            UI.handleFormError(form, formAlert, error);
        } finally {
            UI.setButtonLoading(saveButton, false);
        }
    });

    modalElement.addEventListener('shown.bs.modal', () => studentSelect.focus());
    UI.bindLiveValidation(form);

    // ------------------------------------------------------------------
    // View details
    // ------------------------------------------------------------------
    async function openView(grade) {
        viewingGrade = grade;
        viewBody.innerHTML =
            '<dt><span class="skeleton skeleton-line"></span></dt><dd><span class="skeleton skeleton-line"></span></dd>' +
            '<dt><span class="skeleton skeleton-line"></span></dt><dd><span class="skeleton skeleton-line"></span></dd>' +
            '<dt><span class="skeleton skeleton-line"></span></dt><dd><span class="skeleton skeleton-line"></span></dd>';
        viewModal.show();

        try {
            const fresh = await Api.get(`/api/grades/${grade.gradeRecordId}`);
            viewingGrade = fresh;

            viewBody.innerHTML = UI.detailListHtml([
                ['Student', `${esc(fresh.studentName)} <span class="cell-sub">${esc(fresh.studentNumber)}</span>`],
                ['Subject', `<span class="code-chip">${esc(fresh.subjectCode)}</span> <span class="cell-sub">${esc(fresh.subjectName)}</span>`],
                ['Units', String(fresh.units)],
                ['School year', esc(fresh.schoolYear)],
                ['Semester', esc(fresh.semester)],
                ['Prelim (30%)', UI.formatScore(fresh.prelim)],
                ['Midterm (30%)', UI.formatScore(fresh.midterm)],
                ['Finals (40%)', UI.formatScore(fresh.finals)],
                ['Final grade', UI.formatScore(fresh.finalGrade)],
                ['Remarks', UI.remarksBadge(fresh.remarks)],
                ['Created', esc(UI.formatDateTime(fresh.createdAt))],
                ['Last updated', esc(UI.formatDateTime(fresh.updatedAt))]
            ]);
        } catch (error) {
            viewModal.hide();
            UI.toastError(error, 'Could not load the grade record.');
            if (error.status === 404) {
                loadGrades();
            }
        }
    }

    viewEditButton.addEventListener('click', () => {
        const grade = viewingGrade;
        if (!grade) {
            return;
        }
        viewModalElement.addEventListener('hidden.bs.modal', () => openEdit(grade), { once: true });
        viewModal.hide();
    });

    // ------------------------------------------------------------------
    // Delete
    // ------------------------------------------------------------------
    async function remove(grade, button) {
        const confirmed = await UI.confirm({
            title: 'Delete grade record?',
            message: `${grade.subjectCode} for ${grade.studentName} (${grade.schoolYear}, ${grade.semester}) ` +
                'will be removed. This cannot be undone.',
            confirmText: 'Delete record'
        });
        if (!confirmed) {
            return;
        }

        if (button) {
            button.disabled = true;
        }

        try {
            await Api.del(`/api/grades/${grade.gradeRecordId}`);
            UI.toast('The grade record was deleted.', 'success');
            await loadGrades();
        } catch (error) {
            if (button) {
                button.disabled = false;
            }
            UI.toastError(error, 'Could not delete the grade record.');
            if (error.status === 404) {
                loadGrades();
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
        const grade = grades.find(item => item.gradeRecordId === Number(button.dataset.id));
        if (!grade) {
            return;
        }

        if (button.dataset.action === 'view') {
            openView(grade);
        } else if (button.dataset.action === 'edit') {
            openEdit(grade);
        } else if (button.dataset.action === 'delete') {
            remove(grade, button);
        }
    });

    addButton.addEventListener('click', openCreate);

    [filterStudent, filterSubject, filterSchoolYear, filterSemester].forEach(select => {
        select.addEventListener('change', loadGrades);
    });

    clearFiltersButton.addEventListener('click', () => {
        filterStudent.value = '';
        filterSubject.value = '';
        filterSchoolYear.value = '';
        filterSemester.value = '';
        searchInput.value = '';
        currentSearch = '';
        loadGrades();
        UI.toast('Filters cleared.', 'info');
    });

    searchInput.addEventListener('input', UI.debounce(() => {
        currentSearch = searchInput.value.trim();
        applySearch();
    }, 300));

    // ------------------------------------------------------------------
    // Start
    // ------------------------------------------------------------------
    async function start() {
        table.showSkeleton(6);
        fillSchoolYears();

        try {
            await loadLookups();
        } catch (error) {
            table.showError(error.message);
            UI.toastError(error, 'Could not load students and subjects.');
            return;
        }

        // Arriving from "View grades" on the Students page: /Faculty/Grades?studentId=5
        const requestedStudent = new URLSearchParams(window.location.search).get('studentId');
        if (requestedStudent && students.some(student => String(student.studentId) === requestedStudent)) {
            filterStudent.value = requestedStudent;
        }

        await loadGrades();
    }

    start();
})();