/// <binding AfterBuild='global-styles, page-styles, scripts' />

var gulp = require('gulp');
var del = require('del');
var concat = require('gulp-concat');
var browserify = require("browserify");
var source = require('vinyl-source-stream');
var tsify = require("tsify");
var uglify = require('gulp-uglify-es').default;
var uglifyCss = require('gulp-uglifycss');
var sourcemaps = require('gulp-sourcemaps');
var buffer = require('vinyl-buffer');

gulp.task('clean', function () {
	return del.sync(['client/scripts/**/*.js', 'client/spec/**/*.js']);
});

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

gulp.task('scripts', function () {
	return browserify(browserifySettings)
		.plugin(tsify)
		.bundle()
		.pipe(source('app.js'))
		.pipe(buffer())
		.pipe(sourcemaps.init({ loadMaps: true }))
		.pipe(uglify().on('error', function (e) {
			console.log(e); // https://stackoverflow.com/a/33006210/2621693
		}))
		.pipe(sourcemaps.write('./'))
		.pipe(gulp.dest("wwwroot/scripts"));
});

gulp.task('scriptsUncompressed', function () {
	return browserify(browserifySettings)
		.plugin(tsify)
		.bundle()
		.pipe(source('app.js'))
		.pipe(buffer())
		.pipe(sourcemaps.init({ loadMaps: true }))
		.pipe(sourcemaps.write('./'))
		.pipe(gulp.dest("client"));
});
