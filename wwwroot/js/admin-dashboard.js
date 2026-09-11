// ================================================
// Admin Dashboard JavaScript - SmartGear Online
// Renders the revenue chart from server data and
// manages the period filter buttons.
// ================================================

(function () {
    'use strict';

    var revenueChart = null;

    document.addEventListener('DOMContentLoaded', function () {
        initRevenueChart();
    });

    // Called from the period filter buttons (onclick="filterByPeriod('month')")
    window.filterByPeriod = function (period) {
        var buttons = document.querySelectorAll('.btn-group .btn');
        buttons.forEach(function (btn) {
            btn.classList.remove('active');
        });

        var labels = { 'month': 'month', 'quarter': 'quarter', 'year': 'year' };
        var activeKey = labels[period] || 'month';

        var target = null;
        buttons.forEach(function (btn) {
            if (btn.onclick && btn.onclick.toString().indexOf("'" + activeKey + "'") !== -1) {
                target = btn;
            }
        });

        if (!target) {
            // Fallback: the button carrying the matching label text
            target = Array.prototype.find.call(buttons, function (btn) {
                return btn.textContent.trim().toLowerCase().indexOf(activeKey) !== -1;
            });
        }

        if (target) {
            target.classList.add('active');
        }

        var headerLabel = document.querySelector('.card-header h5 i.fa-chart-bar');
        var chartTitle = headerLabel ? headerLabel.closest('h5') : null;
        if (chartTitle) {
            var title = period === 'quarter'
                ? 'Revenue This Quarter'
                : period === 'year'
                    ? 'Revenue This Year'
                    : 'Revenue This Month';
            chartTitle.textContent = title.replace('Revenue ', '');
            chartTitle.innerHTML = '<i class="fas fa-chart-bar me-2"></i> ' + title;
        }
    };

    function initRevenueChart() {
        var canvas = document.getElementById('revenueChart');
        if (!canvas || typeof Chart === 'undefined') return;

        var data = window.__dailyRevenue || [];

        var labels = data.map(function (d) { return d.dateLabel; });
        var values = data.map(function (d) { return parseFloat(d.revenue) || 0; });
        var orderCounts = data.map(function (d) { return d.orderCount || 0; });

        revenueChart = new Chart(canvas.getContext('2d'), {
            type: 'bar',
            data: {
                labels: labels,
                datasets: [
                    {
                        label: 'Revenue (R)',
                        data: values,
                        backgroundColor: 'rgba(13, 110, 253, 0.7)',
                        borderColor: 'rgba(13, 110, 253, 1)',
                        borderWidth: 1,
                        yAxisID: 'y'
                    },
                    {
                        label: 'Orders',
                        data: orderCounts,
                        type: 'line',
                        borderColor: 'rgba(25, 135, 84, 1)',
                        backgroundColor: 'rgba(25, 135, 84, 0.15)',
                        pointBackgroundColor: 'rgba(25, 135, 84, 1)',
                        tension: 0.3,
                        yAxisID: 'y1'
                    }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                interaction: {
                    mode: 'index',
                    intersect: false
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        title: { display: true, text: 'Revenue (R)' }
                    },
                    y1: {
                        beginAtZero: true,
                        position: 'right',
                        grid: { drawOnChartArea: false },
                        title: { display: true, text: 'Orders' }
                    }
                },
                plugins: {
                    legend: { display: true, position: 'bottom' }
                }
            }
        });
    }
})();