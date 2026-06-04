// Custom validation methods
(function () {
    if (typeof jQuery !== 'undefined') {
        // Custom validation method for strong password
        jQuery.validator.addMethod("strongpassword", function (value, element) {
            if (this.optional(element)) return true;
            var regex = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$/;
            return regex.test(value);
        }, "Password must contain at least 8 characters, one uppercase, one lowercase, one number, and one special character");

        // Custom validation method for South African phone number
        jQuery.validator.addMethod("southafricanphone", function (value, element) {
            if (this.optional(element)) return true;
            var cleanValue = value.replace(/\s/g, '');
            var regex = /^(\+27|0)[6-8][0-9]{8}$/;
            return regex.test(cleanValue);
        }, "Please enter a valid South African phone number");

        // Custom validation method for postal code
        jQuery.validator.addMethod("postalcode", function (value, element) {
            if (this.optional(element)) return true;
            var regex = /^[0-9]{4}$/;
            return regex.test(value);
        }, "Please enter a valid 4-digit postal code");

        // Custom validation method for letters only
        jQuery.validator.addMethod("lettersonly", function (value, element) {
            if (this.optional(element)) return true;
            return /^[a-zA-Z\s'-]+$/.test(value);
        }, "Only letters, spaces, hyphens, and apostrophes are allowed");

        // Re-validate on every keyup
        jQuery.validator.setDefaults({
            onkeyup: function (element) {
                jQuery(element).valid();
            }
        });

        // Add unobtrusive adapters
        if (typeof jQuery.validator.unobtrusive !== 'undefined') {
            jQuery.validator.unobtrusive.adapters.addBool("strongpassword");
            jQuery.validator.unobtrusive.adapters.addBool("southafricanphone");
            jQuery.validator.unobtrusive.adapters.addBool("postalcode");
            jQuery.validator.unobtrusive.adapters.addBool("lettersonly");
        }
    }
})();