'use strict';

(() => {
    const session = Auth.requireRole('Student');
    if (!session) {
        return;
    }

    const esc = UI.escapeHtml;
    const ALL_TERMS = 'all';
    const YEAR_LABELS = ['', '1st Year', '2nd Year', '3rd Year', '4th Year', '5th Year', '6th Year'];

    const welcomeInitials = document.getElementById('welcome-initials');
    const welcomeName = document.getElementById('welcome-name');
    const welcomeNumber = document.getElementById('welcome-number');
    const welcomeCourse = document.getElementById('welcome-course');
    const printButton = document.getElementById('btn-print');
    const printName = document.getElementById('print-name');
    const printNumber = document.getElementById('print-number');
    const printTerm = document.getElementById('print-term');
    const printDate = document.getElementById('print-date');

    const sumGwa = document.getElementById('sum-gwa');
    const sumGwaHint = document.getElementById('sum-gwa-hint');
    const sumPassed = document.getElementById('sum-passed');
    const sumFailed = document.getElementById('sum-failed');
    const sumIncomplete = document.getElementById('sum-incomplete');

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
        const year = YEAR_LABELS[student.yearLevel] || `Year ${student.yearLevel}`;

        welcomeInitials.textContent = UI.initials(student.fullName);
        welcomeName.textContent = student.fullName;
        welcomeNumber.textContent = `Student No. ${student.studentNumber}`;
        welcomeCourse.textContent = `${student.course} \u2022 ${year}`;

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
        const passed = grades.filter(grade => grade.remarks === 'Passed').length;
        const failed = grades.filter(grade => grade.remarks === 'Failed').length;
        const incomplete = grades.filter(grade => grade.remarks === 'Incomplete').length;

        // General weighted average of the server-computed final grades, weighted by units.
        const completed = grades.filter(grade => grade.finalGrade !== null && grade.finalGrade !== undefined);
        const totalUnits = completed.reduce((sum, grade) => sum + grade.units, 0);
        const weightedSum = completed.reduce((sum, grade) => sum + (grade.finalGrade * grade.units), 0);

        if (totalUnits > 0) {
            UI.countUp(sumGwa, weightedSum / totalUnits, { decimals: 2 });
            sumGwaHint.textContent = `Across ${completed.length} completed subject${completed.length === 1 ? '' : 's'}, ${totalUnits} units`;
        } else {
            sumGwa.textContent = '--';
            sumGwaHint.textContent = 'No completed subjects yet';
        }

        UI.countUp(sumPassed, passed);
        UI.countUp(sumFailed, failed);
        UI.countUp(sumIncomplete, incomplete);
    }

    function renderTable(grades) {
        tableBody.removeAttribute('aria-busy');
        tableBody.innerHTML = grades.map((grade, index) =>
            `<tr class="row-enter" style="--i:${index}">` +
            `<td><span class="code-chip">${esc(grade.subjectCode)}</span></td>` +
            `<td><span class="cell-main">${esc(grade.subjectName)}</span></td>` +
            `<td class="col-num">${grade.units}</td>` +
            `<td>${esc(grade.schoolYear)}</td>` +
            `<td>${esc(grade.semester)}</td>` +
            `<td class="col-num">${UI.formatScore(grade.prelim)}</td>` +
            `<td class="col-num">${UI.formatScore(grade.midterm)}</td>` +
            `<td class="col-num">${UI.formatScore(grade.finals)}</td>` +
            `<td class="col-num"><strong>${UI.formatScore(grade.finalGrade)}</strong></td>` +
            `<td>${UI.remarksBadge(grade.remarks)}</td>` +
            `</tr>`).join('');
    }

    function renderCards(grades) {
        cardsWrap.innerHTML = grades.map((grade, index) =>
            `<article class="grade-card row-enter" style="--i:${index}">` +
            `<div class="grade-card-head">` +
            `<div><span class="grade-card-title">${esc(grade.subjectCode)} \u2022 ${esc(grade.subjectName)}</span>` +
            `<span class="grade-card-term">${esc(grade.schoolYear)} \u2022 ${esc(grade.semester)} \u2022 ${grade.units} units</span></div>` +
            `${UI.remarksBadge(grade.remarks)}` +
            `</div>` +
            `<div class="grade-card-scores">` +
            `<div><span>Prelim</span><strong>${UI.formatScore(grade.prelim)}</strong></div>` +
            `<div><span>Midterm</span><strong>${UI.formatScore(grade.midterm)}</strong></div>` +
            `<div><span>Finals</span><strong>${UI.formatScore(grade.finals)}</strong></div>` +
            `<div><span>Final</span><strong>${UI.formatScore(grade.finalGrade)}</strong></div>` +
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

        if (!empty) {
            renderTable(grades);
            renderCards(grades);
        }

        countLabel.textContent = `${grades.length} record${grades.length === 1 ? '' : 's'}`;
        printTerm.textContent = termLabel(activeTerm);
    }

    function showSkeleton() {
        UI.skeletonRows(tableBody, 10, 4);
        let cards = '';
        for (let i = 0; i < 3; i += 1) {
            cards += '<div class="grade-card"><span class="skeleton skeleton-line"></span>' +
                '<span class="skeleton skeleton-line mt-2"></span></div>';
        }
        cardsWrap.innerHTML = cards;
    }

    function showLoadError(message) {
        [sumGwa, sumPassed, sumFailed, sumIncomplete].forEach(element => {
            element.textContent = '--';
        });
        welcomeName.textContent = session.fullName;
        welcomeInitials.textContent = UI.initials(session.fullName);
        termTabs.innerHTML = '';
        cardsWrap.innerHTML = '';
        tableBody.removeAttribute('aria-busy');
        tableBody.innerHTML = `<tr class="empty-row"><td colspan="10">${UI.emptyStateHtml({
            title: 'Could not load your grades',
            text: message,
            retry: true
        })}</td></tr>`;
        cardsWrap.innerHTML = UI.emptyStateHtml({
            title: 'Could not load your grades',
            text: message,
            retry: true
        });
    }

    async function load() {
        showSkeleton();

        try {
            // No id is sent. The API reads the student id from the signed token.
            const data = await Api.get('/api/grades/me');

            allGrades = data.grades || [];
            activeTerm = ALL_TERMS;

            renderProfile(data.student);
            renderTabs();
            render();
        } catch (error) {
            showLoadError(error.message);
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