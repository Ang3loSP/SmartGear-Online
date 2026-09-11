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

    // Called from the Standard/Express radio buttons (onchange="updateShipping()")
    window.updateShipping = function () {
        var subtotalEl = document.getElementById('summarySubtotal');
        var shippingEl = document.getElementById('summaryShipping');
        var taxEl = document.getElementById('summaryTax');
        var totalEl = document.getElementById('summaryTotal');

        if (!subtotalEl || !shippingEl || !taxEl || !totalEl) return;

        var subtotal = parseFloat(subtotalEl.textContent.replace(/[R$]/g, '').replace(',', '')) || 0;
        var tax = parseFloat(taxEl.textContent.replace(/[R$]/g, '').replace(',', '')) || 0;

        var express = document.getElementById('expressShipping');
        var standard = document.getElementById('standardShipping');

        // Same shipping rule as the server (Services/OrderService.cs):
        // Standard = R5.99 under R50, FREE at R50+; Express = flat R15.00.
        var shipping = 0;
        if (express && express.checked) {
            shipping = 15.00;
        } else if (standard && standard.checked && subtotal < 50) {
            shipping = 5.99;
        }

        if (shipping === 0) {
            shippingEl.textContent = 'FREE';
            shippingEl.classList.add('text-success');
        } else {
            shippingEl.textContent = 'R' + shipping.toFixed(2);
            shippingEl.classList.remove('text-success');
        }

        var total = subtotal + tax + shipping;
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