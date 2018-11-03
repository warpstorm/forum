/// <binding AfterBuild='global-styles, page-styles, scripts' />

var gulp = require('gulp');
var concat = require('gulp-concat');
var browserify = require("browserify");
var source = require('vinyl-source-stream');
var watchify = require("watchify");
var tsify = require("tsify");
var gutil = require("gulp-util");
var uglify = require('gulp-uglify');
var sourcemaps = require('gulp-sourcemaps');
var buffer = require('vinyl-buffer');

gulp.task('global-styles', function () {
	return gulp
		.src([
			"client/styles/global-elements.css",
			"client/styles/layout.css",
			"client/styles/forms.css",
			"client/styles/buttons.css",
			"client/styles/standard-classes.css",
			"client/styles/bbc.css"
		])
		.pipe(uglify())
		.pipe(concat('global.css'))
		.pipe(gulp.dest('wwwroot/styles'));
});

gulp.task('page-styles', function () {
	return gulp.src('client/styles/pages/*.css')
		.pipe(uglify())
		.pipe(gulp.dest('wwwroot/styles'));
});

var watchedBrowserify = watchify(browserify({
	basedir: '.',
	debug: true,
	entries: ['client/scripts/app.ts'],
	cache: {},
	packageCache: {}
}).plugin(tsify));

function bundleApp() {
	return watchedBrowserify
		.bundle()
		.pipe(source('app.js'))
		.pipe(buffer())
		.pipe(sourcemaps.init({ loadMaps: true }))
		.pipe(uglify())
		.pipe(sourcemaps.write('./'))
		.pipe(gulp.dest("wwwroot/scripts"));
}

gulp.task('scripts', bundleApp);

watchedBrowserify.on('update', bundleApp);
watchedBrowserify.on('log', gutil.log);