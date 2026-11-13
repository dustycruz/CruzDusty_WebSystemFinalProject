'use strict';

(() => {
    const session = Auth.requireRole('Student');
    if (!session) {
        return;
    }

    const esc = UI.escapeHtml;
    const ALL_TERMS = 'all';

    const welcomeInitials = document.getElementById('welcome-initials');
    const welcomeName = document.getElementById('welcome-name');
    const welcomeNumber = document.getElementById('welcome-number');
    const welcomeEmail = document.getElementById('welcome-email');
    const printButton = document.getElementById('btn-print');
    const printName = document.getElementById('print-name');
    const printNumber = document.getElementById('print-number');
    const printTerm = document.getElementById('print-term');
    const printDate = document.getElementById('print-date');

    const sumAverage = document.getElementById('sum-average');
    const sumAverageHint = document.getElementById('sum-average-hint');
    const sumPassed = document.getElementById('sum-passed');
    const sumFailed = document.getElementById('sum-failed');
    const sumTotal = document.getElementById('sum-total');

    const countLabel = document.getElementById('my-grades-count');
    const termTabs = document.getElementById('term-tabs');
    const tableElement = document.getElementById('my-grades-table');
    const tableBody = tableElement.querySelector('tbody');
    const desktopWrap = document.querySelector('.grades-desktop');
    const cardsWrap = document.getElementById('my-grades-cards');
    const emptyState = document.getElementById('my-grades-empty');

    let allGrades = [];
    let activeTerm = ALL_TERMS;

    function termKey(grade) {
        return `${grade.schoolYear}|${grade.semester}`;
    }

    function termLabel(key) {
        if (key === ALL_TERMS) {
            return 'All Terms';
        }
        const parts = key.split('|');
        return `${parts[0]} \u2022 ${parts[1]}`;
    }

    function renderProfile(student) {
        welcomeInitials.textContent = UI.initials(student.fullName);
        welcomeName.textContent = student.fullName;
        welcomeNumber.textContent = `Student No. ${student.studentNumber}`;
        welcomeEmail.textContent = student.email || '';

        printName.textContent = student.fullName;
        printNumber.textContent = student.studentNumber;
    }

    function renderTabs() {
        const keys = [ALL_TERMS];
        allGrades.forEach(grade => {
            const key = termKey(grade);
            if (!keys.includes(key)) {
                keys.push(key);
            }
        });

        termTabs.innerHTML = keys.map(key => {
            const active = key === activeTerm;
            return `<li role="presentation"><button type="button" role="tab" class="term-tab${active ? ' active' : ''}" ` +
                `aria-selected="${active}" data-term="${esc(key)}">${esc(termLabel(key))}</button></li>`;
        }).join('');
    }

    function renderSummary(grades) {
        const passed = grades.filter(grade => UI.isPassing(grade.remarks)).length;
        const failed = grades.length - passed;

        if (grades.length > 0) {
            const average = grades.reduce((sum, grade) => sum + grade.grade, 0) / grades.length;
            UI.countUp(sumAverage, average, { decimals: 2 });
            sumAverageHint.textContent = `Across ${grades.length} subject${grades.length === 1 ? '' : 's'}`;
        } else {
            sumAverage.textContent = '--';
            sumAverageHint.textContent = 'No grades posted yet';
        }

        UI.countUp(sumPassed, passed);
        UI.countUp(sumFailed, failed);
        UI.countUp(sumTotal, grades.length);
    }

    function renderTable(grades) {
        tableBody.removeAttribute('aria-busy');
        tableBody.innerHTML = grades.map((grade, index) =>
            `<tr class="row-enter" style="--i:${index}">` +
            `<td><span class="code-chip">${esc(grade.subjectCode)}</span></td>` +
            `<td><span class="cell-main">${esc(grade.subjectName)}</span></td>` +
            `<td class="col-num"><strong>${Number(grade.grade)}</strong></td>` +
            `<td>${UI.remarksBadge(grade.remarks)}</td>` +
            `<td>${esc(grade.semester)}</td>` +
            `<td>${esc(grade.schoolYear)}</td>` +
            `</tr>`).join('');
    }

    function renderCards(grades) {
        cardsWrap.innerHTML = grades.map((grade, index) =>
            `<article class="grade-card row-enter" style="--i:${index}">` +
            `<div class="grade-card-head">` +
            `<div><span class="grade-card-title">${esc(grade.subjectCode)} \u2022 ${esc(grade.subjectName)}</span>` +
            `<span class="grade-card-term">${esc(grade.semester)} \u2022 ${esc(grade.schoolYear)}</span></div>` +
            `${UI.remarksBadge(grade.remarks)}` +
            `</div>` +
            `<div class="grade-card-scores" style="grid-template-columns: repeat(3, 1fr)">` +
            `<div><span>Grade</span><strong>${Number(grade.grade)}</strong></div>` +
            `<div><span>Semester</span><strong>${esc(grade.semester)}</strong></div>` +
            `<div><span>School Year</span><strong>${esc(grade.schoolYear)}</strong></div>` +
            `</div></article>`).join('');
    }

    function render() {
        const grades = activeTerm === ALL_TERMS
            ? allGrades
            : allGrades.filter(grade => termKey(grade) === activeTerm);

        renderSummary(grades);

        const empty = grades.length === 0;
        emptyState.hidden = !empty;
        desktopWrap.hidden = empty;
        cardsWrap.hidden = empty;
        termTabs.hidden = allGrades.length === 0;

        if (!empty) {
            renderTable(grades);
            renderCards(grades);
        }

        countLabel.textContent = `${grades.length} record${grades.length === 1 ? '' : 's'}`;
        printTerm.textContent = termLabel(activeTerm);
    }

    function showSkeleton() {
        UI.skeletonRows(tableBody, 6, 4);
        let cards = '';
        for (let i = 0; i < 3; i += 1) {
            cards += '<div class="grade-card"><span class="skeleton skeleton-line"></span>' +
                '<span class="skeleton skeleton-line mt-2"></span></div>';
        }
        cardsWrap.innerHTML = cards;
    }

    function describeError(error) {
        if (error.status === 401) {
            return 'Your session has ended. Please sign in again to see your grades.';
        }
        if (error.status === 403) {
            return 'You are not authorized to view these grades. Sign in with your student account.';
        }
        return error.message || 'Your grades could not be loaded. Please try again.';
    }

    function showLoadError(message) {
        [sumAverage, sumPassed, sumFailed, sumTotal].forEach(element => {
            element.textContent = '--';
        });
        welcomeName.textContent = session.fullName;
        welcomeInitials.textContent = UI.initials(session.fullName);
        termTabs.innerHTML = '';
        emptyState.hidden = true;
        desktopWrap.hidden = false;
        cardsWrap.hidden = false;

        const html = UI.emptyStateHtml({
            title: 'Could not load your grades',
            text: message,
            retry: true
        });

        tableBody.removeAttribute('aria-busy');
        tableBody.innerHTML = `<tr class="empty-row"><td colspan="6">${html}</td></tr>`;
        cardsWrap.innerHTML = html;
    }

    async function load() {
        showSkeleton();

        try {
            // No student id is sent. The API reads it from the signed token.
            const data = await Api.get('/api/student/grades');

            allGrades = data.grades || [];
            activeTerm = ALL_TERMS;

            renderProfile(data.student);
            renderTabs();
            render();
        } catch (error) {
            showLoadError(describeError(error));
            UI.toastError(error, 'Could not load your grades.');
        }
    }

    termTabs.addEventListener('click', event => {
        const button = event.target.closest('[data-term]');
        if (!button) {
            return;
        }
        activeTerm = button.dataset.term;
        renderTabs();
        render();

        const active = termTabs.querySelector('.term-tab.active');
        if (active) {
            active.focus();
        }
    });

    document.querySelector('.page-content').addEventListener('click', event => {
        if (event.target.closest('[data-retry]')) {
            load();
        }
    });

    printButton.addEventListener('click', () => {
        printDate.textContent = new Date().toLocaleDateString(undefined, { dateStyle: 'long' });
        UI.toast('Opening the print dialog. Choose "Save as PDF" to keep a copy.', 'info');
        setTimeout(() => window.print(), 250);
    });

    load();
})();