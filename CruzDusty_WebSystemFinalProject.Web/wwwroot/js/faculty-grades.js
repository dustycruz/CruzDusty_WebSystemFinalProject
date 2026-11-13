'use strict';

(() => {
    const session = Auth.requireRole('Faculty');
    if (!session) {
        return;
    }

    const esc = UI.escapeHtml;
    const SCHOOL_YEAR_PATTERN = /^(\d{4})-(\d{4})$/;
    const SUBJECT_CODE_PATTERN = /^[A-Za-z0-9 -]{2,30}$/;
    const WHOLE_NUMBER_PATTERN = /^\d{1,3}$/;
    const GRADE_MESSAGE = 'Grade must be a whole number from 0 to 100.';
    const PREVIEW_IDLE_TEXT =
        'Enter a grade to see a preview. The saved remark is always recomputed by the server.';

    const filterStudent = document.getElementById('filter-student');
    const filterSubject = document.getElementById('filter-subject');
    const filterSemester = document.getElementById('filter-semester');
    const filterSchoolYear = document.getElementById('filter-school-year');
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
    const subjectCodeInput = document.getElementById('grade-subject-code');
    const subjectCodeOptions = document.getElementById('subject-code-options');
    const subjectNameInput = document.getElementById('grade-subject-name');
    const gradeInput = document.getElementById('grade-value');
    const semesterSelect = document.getElementById('grade-semester');
    const schoolYearInput = document.getElementById('grade-school-year');
    const schoolYearOptions = document.getElementById('school-year-options');

    const previewBox = document.getElementById('grade-preview');
    const previewGrade = document.getElementById('preview-grade');
    const previewRemarks = document.getElementById('preview-remarks');
    const previewStatus = document.getElementById('preview-status');

    const viewModalElement = document.getElementById('grade-view-modal');
    const viewModal = bootstrap.Modal.getOrCreateInstance(viewModalElement);
    const viewBody = document.getElementById('grade-view-body');
    const viewEditButton = document.getElementById('grade-view-edit');

    let students = [];
    let grades = [];
    const knownSubjects = new Map();
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
    // Subjects: the exam schema stores code and name on each grade,
    // so the known subjects are collected from the loaded grade records.
    // ------------------------------------------------------------------
    function rememberSubjects(list) {
        list.forEach(grade => {
            if (!knownSubjects.has(grade.subjectCode)) {
                knownSubjects.set(grade.subjectCode, grade.subjectName);
            }
        });
    }

    function fillSubjects() {
        const codes = Array.from(knownSubjects.keys()).sort();
        const selected = filterSubject.value;

        filterSubject.innerHTML = '<option value="">All subjects</option>' +
            codes.map(code => `<option value="${esc(code)}">${esc(code)} - ${esc(knownSubjects.get(code))}</option>`).join('');
        if (selected && codes.includes(selected)) {
            filterSubject.value = selected;
        }

        subjectCodeOptions.innerHTML = codes
            .map(code => `<option value="${esc(code)}">${esc(knownSubjects.get(code))}</option>`)
            .join('');
    }

    // ------------------------------------------------------------------
    // Students
    // ------------------------------------------------------------------
    function studentLabel(student) {
        return `${student.fullName} (${student.studentNumber})`;
    }

    function fillStudentFilter() {
        const selected = filterStudent.value;
        filterStudent.innerHTML = '<option value="">All students</option>' +
            students.map(student => `<option value="${student.studentId}">${esc(studentLabel(student))}</option>`).join('');
        if (selected) {
            filterStudent.value = selected;
        }
    }

    // Only active students can receive new grades; an inactive student stays selectable
    // when editing a grade that already belongs to them.
    function fillStudentSelect(includeStudentId) {
        const options = students.filter(student => student.isActive || student.studentId === includeStudentId);
        studentSelect.innerHTML = '<option value="">Select a student</option>' +
            options.map(student =>
                `<option value="${student.studentId}">${esc(studentLabel(student))}${student.isActive ? '' : ' (inactive)'}</option>`)
                .join('');
    }

    async function loadStudents() {
        students = await Api.get('/api/students');
        fillStudentFilter();
        fillStudentSelect(null);
    }

    // ------------------------------------------------------------------
    // Table
    // ------------------------------------------------------------------
    function renderRow(grade) {
        const id = grade.gradeId;
        const justSaved = id === lastSavedId;
        const label = `${esc(grade.subjectCode)} for ${esc(grade.studentName)}`;

        return `<tr data-id="${id}"${justSaved ? ' class="row-selected"' : ''}>` +
            `<td><span class="cell-main">${esc(grade.studentName)}</span><span class="cell-sub">${esc(grade.studentNumber)}</span></td>` +
            `<td><span class="code-chip">${esc(grade.subjectCode)}</span><span class="cell-sub">${esc(grade.subjectName)}</span></td>` +
            `<td class="col-num"><strong>${Number(grade.grade)}</strong></td>` +
            `<td>${UI.remarksBadge(grade.remarks, justSaved)}</td>` +
            `<td>${esc(grade.semester)}</td>` +
            `<td>${esc(grade.schoolYear)}</td>` +
            `<td>${esc(grade.facultyName)}</td>` +
            `<td class="col-actions"><div class="row-actions">` +
            `<button type="button" class="action-btn" data-action="view" data-id="${id}" data-bs-toggle="tooltip" title="View details" aria-label="View ${label}"><i class="bi bi-eye" aria-hidden="true"></i></button>` +
            `<button type="button" class="action-btn" data-action="edit" data-id="${id}" data-bs-toggle="tooltip" title="Edit" aria-label="Edit ${label}"><i class="bi bi-pencil-square" aria-hidden="true"></i></button>` +
            `<button type="button" class="action-btn action-btn-danger" data-action="delete" data-id="${id}" data-bs-toggle="tooltip" title="Delete" aria-label="Delete ${label}"><i class="bi bi-trash3" aria-hidden="true"></i></button>` +
            `</div></td></tr>`;
    }

    function hasActiveFilters() {
        return Boolean(filterStudent.value || filterSubject.value || filterSemester.value ||
            filterSchoolYear.value || currentSearch);
    }

    function emptyHtml() {
        if (hasActiveFilters()) {
            return UI.emptyStateHtml({
                title: 'No grades match these filters',
                text: 'Change or clear the filters to see more records.'
            });
        }
        return UI.emptyStateHtml({
            title: 'No grades posted yet',
            text: 'Post the first one.',
            actionLabel: 'Add Grade'
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
        onRetry: () => (students.length > 0 ? loadGrades() : start())
    });

    function applySearch() {
        const term = currentSearch.toLowerCase();
        const filtered = term
            ? grades.filter(grade => [
                grade.studentName, grade.studentNumber, grade.subjectCode, grade.subjectName,
                grade.remarks, grade.semester, grade.schoolYear, grade.facultyName, String(grade.grade)
            ].some(value => String(value).toLowerCase().includes(term)))
            : grades;

        table.setRows(filtered);

        countLabel.textContent = term
            ? `${filtered.length} of ${grades.length} records`
            : `${grades.length} record${grades.length === 1 ? '' : 's'}`;
    }

    // Filtering by student, subject, semester and school year is done by the API.
    async function loadGrades() {
        const sequence = ++loadSequence;
        table.showSkeleton(6);

        try {
            const result = await Api.get('/api/grades', {
                studentId: filterStudent.value,
                subjectCode: filterSubject.value,
                semester: filterSemester.value,
                schoolYear: filterSchoolYear.value
            });
            if (sequence !== loadSequence) {
                return;
            }

            grades = result;
            rememberSubjects(grades);
            fillSubjects();
            fillSchoolYears();
            applySearch();

            if (lastSavedId !== null) {
                const savedId = lastSavedId;
                table.showRow(row => row.gradeId === savedId);
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
    // Grade input and the server-computed preview
    // ------------------------------------------------------------------
    function readGrade() {
        const raw = gradeInput.value.trim();

        if (raw === '') {
            // A number input reports "" for text it cannot parse, such as "12e".
            if (gradeInput.validity && gradeInput.validity.badInput) {
                return { value: null, error: GRADE_MESSAGE };
            }
            return { value: null, error: null };
        }

        // Digits only: rejects 85.5, -1, 1e2 and similar.
        if (!WHOLE_NUMBER_PATTERN.test(raw)) {
            return { value: null, error: GRADE_MESSAGE };
        }

        const value = Number(raw);
        if (value > 100) {
            return { value: value, error: GRADE_MESSAGE };
        }

        return { value: value, error: null };
    }

    function setPreview(gradeText, remarksHtml, statusText) {
        previewGrade.textContent = gradeText;
        previewRemarks.innerHTML = remarksHtml;
        previewStatus.textContent = statusText;
    }

    function resetPreview() {
        previewSequence += 1;
        previewBox.classList.remove('is-loading');
        setPreview('--', '--', PREVIEW_IDLE_TEXT);
    }

    async function requestPreview() {
        const { value, error } = readGrade();

        if (error) {
            previewSequence += 1;
            previewBox.classList.remove('is-loading');
            UI.setFieldError(form, 'grade', error);
            setPreview('--', '--', 'Fix the grade to see a preview.');
            return;
        }

        if (value === null) {
            resetPreview();
            return;
        }

        const sequence = ++previewSequence;
        previewBox.classList.add('is-loading');
        previewStatus.textContent = 'Asking the server to compute the remark.';

        try {
            const result = await Api.post('/api/grades/preview', { grade: value });
            if (sequence !== previewSequence) {
                return;
            }
            setPreview(
                String(result.grade),
                UI.remarksBadge(result.remarks, true),
                'Preview computed by the server. The saved remark is recomputed by the server when you save.');
        } catch (previewError) {
            if (sequence !== previewSequence) {
                return;
            }
            setPreview('--', '--', previewError.message || 'The preview is not available right now.');
        } finally {
            if (sequence === previewSequence) {
                previewBox.classList.remove('is-loading');
            }
        }
    }

    // The field error is set inside requestPreview, after the debounce, so the
    // live-validation handler (which clears errors on every keystroke) does not hide it.
    gradeInput.addEventListener('input', UI.debounce(requestPreview, 400));

    subjectCodeInput.addEventListener('change', () => {
        const code = subjectCodeInput.value.trim().toUpperCase();
        subjectCodeInput.value = code;
        if (knownSubjects.has(code) && !subjectNameInput.value.trim()) {
            subjectNameInput.value = knownSubjects.get(code);
        }
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
        if (!students.some(student => student.isActive)) {
            UI.toast('There are no active students to grade.', 'info');
            return;
        }

        editingId = null;
        resetForm();
        fillStudentSelect(null);

        modalTitle.textContent = 'Add Grade';
        studentSelect.value = filterStudent.value;
        subjectCodeInput.value = filterSubject.value;
        subjectNameInput.value = filterSubject.value ? (knownSubjects.get(filterSubject.value) || '') : '';
        semesterSelect.value = filterSemester.value;
        schoolYearInput.value = filterSchoolYear.value || defaultSchoolYear();

        modal.show();
    }

    function openEdit(grade) {
        editingId = grade.gradeId;
        resetForm();
        fillStudentSelect(grade.studentId);

        modalTitle.textContent = 'Edit Grade';
        studentSelect.value = String(grade.studentId);
        subjectCodeInput.value = grade.subjectCode;
        subjectNameInput.value = grade.subjectName;
        gradeInput.value = String(grade.grade);
        semesterSelect.value = grade.semester;
        schoolYearInput.value = grade.schoolYear;

        modal.show();
        requestPreview();
    }

    function collect() {
        const errors = {};
        const gradeResult = readGrade();

        // There is no remarks, facultyId or date here: the server sets all of them.
        const payload = {
            studentId: Number(studentSelect.value) || 0,
            subjectCode: subjectCodeInput.value.trim().toUpperCase(),
            subjectName: subjectNameInput.value.trim(),
            grade: gradeResult.value,
            semester: semesterSelect.value,
            schoolYear: schoolYearInput.value.trim()
        };

        if (!payload.studentId) {
            errors.studentId = 'Select a student.';
        }

        if (!payload.subjectCode) {
            errors.subjectCode = 'Subject code is required.';
        } else if (!SUBJECT_CODE_PATTERN.test(payload.subjectCode)) {
            errors.subjectCode = 'Use 2 to 30 letters, numbers, spaces or hyphens.';
        }

        if (!payload.subjectName) {
            errors.subjectName = 'Subject name is required.';
        }

        if (gradeResult.error) {
            errors.grade = gradeResult.error;
        } else if (payload.grade === null) {
            errors.grade = 'Grade is required.';
        }

        if (!payload.semester) {
            errors.semester = 'Select a semester.';
        }

        const match = SCHOOL_YEAR_PATTERN.exec(payload.schoolYear);
        if (!payload.schoolYear) {
            errors.schoolYear = 'School year is required.';
        } else if (!match) {
            errors.schoolYear = 'School year must look like 2026-2027.';
        } else if (Number(match[2]) !== Number(match[1]) + 1) {
            errors.schoolYear = 'Use two consecutive years, for example 2026-2027.';
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

            lastSavedId = saved.gradeId;
            modal.hide();

            UI.toast(
                `Grade ${isEdit ? 'updated' : 'posted'}: ${saved.grade} is ${saved.remarks} (remark computed by the server).`,
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
    // View details (GET /api/grades/{id})
    // ------------------------------------------------------------------
    async function openView(grade) {
        viewingGrade = grade;
        viewBody.innerHTML =
            '<dt><span class="skeleton skeleton-line"></span></dt><dd><span class="skeleton skeleton-line"></span></dd>' +
            '<dt><span class="skeleton skeleton-line"></span></dt><dd><span class="skeleton skeleton-line"></span></dd>' +
            '<dt><span class="skeleton skeleton-line"></span></dt><dd><span class="skeleton skeleton-line"></span></dd>';
        viewModal.show();

        try {
            const fresh = await Api.get(`/api/grades/${grade.gradeId}`);
            viewingGrade = fresh;

            viewBody.innerHTML = UI.detailListHtml([
                ['Grade ID', String(fresh.gradeId)],
                ['Student', `${esc(fresh.studentName)} <span class="cell-sub">${esc(fresh.studentNumber)}</span>`],
                ['Subject', `<span class="code-chip">${esc(fresh.subjectCode)}</span> <span class="cell-sub">${esc(fresh.subjectName)}</span>`],
                ['Grade', String(fresh.grade)],
                ['Remarks', UI.remarksBadge(fresh.remarks)],
                ['Semester', esc(fresh.semester)],
                ['School year', esc(fresh.schoolYear)],
                ['Posted by', esc(fresh.facultyName)],
                ['Date posted', esc(UI.formatDateTime(fresh.datePosted))],
                ['Date modified', esc(UI.formatDateTime(fresh.dateModified))]
            ]);
        } catch (error) {
            viewModal.hide();
            UI.toastError(error, 'Could not load the grade.');
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
    // Delete (always confirmed first)
    // ------------------------------------------------------------------
    async function remove(grade, button) {
        const confirmed = await UI.confirm({
            title: 'Delete this grade?',
            message: `${grade.subjectCode} (${grade.grade}, ${grade.remarks}) for ${grade.studentName}, ` +
                `${grade.semester} ${grade.schoolYear}, will be permanently removed.`,
            confirmText: 'Delete grade'
        });
        if (!confirmed) {
            return;
        }

        if (button) {
            button.disabled = true;
        }

        try {
            await Api.del(`/api/grades/${grade.gradeId}`);
            UI.toast('The grade was deleted.', 'success');
            await loadGrades();
        } catch (error) {
            if (button) {
                button.disabled = false;
            }
            UI.toastError(error, 'Could not delete the grade.');
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
        const grade = grades.find(item => item.gradeId === Number(button.dataset.id));
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

    [filterStudent, filterSubject, filterSemester, filterSchoolYear].forEach(select => {
        select.addEventListener('change', loadGrades);
    });

    clearFiltersButton.addEventListener('click', () => {
        filterStudent.value = '';
        filterSubject.value = '';
        filterSemester.value = '';
        filterSchoolYear.value = '';
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
            await loadStudents();
        } catch (error) {
            table.showError(error.message);
            UI.toastError(error, 'Could not load the student list.');
            return;
        }

        // Optional deep link: /Faculty/Grades?studentId=1
        const requestedStudent = new URLSearchParams(window.location.search).get('studentId');
        if (requestedStudent && students.some(student => String(student.studentId) === requestedStudent)) {
            filterStudent.value = requestedStudent;
        }

        await loadGrades();
    }

    start();
})();