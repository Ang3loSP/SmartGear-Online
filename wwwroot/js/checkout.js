// ================================================
// Checkout Page JavaScript - SmartGear Online
// Handles the order summary updates, billing toggle,
// card input formatting, and submit guard.
// ================================================

(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {
        initCheckout();
    });

    function initCheckout() {
        formatCardInputs();
        updateShipping();
    }

    // Live business rules + discount state rendered by the server (OrderSettings).
    function getTotalsConfig() {
        var el = document.getElementById('totalsConfig');
        if (!el) {
            return { taxRate: 0.08, threshold: 50, standard: 5.99, express: 15.00, discount: 0, freeShip: false };
        }
        return {
            taxRate: parseFloat(el.dataset.tax) || 0.08,
            threshold: parseFloat(el.dataset.threshold) || 50,
            standard: parseFloat(el.dataset.standard) || 5.99,
            express: parseFloat(el.dataset.express) || 15.00,
            discount: parseFloat(el.dataset.discount) || 0,
            freeShip: el.dataset.freeship === 'true'
        };
    }

    // Called from the Standard/Express radio buttons (onchange="updateShipping()")
    window.updateShipping = function () {
        var subtotalEl = document.getElementById('summarySubtotal');
        var shippingEl = document.getElementById('summaryShipping');
        var taxEl = document.getElementById('summaryTax');
        var totalEl = document.getElementById('summaryTotal');

        if (!subtotalEl || !shippingEl || !taxEl || !totalEl) return;

        var subtotal = parseFloat(subtotalEl.textContent.replace(/[R$]/g, '').replace(',', '')) || 0;
        var tax = parseFloat(taxEl.textContent.replace(/[R$]/g, '').replace(',', '')) || 0;

        var cfg = getTotalsConfig();
        var express = document.getElementById('expressShipping');
        var standard = document.getElementById('standardShipping');

        // Same rules as the server (Services/OrderService.cs):
        // Standard = rate under the threshold, FREE at/over it; Express = flat
        // rate; the FREESHIP code makes shipping free regardless of method.
        var shipping = 0;
        if (!cfg.freeShip) {
            if (express && express.checked) {
                shipping = cfg.express;
            } else if (standard && standard.checked && subtotal < cfg.threshold) {
                shipping = cfg.standard;
            }
        }

        if (shipping === 0) {
            shippingEl.textContent = 'FREE';
            shippingEl.classList.add('text-success');
        } else {
            shippingEl.textContent = 'R' + shipping.toFixed(2);
            shippingEl.classList.remove('text-success');
        }

        // FIX: the discount is subtracted here too, otherwise switching shipping
        // methods silently dropped the discount and showed a bogus total.
        var total = subtotal + tax + shipping - cfg.discount;
        totalEl.textContent = total.toFixed(2);

        // Keep the tax read-out truthful even if shipping changes.
        taxEl.textContent = 'R' + tax.toFixed(2);
    };

    // Called from the "same as shipping" checkbox (onchange="toggleBillingAddress()")
    window.toggleBillingAddress = function () {
        var sameAddress = document.getElementById('sameAddress');
        var differentBilling = document.getElementById('differentBillingForm');

        if (!sameAddress || !differentBilling) return;

        if (sameAddress.checked) {
            differentBilling.style.display = 'none';
        } else {
            differentBilling.style.display = 'block';
        }
    };

    // Auto-format the card number as 4-digit groups, and expiry as MM/YY
    function formatCardInputs() {
        var cardInput = document.getElementById('cardNumberInput');
        var expiryInput = document.querySelector('input[name="ExpiryDate"]');

        if (cardInput) {
            cardInput.addEventListener('input', function () {
                var digits = this.value.replace(/\D/g, '').slice(0, 16);
                this.value = digits.replace(/(.{4})/g, '$1 ').trim();
            });
        }

        if (expiryInput) {
            expiryInput.addEventListener('input', function () {
                var digits = this.value.replace(/\D/g, '').slice(0, 4);
                if (digits.length > 2) {
                    this.value = digits.slice(0, 2) + '/' + digits.slice(2);
                } else {
                    this.value = digits;
                }
            });
        }

        var cvvInput = document.getElementById('cvvInput');
        if (cvvInput) {
            cvvInput.addEventListener('input', function () {
                this.value = this.value.replace(/\D/g, '').slice(0, 4);
            });
        }
    }

    // Give the "same as shipping" checkbox an initial sync on load
    document.addEventListener('DOMContentLoaded', function () {
        var sameAddress = document.getElementById('sameAddress');
        if (sameAddress) {
            toggleBillingAddress();
        }
    });

    // Prevent accidental double-submission of the order
    document.addEventListener('submit', function (e) {
        if (e.target && e.target.id === 'checkoutForm') {
            // SECURITY: never submit the full PAN. Copy only the last four
            // digits of the card into the hidden CardNumberLast4 field so the
            // server model (and any logs) only ever see that fragment.
            var cardInput = document.getElementById('cardNumberInput');
            var last4Field = document.getElementById('cardNumberLast4');
            if (cardInput && last4Field) {
                var digits = cardInput.value.replace(/\D/g, '');
                last4Field.value = digits ? digits.slice(-4) : '';
            }

            var btn = document.getElementById('placeOrderBtn');
            if (btn && btn.dataset.submitted) {
                e.preventDefault();
                return;
            }
            if (btn) {
                btn.dataset.submitted = 'true';
                btn.disabled = true;
                btn.innerHTML = '<i class="fas fa-spinner fa-spin me-2"></i> Processing...';
            }
        }
    }, true);
})();