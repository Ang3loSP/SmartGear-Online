// ================================================
// QUESTION 7 &amp; 8: CLIENT-SIDE FUNCTIONALITY
// ================================================

$(document).ready(function () {
    $('[data-bs-toggle="tooltip"]').tooltip();
    $('[data-bs-toggle="popover"]').popover();

    // Auto-hide alerts after 5 seconds
    setTimeout(function () {
        $('.alert').fadeOut('slow');
    }, 5000);

    // Update cart count via AJAX
    updateCartCount();

    // Fade-in animation for main content
    $('.main-content').addClass('fade-in');
});

// ================================================
// Cart Functions
// ================================================

// Single authoritative cart count updater.
// The badge ID is "cartCountBadge" — matches _Navigation.cshtml.
function updateCartCount() {
    $.ajax({
        url: '/Cart/GetCartCount',
        type: 'GET',
        success: function (data) {
            var badge = $('#cartCountBadge');
            if (data > 0) {
                badge.text(data).show();
            } else {
                badge.hide();
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
    if ($('#toastContainer').length === 0) {
        $('body').append('<div id="toastContainer" style="position: fixed; bottom: 20px; right: 20px; z-index: 9999;"></div>');
    }

    var toastColor = type === 'success' ? 'bg-success' : (type === 'error' ? 'bg-danger' : 'bg-info');

    var toastHtml = '<div class="toast align-items-center text-white ' + toastColor + ' border-0 mb-2" role="alert" aria-live="assertive" aria-atomic="true" data-bs-autohide="true" data-bs-delay="3000">'
        + '<div class="d-flex">'
        + '<div class="toast-body"><strong>' + title + '</strong> - ' + message + '</div>'
        + '<button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>'
        + '</div></div>';

    $('#toastContainer').append(toastHtml);
    var toastElement = $('#toastContainer .toast:last');
    var toast = new bootstrap.Toast(toastElement);
    toast.show();

    toastElement.on('hidden.bs.toast', function () {
        $(this).remove();
    });
}

// ================================================
// Customization Preview
// ================================================

function updateProductPreview(color, logoUrl, customText) {
    if (color) {
        $('#previewImage').css('filter', 'drop-shadow(0 0 5px ' + color + ')');
    }
    if (logoUrl) {
        $('#previewLogo').attr('src', logoUrl).show();
    }
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
