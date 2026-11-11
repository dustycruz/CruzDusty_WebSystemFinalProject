'use strict';

(() => {
    const session = Auth.requireRole('Faculty');
    if (!session) {
        return;
    }

    const esc = UI.escapeHtml;

    const statStudents = document.getElementById('stat-students');
    const statSubjects = document.getElementById('stat-subjects');
    const statGrades = document.getElementById('stat-grades');
    const statPassing = document.getElementById('stat-passing');
    const statPassingHint = document.getElementById('stat-passing-hint');
    const chartWrap = document.getElementById('chart-wrap');
    const chartCanvas = document.getElementById('remarks-chart');
    const chartLegend = document.getElementById('chart-legend');
    const chartEmpty = document.getElementById('chart-empty');
    const recentList = document.getElementById('recent-grades');

    let chart = null;
    let counts = { passed: 0, failed: 0, incomplete: 0 };

    function renderLegend() {
        chartLegend.innerHTML =
            `<li><span class="legend-dot legend-passed" aria-hidden="true"></span>Passed <strong>${counts.passed}</strong></li>` +
            `<li><span class="legend-dot legend-failed" aria-hidden="true"></span>Failed <strong>${counts.failed}</strong></li>` +
            `<li><span class="legend-dot legend-incomplete" aria-hidden="true"></span>Incomplete <strong>${counts.incomplete}</strong></li>`;
    }

    function renderChart() {
        const total = counts.passed + counts.failed + counts.incomplete;

        if (chart) {
            chart.destroy();
            chart = null;
        }

        if (total === 0) {
            chartWrap.hidden = true;
            chartLegend.hidden = true;
            chartEmpty.hidden = false;
            return;
        }

        chartEmpty.hidden = true;
        chartLegend.hidden = false;
        renderLegend();

        // If the Chart.js CDN is blocked, the legend above still shows the numbers.
        if (typeof Chart === 'undefined') {
            chartWrap.hidden = true;
            return;
        }

        chartWrap.hidden = false;

        const colors = [UI.cssVar('--ac-success'), UI.cssVar('--ac-red'), UI.cssVar('--ac-sun')];

        chart = new Chart(chartCanvas, {
            type: 'doughnut',
            data: {
                labels: ['Passed', 'Failed', 'Incomplete'],
                datasets: [{
                    data: [counts.passed, counts.failed, counts.incomplete],
                    backgroundColor: colors,
                    hoverBackgroundColor: colors,
                    borderColor: UI.resolveColor('--panel'),
                    borderWidth: 3,
                    hoverOffset: 6
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                cutout: '62%',
                animation: { duration: UI.prefersReducedMotion() ? 0 : 900 },
                plugins: {
                    legend: { display: false }
                }
            }
        });
    }

    function renderRecent(grades) {
        if (grades.length === 0) {
            recentList.innerHTML = `<li>${UI.emptyStateHtml({
                title: 'No grade records yet',
                text: 'Add the first one from the Grade Records page.'
            })}</li>`;
            return;
        }

        const timeOf = grade => new Date(grade.updatedAt || grade.createdAt).getTime() || 0;
        const recent = grades.slice().sort((a, b) => timeOf(b) - timeOf(a)).slice(0, 6);

        recentList.innerHTML = recent.map((grade, index) =>
            `<li class="recent-item row-enter" style="--i:${index}">` +
            `<div class="recent-main">` +
            `<span class="recent-name">${esc(grade.studentName)}</span>` +
            `<span class="recent-sub">${esc(grade.subjectCode)} \u2022 ${esc(grade.schoolYear)} \u2022 ${esc(grade.semester)}</span>` +
            `</div>` +
            `<div class="recent-side">` +
            `<span class="recent-grade">${UI.formatScore(grade.finalGrade)}</span>` +
            `${UI.remarksBadge(grade.remarks)}` +
            `</div></li>`).join('');
    }

    function showRecentSkeleton() {
        let html = '';
        for (let i = 0; i < 5; i += 1) {
            html += '<li class="recent-item"><span class="skeleton skeleton-line"></span></li>';
        }
        recentList.innerHTML = html;
    }

    function showLoadError(message) {
        [statStudents, statSubjects, statGrades, statPassing].forEach(element => {
            element.textContent = '--';
        });
        chartWrap.hidden = true;
        chartLegend.hidden = true;
        recentList.innerHTML = `<li>${UI.emptyStateHtml({
            title: 'Could not load the dashboard',
            text: message,
            retry: true
        })}</li>`;
    }

    async function load() {
        showRecentSkeleton();

        try {
            const [students, subjects, grades] = await Promise.all([
                Api.get('/api/students'),
                Api.get('/api/subjects'),
                Api.get('/api/grades')
            ]);

            counts = {
                passed: grades.filter(grade => grade.remarks === 'Passed').length,
                failed: grades.filter(grade => grade.remarks === 'Failed').length,
                incomplete: grades.filter(grade => grade.remarks === 'Incomplete').length
            };

            const completed = counts.passed + counts.failed;
            const passingRate = completed > 0 ? (counts.passed / completed) * 100 : 0;

            UI.countUp(statStudents, students.length);
            UI.countUp(statSubjects, subjects.length);
            UI.countUp(statGrades, grades.length);
            UI.countUp(statPassing, passingRate, { decimals: 1, suffix: '%' });

            statPassingHint.textContent = completed > 0
                ? `${counts.passed} of ${completed} completed records`
                : 'No completed records yet';

            renderChart();
            renderRecent(grades);
        } catch (error) {
            showLoadError(error.message);
            UI.toastError(error, 'Could not load the dashboard.');
        }
    }

    recentList.addEventListener('click', event => {
        if (event.target.closest('[data-retry]')) {
            load();
        }
    });

    // Redraw so the ring border matches the new panel colour.
    document.addEventListener('ac:themechange', () => {
        if (chart) {
            renderChart();
        }
    });

    load();
})();