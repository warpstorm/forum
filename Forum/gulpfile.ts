/// <binding AfterBuild='global-styles, page-styles, scripts' />

let gulp = require('gulp');
let del = require('del');
let concat = require('gulp-concat');
let browserify = require('browserify');
let sourcemaps = require('gulp-sourcemaps');
let source = require('vinyl-source-stream');
let buffer = require('vinyl-buffer');
let uglify = require('gulp-uglify-es').default;
let tsify = require('tsify');
let uglifyCss = require('gulp-uglifycss');

gulp.task('clean', function () {
	return del.sync([
		'client/app/**/*.js',
		'client/spec/**/*.js',
		'wwwroot/styles',
		'wwwroot/scripts'
	]);
});

gulp.task('global-styles', function () {
	return gulp
		.src([
			'client/styles/global-elements.css',
			'client/styles/layout.css',
			'client/styles/forms.css',
			'client/styles/buttons.css',
			'client/styles/standard-classes.css',
			'client/styles/bbc.css'
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
	basedir: 'client',
	debug: true,
	entries: ['app/app.ts'],
	plugin: [tsify],
	cache: {},
	packageCache: {}
};

gulp.task('scripts', function () {
	process.env.NODE_ENV = 'production';

	return browserify(browserifySettings)
		.bundle()
		.pipe(source('app.js'))
		.pipe(buffer())
		.pipe(sourcemaps.init({ loadMaps: true }))
		.pipe(uglify().on('error', function (e: any) {
			console.log(e); // https://stackoverflow.com/a/33006210/2621693
		}))
		.pipe(sourcemaps.write('./'))
		.pipe(gulp.dest('wwwroot/scripts'));
});

gulp.task('scripts-dev', function () {
	process.env.NODE_ENV = 'development';

	return browserify(browserifySettings)
		.bundle()
		.pipe(source('app.js'))
		.pipe(buffer())
		.pipe(sourcemaps.init({ loadMaps: true }))
		.pipe(sourcemaps.write('./'))
		.pipe(gulp.dest('wwwroot/scripts'));
});
