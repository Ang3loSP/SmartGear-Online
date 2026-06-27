// ================================================
// QUESTION 8: GULP TASK RUNNER CONFIGURATION
// Automates SASS compilation, minification, bundling
// ================================================

const gulp = require('gulp');
const sass = require('gulp-sass')(require('sass'));
const uglify = require('gulp-uglify');
const concat = require('gulp-concat');
const rename = require('gulp-rename');
const cleanCSS = require('gulp-clean-css');
const sourcemaps = require('gulp-sourcemaps');
const browserSync = require('browser-sync').create();
const autoprefixer = require('gulp-autoprefixer');

// File paths
const paths = {
    scss: {
        src: 'wwwroot/scss/**/*.scss',
        dest: 'wwwroot/css/'
    },
    js: {
        src: 'wwwroot/js-src/**/*.js',
        dest: 'wwwroot/js/'
    },
    vendor: {
        src: 'node_modules/',
        dest: 'wwwroot/lib/'
    }
};

// ================================================
// TASK 1: Compile SASS to CSS
// ================================================
function compileSass() {
    return gulp.src('wwwroot/scss/main.scss')
        .pipe(sourcemaps.init())
        .pipe(sass({
            outputStyle: 'compressed',
            includePaths: ['node_modules']
        }).on('error', sass.logError))
        .pipe(autoprefixer({
            cascade: false
        }))
        .pipe(cleanCSS())
        .pipe(rename({
            suffix: '.min'
        }))
        .pipe(sourcemaps.write('.'))
        .pipe(gulp.dest(paths.scss.dest))
        .pipe(browserSync.stream());
}

// ================================================
// TASK 2: Bundle and Minify JavaScript
// ================================================
function bundleJs() {
    return gulp.src([
        'wwwroot/js-src/site.js',
        'wwwroot/js-src/cart.js',
        'wwwroot/js-src/customization.js',
        'wwwroot/js-src/order.js'
    ])
        .pipe(sourcemaps.init())
        .pipe(concat('bundle.js'))
        .pipe(uglify())
        .pipe(rename({
            suffix: '.min'
        }))
        .pipe(sourcemaps.write('.'))
        .pipe(gulp.dest(paths.js.dest))
        .pipe(browserSync.stream());
}

// ================================================
// TASK 3: Copy Vendor Libraries
// ================================================
function copyVendors() {
    // Copy Bootstrap JS
    gulp.src('node_modules/bootstrap/dist/js/bootstrap.bundle.min.js')
        .pipe(gulp.dest(paths.vendor.dest + 'bootstrap/js/'));

    // Copy Bootstrap CSS
    gulp.src('node_modules/bootstrap/dist/css/bootstrap.min.css')
        .pipe(gulp.dest(paths.vendor.dest + 'bootstrap/css/'));

    // Copy jQuery
    gulp.src('node_modules/jquery/dist/jquery.min.js')
        .pipe(gulp.dest(paths.vendor.dest + 'jquery/'));

    // Copy Popper.js
    gulp.src('node_modules/@popperjs/core/dist/umd/popper.min.js')
        .pipe(gulp.dest(paths.vendor.dest + 'popperjs/'));

    return gulp.src(paths.vendor.src + '**/*')
        .pipe(gulp.dest(paths.vendor.dest));
}

// ================================================
// TASK 4: Watch for Changes (Development)
// ================================================
function watch() {
    browserSync.init({
        proxy: 'localhost:5000',  // Your ASP.NET Core app URL
        port: 3000,
        open: true,
        notify: false
    });

    gulp.watch(paths.scss.src, compileSass);
    gulp.watch(paths.js.src, bundleJs);
    gulp.watch('Views/**/*.cshtml').on('change', browserSync.reload);
}

// ================================================
// TASK 5: Build for Production
// ================================================
const build = gulp.series(compileSass, bundleJs, copyVendors);

// ================================================
// Export Tasks
// ================================================
exports.compileSass = compileSass;
exports.bundleJs = bundleJs;
exports.copyVendors = copyVendors;
exports.watch = watch;
exports.build = build;
exports.default = watch;