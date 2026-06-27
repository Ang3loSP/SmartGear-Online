// ================================================
// Shopping Cart JavaScript - SmartGear Online
// ================================================

// Update cart totals (subtotal, tax, shipping, total)
function updateCartTotals() {
    let subtotal = 0;

    document.querySelectorAll('.cart-item').forEach(item => {
        const price = parseFloat(item.dataset.price);
        const quantity = parseInt(item.querySelector('.qty-input').value);
        const lineTotal = price * quantity;

        item.querySelector('.line-total').textContent = lineTotal.toFixed(2);
        subtotal += lineTotal;
    });

    const tax = subtotal * 0.08;

    let discount = 0;
    const discountRow = document.getElementById('discountRow');
    if (discountRow && discountRow.style.display !== 'none') {
        const discountText = document.getElementById('discountAmount').textContent;
        discount = parseFloat(discountText.replace('$', '').replace('-', '')) || 0;
    }

    let shipping = 0;
    const shippingElement = document.getElementById('shipping');
    if (subtotal < 50 && subtotal > 0) {
        shipping = 5.99;
        shippingElement.innerHTML = '$5.99';
        shippingElement.classList.remove('text-success');
    } else {
        shippingElement.innerHTML = 'FREE';
        shippingElement.classList.add('text-success');
    }

    const total = subtotal + tax + shipping - discount;

    document.getElementById('subtotal').textContent = '$' + subtotal.toFixed(2);
    document.getElementById('tax').textContent = '$' + tax.toFixed(2);
    document.getElementById('total').textContent = total.toFixed(2);
}

// Update quantity via AJAX
function updateQuantity(itemId, newQuantity) {
    fetch('/Cart/UpdateQuantity', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
            'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
        },
        body: 'cartItemId=' + itemId + '&quantity=' + newQuantity
    })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                updateCartTotals();
                if (typeof updateCartCount === 'function') {
                    updateCartCount();
                }
            } else {
                // FIX: was location.reload() — a full page reload here just to
                // report a failed quantity update was the main thing making
                // this page feel rougher than the rest of the site (every
                // other page only goes through the load + fade-in once).
                // Just tell the user & leave the page as it is.
                alert(data.message || 'Failed to update quantity');
            }
        })
        .catch(error => {
            console.error('Error updating quantity:', error);
        });
}

// Remove item from cart
function removeItem(itemId) {
    if (!confirm('Are you sure you want to remove this item from your cart?')) {
        return;
    }

    const itemRow = document.querySelector('.cart-item[data-item-id="' + itemId + '"]');
    if (itemRow) {
        itemRow.classList.add('cart-item-removing');
    }

    fetch('/Cart/RemoveFromCart', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
            'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
        },
        body: 'cartItemId=' + itemId
    })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                // FIX: was location.reload() — that forced a full page reload
                // (white flash, scroll reset, the site-wide fade-in restarting)
                // every single time, which is why this page felt rougher than
                // the rest of the site. Now we just remove the row once the
                // CSS fade/slide-out transition finishes & recalculate totals,
                // exactly like updateQuantity already did correctly.
                setTimeout(() => {
                    const remainingItems = document.querySelectorAll('.cart-item').length;

                    if (remainingItems <= 1) {
                        // Last item removed - the page needs to switch to the
                        // server-rendered "Your Cart is Empty" state, so a
                        // reload is the right call here, not a workaround.
                        location.reload();
                        return;
                    }

                    if (itemRow) {
                        itemRow.remove();
                    }
                    updateCartTotals();
                    if (typeof updateCartCount === 'function') {
                        updateCartCount();
                    }
                }, 300);
            } else {
                alert('Failed to remove item');
                if (itemRow) itemRow.classList.remove('cart-item-removing');
            }
        })
        .catch(error => {
            console.error('Error removing item:', error);
            if (itemRow) itemRow.classList.remove('cart-item-removing');
        });
}

// Apply discount code — validated server-side, no codes in JS
function applyDiscount() {
    const code = document.getElementById('discountCode').value.trim();

    if (!code) {
        alert('Please enter a discount code');
        return;
    }

    const btn = document.getElementById('applyDiscountBtn');
    btn.disabled = true;
    btn.innerHTML = '<i class="fas fa-spinner fa-spin me-1"></i> Checking...';

    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';

    fetch('/Cart/ApplyDiscount', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
            'RequestVerificationToken': token
        },
        body: 'discountCode=' + encodeURIComponent(code)
    })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                if (data.discountType === 'FreeShipping') {
                    const shippingEl = document.getElementById('shipping');
                    shippingEl.innerHTML = 'FREE';
                    shippingEl.classList.add('text-success');
                } else {
                    document.getElementById('discountAmount').textContent =
                        '-$' + parseFloat(data.discountAmount).toFixed(2);
                    document.getElementById('discountRow').style.display = 'flex';
                }

                updateCartTotals();

                btn.innerHTML = '<i class="fas fa-check me-1"></i> Applied';
            } else {
                alert(data.message || 'Invalid discount code');
                btn.disabled = false;
                btn.innerHTML = 'Apply';
            }
        })
        .catch(error => {
            console.error('Error applying discount:', error);
            btn.disabled = false;
            btn.innerHTML = 'Apply';
        });
}

// Initialise all event listeners
function initCartPage() {
    updateCartTotals();

    document.querySelectorAll('.qty-increase').forEach(btn => {
        btn.addEventListener('click', function () {
            const input = this.closest('.quantity-control').querySelector('.qty-input');
            const currentValue = parseInt(input.value);
            const maxValue = parseInt(input.getAttribute('max')) || 100;
            if (currentValue < maxValue) {
                const newValue = currentValue + 1;
                input.value = newValue;
                updateQuantity(input.dataset.itemId, newValue);
            }
        });
    });

    document.querySelectorAll('.qty-decrease').forEach(btn => {
        btn.addEventListener('click', function () {
            const input = this.closest('.quantity-control').querySelector('.qty-input');
            const currentValue = parseInt(input.value);
            if (currentValue > 1) {
                const newValue = currentValue - 1;
                input.value = newValue;
                updateQuantity(input.dataset.itemId, newValue);
            }
        });
    });

    document.querySelectorAll('.qty-input').forEach(input => {
        input.addEventListener('change', function () {
            let value = parseInt(this.value);
            const minValue = parseInt(this.getAttribute('min')) || 1;
            const maxValue = parseInt(this.getAttribute('max')) || 100;

            if (isNaN(value) || value < minValue) {
                value = minValue;
                this.value = minValue;
            } else if (value > maxValue) {
                value = maxValue;
                this.value = maxValue;
            }

            updateQuantity(this.dataset.itemId, value);
        });
    });

    document.querySelectorAll('.remove-item-btn').forEach(btn => {
        btn.addEventListener('click', function () {
            removeItem(this.dataset.itemId);
        });
    });

    const applyBtn = document.getElementById('applyDiscountBtn');
    if (applyBtn) {
        applyBtn.addEventListener('click', applyDiscount);
    }
}

document.addEventListener('DOMContentLoaded', initCartPage);