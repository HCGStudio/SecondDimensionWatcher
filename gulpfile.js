const { src, dest }  = require("gulp");
const minify = require("gulp-minify");

function minifyjs() {

    return src('SecondDimensionWatcher/wwwroot/js/main.js') 
        .pipe(minify({noSource: true}))
        .pipe(dest('SecondDimensionWatcher/wwwroot/js'))
}

exports.default = minifyjs;