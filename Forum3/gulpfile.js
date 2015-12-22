/// <binding BeforeBuild='default' Clean='clean' />
"use strict";

var gulp = require("gulp"),
    rimraf = require("rimraf"),
    concat = require("gulp-concat"),
    cssmin = require("gulp-cssmin"),
    uglify = require("gulp-uglify"),
    merge = require('merge2'),
    ts = require('gulp-typescript');

var paths = {
    jsOutput: "./wwwroot/js/",
    css: "./wwwroot/css/",
    tsSource: "./Scripts/**/*.ts",
    tsBuild: "./Scripts/build/",
    tsDef: "./Scripts/def/"
};

paths.sourceJs = paths.tsBuild + "*.js";
paths.outputJsMin = "site.min.js";

paths.minCssAll = paths.css + "**/*.min.css";
paths.minCssDest = paths.css + "site.min.css";

var tsProject = ts.createProject({
    declarationFiles: true,
    noExternalResolve: false,
    module: 'AMD',
    removeComments: true
});

gulp.task('watch', ['ts:compile'], function () {
    gulp.watch(paths.tsDef, ['ts:compile']);
});

gulp.task("default", ['rebuild:js', 'rebuild:css']);

gulp.task('rebuild:js', ['ts:compile'], function () {
    gulp.src(paths.sourceJs)
        .pipe(concat("site.js"))
        .pipe(gulp.dest(paths.jsOutput));
})

gulp.task('ts:compile', function (done) {
    var tsResult = gulp.src(paths.tsSource).pipe(ts(tsProject));

    return merge([
        tsResult.dts.pipe(gulp.dest(paths.tsDef)),
        tsResult.js.pipe(gulp.dest(paths.tsBuild))
    ]);
});

gulp.task('rebuild:css', function () {
    gulp.src([paths.css, "!" + paths.minCssAll])
        .pipe(concat(paths.minCssDest))
        .pipe(cssmin())
        .pipe(gulp.dest(paths.minCssDest));
});

gulp.task('clean', ['clean:js', 'clean:tsBuild', 'clean:css'])

gulp.task("clean:js", function (done) {
    rimraf(paths.jsOutput, done);
});

gulp.task("clean:tsBuild", function (done) {
    rimraf(paths.tsBuild, done);
});

gulp.task("clean:css", function (done) {
    rimraf(paths.minCssDest, done);
});