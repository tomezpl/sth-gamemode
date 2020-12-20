const gulp = require("gulp");

const exec = require("child_process").exec;

const srcFiles = ['client.js', 'server.js'];

function build(cb) {
    const argList = ['', ...srcFiles].reduce((acc, fName) => ((acc && acc.length >= 0) ? `${acc} ` : '') + `./${fName}`);
    exec('node ./build.js ' + argList);
    cb();
}

exports.default = () => {
    gulp.watch(srcFiles, build);
};