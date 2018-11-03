/// <binding AfterBuild='global-styles, page-styles, scripts' />

var gulp = require('gulp');
var concat = require('gulp-concat');
var browserify = require("browserify");
var source = require('vinyl-source-stream');
var tsify = require("tsify");
var uglify = require('gulp-uglify');
var uglifyCss = require('gulp-uglifycss');
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
		.pipe(uglifyCss())
		.pipe(concat('global.css'))
		.pipe(gulp.dest('wwwroot/styles'));
});

gulp.task('page-styles', function () {
	return gulp.src('client/styles/pages/*.css')
		.pipe(uglifyCss())
		.pipe(gulp.dest('wwwroot/styles'));
});

var browserifySettings = {
	basedir: 'client/scripts',
	debug: true,
	entries: ['app.ts'],
	cache: {},
	packageCache: {}
};

function bundleApp() {
	return browserify(browserifySettings)
		.plugin(tsify)
		.bundle()
		.pipe(source('app.js'))
		.pipe(buffer())
		.pipe(sourcemaps.init({ loadMaps: true }))
		.pipe(uglify())
		.pipe(sourcemaps.write('./'))
		.pipe(gulp.dest("wwwroot/scripts"));
}

gulp.task('scripts', bundleApp);
