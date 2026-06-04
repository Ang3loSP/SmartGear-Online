// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
// ================================================
// QUESTION 7 & 8: CLIENT-SIDE FUNCTIONALITY
// ================================================

// Initialize on page load
$(document).ready(function () {
    // Initialize tooltips
    $('[data-bs-toggle="tooltip"]').tooltip();

    // Initialize popovers
    $('[data-bs-toggle="popover"]').popover();

    // Auto-hide alerts after 5 seconds
    setTimeout(function () {
        $('.alert').fadeOut('slow');
    }, 5000);

    // Update cart count via AJAX
    updateCartCount();

    // Add fade-in animation to main content
    $('.main-content').addClass('fade-in');
});

// ================================================
// Shopping Cart Functions
// ================================================

function updateCartCount() {
    $.ajax({
        url: '/Cart/GetCartCount',
        type: 'GET',
        success: function (data) {
            $('#cartCountBadge').text(data);
            if (data > 0) {
                $('#cartCountBadge').show();
            } else {
                $('#cartCountBadge').hide();
            }
        },
        error: function () {
            console.log('Error updating cart count');
        }
    });
}

function addToCart(productId, quantity, customizationId) {
    $.ajax({
        url: '/Cart/AddToCart',
        type: 'POST',
        data: {
            productId: productId,
            quantity: quantity,
            customizationId: customizationId
        },
        success: function (result) {
            if (result.success) {
                // Show success message
                showToast('Success', 'Item added to cart!', 'success');
                updateCartCount();
            } else {
                showToast('Error', result.message, 'error');
            }
        },
        error: function () {
            showToast('Error', 'Failed to add item to cart', 'error');
        }
    });
}

function updateCartItemQuantity(itemId, newQuantity) {
    $.ajax({
        url: '/Cart/UpdateQuantity',
        type: 'POST',
        data: {
            cartItemId: itemId,
            quantity: newQuantity
        },
        success: function (result) {
            if (result.success) {
                location.reload();
            }
        }
    });
}

function removeCartItem(itemId) {
    if (confirm('Are you sure you want to remove this item?')) {
        $.ajax({
            url: '/Cart/RemoveFromCart',
            type: 'POST',
            data: { cartItemId: itemId },
            success: function (result) {
                if (result.success) {
                    location.reload();
                }
            }
        });
    }
}

// ================================================
// Toast Notification System
// ================================================

function showToast(title, message, type) {
    // Create toast container if it doesn't exist
    if ($('#toastContainer').length === 0) {
        $('body').append('<div id="toastContainer" style="position: fixed; bottom: 20px; right: 20px; z-index: 9999;"></div>');
    }

    var toastColor = type === 'success' ? 'bg-success' : (type === 'error' ? 'bg-danger' : 'bg-info');

    var toastHtml = `
        <div class="toast align-items-center text-white ${toastColor} border-0 mb-2" role="alert" aria-live="assertive" aria-atomic="true" data-bs-autohide="true" data-bs-delay="3000">
            <div class="d-flex">
                <div class="toast-body">
                    <strong>${title}</strong> - ${message}
                </div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
            </div>
        </div>
    `;

    $('#toastContainer').append(toastHtml);
    var toastElement = $('#toastContainer .toast:last');
    var toast = new bootstrap.Toast(toastElement);
    toast.show();

    // Remove toast after it's hidden
    toastElement.on('hidden.bs.toast', function () {
        $(this).remove();
    });
}

// ================================================
// Customization Preview Functions
// ================================================

function updateProductPreview(color, logoUrl, customText) {
    // Update color on preview image
    if (color) {
        $('#previewImage').css('filter', `drop-shadow(0 0 5px ${color})`);
    }

    // Update logo on preview
    if (logoUrl) {
        $('#previewLogo').attr('src', logoUrl).show();
    }

    // Update text on preview
    if (customText) {
        $('#previewText').text(customText).show();
    }
}

// ================================================
// Form Validation Helpers
// ================================================

function validateEmail(email) {
    var re = /^[^\s@]+@([^\s@.,]+\.)+[^\s@.,]{2,}$/;
    return re.test(email);
}

function validatePhone(phone) {
    var re = /^[\+]?[(]?[0-9]{3}[)]?[-\s\.]?[0-9]{3}[-\s\.]?[0-9]{4,6}$/;
    return re.test(phone);
}

// ================================================
// Search Autocomplete
// ================================================

$('#searchInput').on('keyup', function () {
    var query = $(this).val();
    if (query.length > 2) {
        $.ajax({
            url: '/Product/SearchSuggestions',
            type: 'GET',
            data: { query: query },
            success: function (data) {
                var suggestions = '';
                data.forEach(function (item) {
                    suggestions += `<div class="suggestion-item p-2 border-bottom">${item}</div>`;
                });
                $('#searchSuggestions').html(suggestions).show();
            }
        });
    } else {
        $('#searchSuggestions').hide();
    }
});

// ================================================
// Price Calculations
// ================================================

function calculateTotalPrice() {
    var subtotal = 0;
    $('.cart-item-price').each(function () {
        subtotal += parseFloat($(this).text());
    });

    var tax = subtotal * 0.08; // 8% tax
    var shipping = subtotal > 50 ? 0 : 5.99;
    var total = subtotal + tax + shipping;

    $('#subtotal').text('$' + subtotal.toFixed(2));
    $('#tax').text('$' + tax.toFixed(2));
    $('#shipping').text('$' + shipping.toFixed(2));
    $('#total').text('$' + total.toFixed(2));
}

// ================================================
// Back to Top Button
// ================================================

$(window).scroll(function () {
    if ($(this).scrollTop() > 300) {
        $('#backToTop').fadeIn();
    } else {
        $('#backToTop').fadeOut();
    }
});

$('body').append('<button id="backToTop" class="btn btn-primary rounded-circle" style="position: fixed; bottom: 20px; right: 20px; display: none; width: 45px; height: 45px;"><i class="fas fa-arrow-up"></i></button>');

$('#backToTop').click(function () {
    $('html, body').animate({ scrollTop: 0 }, 500);
});