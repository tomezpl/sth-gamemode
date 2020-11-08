const gulp = require("gulp");

const exec = require("child_process").exec;

function build(cb) {
    exec("node ./build.js ./client.js ./server.js");
    cb();
}

exports.default = () => {
    gulp.watch(["client.js", "server.js"], build);
};