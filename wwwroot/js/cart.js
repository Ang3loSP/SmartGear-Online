// ================================================
// Shopping Cart JavaScript - SmartGear Online
// ================================================

// Update cart totals (subtotal, tax, shipping, total)
function updateCartTotals() {
    let subtotal = 0;

    // Calculate subtotal from all cart items
    document.querySelectorAll('.cart-item').forEach(item => {
        const price = parseFloat(item.dataset.price);
        const quantity = parseInt(item.querySelector('.qty-input').value);
        const lineTotal = price * quantity;

        item.querySelector('.line-total').textContent = lineTotal.toFixed(2);
        subtotal += lineTotal;
    });

    // Calculate tax (8%)
    const tax = subtotal * 0.08;

    // Get discount if applied
    let discount = 0;
    const discountRow = document.getElementById('discountRow');
    if (discountRow && discountRow.style.display !== 'none') {
        const discountText = document.getElementById('discountAmount').textContent;
        discount = parseFloat(discountText.replace('$', '').replace('-', '')) || 0;
    }

    // Calculate shipping (free over $50)
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

    // Calculate total
    const total = subtotal + tax + shipping - discount;

    // Update display
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
            } else {
                location.reload();
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

    const itemRow = document.querySelector(`.cart-item[data-item-id="${itemId}"]`);
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
                setTimeout(() => {
                    location.reload();
                }, 300);
            } else {
                alert('Failed to remove item');
                if (itemRow) {
                    itemRow.classList.remove('cart-item-removing');
                }
            }
        })
        .catch(error => {
            console.error('Error removing item:', error);
            if (itemRow) {
                itemRow.classList.remove('cart-item-removing');
            }
        });
}

// Apply discount code
function applyDiscount() {
    const code = document.getElementById('discountCode').value.trim();

    if (!code) {
        alert('Please enter a discount code');
        return;
    }

    const subtotalText = document.getElementById('subtotal').textContent;
    const subtotal = parseFloat(subtotalText.replace('$', ''));

    // Discount codes (in real app, this would be server-side validation)
    const validCodes = {
        'SAVE10': 0.10,
        'SAVE20': 0.20,
        'FIRST25': 0.25,
        'FREESHIP': 'shipping'
    };

    if (validCodes[code]) {
        if (validCodes[code] === 'shipping') {
            document.getElementById('shipping').innerHTML = 'FREE';
            document.getElementById('shipping').classList.add('text-success');
            alert('Free shipping applied!');
        } else {
            const discountAmount = subtotal * validCodes[code];
            document.getElementById('discountAmount').textContent = '-$' + discountAmount.toFixed(2);
            document.getElementById('discountRow').style.display = 'flex';
            alert('Discount applied successfully!');
        }
        updateCartTotals();

        const btn = document.getElementById('applyDiscountBtn');
        btn.disabled = true;
        btn.innerHTML = '<i class="fas fa-check me-1"></i> Applied';
    } else {
        alert('Invalid discount code');
    }
}

// Initialize all event listeners
function initCartPage() {
    updateCartTotals();

    // Quantity increase buttons
    document.querySelectorAll('.qty-increase').forEach(btn => {
        btn.addEventListener('click', function () {
            const input = this.closest('.quantity-control').querySelector('.qty-input');
            const currentValue = parseInt(input.value);
            const maxValue = parseInt(input.getAttribute('max')) || 100;
            if (currentValue < maxValue) {
                const newValue = currentValue + 1;
                input.value = newValue;
                const itemId = input.dataset.itemId;
                updateQuantity(itemId, newValue);
            }
        });
    });

    // Quantity decrease buttons
    document.querySelectorAll('.qty-decrease').forEach(btn => {
        btn.addEventListener('click', function () {
            const input = this.closest('.quantity-control').querySelector('.qty-input');
            const currentValue = parseInt(input.value);
            if (currentValue > 1) {
                const newValue = currentValue - 1;
                input.value = newValue;
                const itemId = input.dataset.itemId;
                updateQuantity(itemId, newValue);
            }
        });
    });

    // Manual quantity input change
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

            const itemId = this.dataset.itemId;
            updateQuantity(itemId, value);
        });
    });

    // Remove item buttons
    document.querySelectorAll('.remove-item-btn').forEach(btn => {
        btn.addEventListener('click', function () {
            const itemId = this.dataset.itemId;
            removeItem(itemId);
        });
    });

    // Apply discount button
    const applyBtn = document.getElementById('applyDiscountBtn');
    if (applyBtn) {
        applyBtn.addEventListener('click', applyDiscount);
    }
}

// Run initialization when DOM is ready
document.addEventListener('DOMContentLoaded', initCartPage);