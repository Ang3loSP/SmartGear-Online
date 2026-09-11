// ================================================
// Order Tracking JavaScript - SmartGear Online
// Adds polish to the tracking timeline: entrance
// animation for the current/active step and a
// print-friendly action for the Print button.
// ================================================

(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {
        animateCurrentStep();
        wirePrintButton();
    });

    // Subtle fade-in for each timeline marker, staggered by position.
    // The "current" step gets an extra highlight pop.
    function animateCurrentStep() {
        var markers = document.querySelectorAll('.timeline-marker');
        markers.forEach(function (marker, index) {
            setTimeout(function () {
                marker.style.transition = 'transform 0.3s ease, box-shadow 0.3s ease';
                marker.style.transform = 'scale(1.1)';
                marker.style.boxShadow = '0 0 0 4px rgba(13, 110, 253, 0.2)';

                setTimeout(function () {
                    marker.style.transform = '';
                    marker.style.boxShadow = '';
                }, 350);
            }, index * 150);
        });
    }

    // The view's inline Print button calls window.print() already;
    // this ensures the print stylesheet applies predictably and
    // that printing is not blocked by hovering animation states.
    function wirePrintButton() {
        var printBtn = document.querySelector('button.btn-outline-primary');
        if (!printBtn) return;

        printBtn.addEventListener('click', function () {
            document.body.classList.add('printing');
        });

        window.addEventListener('afterprint', function () {
            document.body.classList.remove('printing');
        });
    }
})();