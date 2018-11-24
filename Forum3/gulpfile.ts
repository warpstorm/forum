/// <binding AfterBuild='global-styles, page-styles, scripts' />

import * as gulp from 'gulp';
import * as del from 'del';
import * as concat from 'gulp-concat';
import * as browserify from 'browserify';
import * as source from 'vinyl-source-stream';
import * as sourcemaps from 'gulp-sourcemaps';
import * as buffer from 'vinyl-buffer';
import uglify from 'gulp-uglify-es';

// no @types available:
const tsify = require('tsify');
const uglifyCss = require('gulp-uglifycss');

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
		.pipe(gulp.dest('wwwroot/scripts'));
});

gulp.task('scriptsUncompressed', function () {
	return browserify(browserifySettings)
		.plugin(tsify)
		.bundle()
		.pipe(source('app.js'))
		.pipe(buffer())
		.pipe(sourcemaps.init({ loadMaps: true }))
		.pipe(sourcemaps.write('./'))
		.pipe(gulp.dest('client'));
});
