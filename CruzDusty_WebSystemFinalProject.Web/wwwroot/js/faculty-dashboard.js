'use strict';

(() => {
    const session = Auth.requireRole('Faculty');
    if (!session) {
        return;
    }

    const esc = UI.escapeHtml;

    const statGrades = document.getElementById('stat-grades');
    const statStudents = document.getElementById('stat-students');
    const statPassing = document.getElementById('stat-passing');
    const statPassingHint = document.getElementById('stat-passing-hint');
    const statAverage = document.getElementById('stat-average');
    const chartWrap = document.getElementById('chart-wrap');
    const chartCanvas = document.getElementById('remarks-chart');
    const chartLegend = document.getElementById('chart-legend');
    const chartEmpty = document.getElementById('chart-empty');
    const recentList = document.getElementById('recent-grades');

    let chart = null;
    let counts = {};
    let total = 0;

    function renderLegend() {
        chartLegend.innerHTML = UI.remarkOrder.map(remark =>
            `<li><span class="legend-dot" style="background: var(${UI.remarkStyle(remark).chartVar})" aria-hidden="true"></span>` +
            `${esc(remark)} <strong>${counts[remark] || 0}</strong></li>`).join('');
    }

    function renderChart() {
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

        // If the Chart.js CDN is blocked, the legend above still shows every count.
        if (typeof Chart === 'undefined') {
            chartWrap.hidden = true;
            return;
        }

        chartWrap.hidden = false;

        const colors = UI.remarkOrder.map(remark => UI.cssVar(UI.remarkStyle(remark).chartVar));
        const textColor = UI.resolveColor('--text-muted');
        const gridColor = UI.resolveColor('--border');

        chart = new Chart(chartCanvas, {
            type: 'bar',
            data: {
                labels: UI.remarkOrder,
                datasets: [{
                    label: 'Grade records',
                    data: UI.remarkOrder.map(remark => counts[remark] || 0),
                    backgroundColor: colors,
                    hoverBackgroundColor: colors,
                    borderRadius: 6,
                    maxBarThickness: 48
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                animation: { duration: UI.prefersReducedMotion() ? 0 : 900 },
                plugins: {
                    legend: { display: false }
                },
                scales: {
                    x: {
                        ticks: { color: textColor },
                        grid: { display: false }
                    },
                    y: {
                        beginAtZero: true,
                        ticks: { color: textColor, precision: 0 },
                        grid: { color: gridColor }
                    }
                }
            }
        });
    }

    function renderRecent(grades) {
        if (grades.length === 0) {
            recentList.innerHTML = `<li>${UI.emptyStateHtml({
                title: 'No grades posted yet',
                text: 'Post the first one from the Grade Records page.'
            })}</li>`;
            return;
        }

        const timeOf = grade => new Date(grade.dateModified || grade.datePosted).getTime() || 0;
        const recent = grades.slice().sort((a, b) => timeOf(b) - timeOf(a)).slice(0, 6);

        recentList.innerHTML = recent.map((grade, index) =>
            `<li class="recent-item row-enter" style="--i:${index}">` +
            `<div class="recent-main">` +
            `<span class="recent-name">${esc(grade.studentName)}</span>` +
            `<span class="recent-sub">${esc(grade.subjectCode)} \u2022 ${esc(grade.semester)} \u2022 ${esc(grade.schoolYear)}</span>` +
            `</div>` +
            `<div class="recent-side">` +
            `<span class="recent-grade">${Number(grade.grade)}</span>` +
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
        [statGrades, statStudents, statPassing, statAverage].forEach(element => {
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
            const [grades, students] = await Promise.all([
                Api.get('/api/grades'),
                Api.get('/api/students')
            ]);

            total = grades.length;
            counts = {};
            grades.forEach(grade => {
                counts[grade.remarks] = (counts[grade.remarks] || 0) + 1;
            });

            const passing = grades.filter(grade => UI.isPassing(grade.remarks)).length;
            const passingRate = total > 0 ? (passing / total) * 100 : 0;
            const average = total > 0 ? grades.reduce((sum, grade) => sum + grade.grade, 0) / total : 0;

            UI.countUp(statGrades, total);
            UI.countUp(statStudents, students.filter(student => student.isActive).length);
            UI.countUp(statPassing, passingRate, { decimals: 1, suffix: '%' });

            if (total > 0) {
                UI.countUp(statAverage, average, { decimals: 2 });
                statPassingHint.textContent = `${passing} of ${total} grades are 75 or above`;
            } else {
                statAverage.textContent = '--';
                statPassingHint.textContent = 'No grades posted yet';
            }

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

    // Redraw so the axis colours follow the new theme.
    document.addEventListener('ac:themechange', () => {
        if (chart) {
            renderChart();
        }
    });

    load();
})();