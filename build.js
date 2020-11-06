// Survive the Hunt build script:
// This gets rid of any require() calls in the source code so that FiveM JS engine (client-side) doesn't complain about it not being defined...
// Yeah there's probably a better way of doing this.

const fs = require('fs');

// Get script files passed to the build script.
const scriptsToBuild = process.argv.slice(2);

const outputPath = "./compiled/";

scriptsToBuild.forEach(fileName => {
    fs.readFile(fileName, 'utf8', (err, data) => {
        const regex = /require\('[@]\w+[/]*\w*'\)[;]*[/]{2}ignore/;
        const formatted = data.replace(regex, (substring) => { console.log(`Found ${substring} in ${fileName}, removing...`); return ""; });
        fs.writeFile(`${outputPath}${fileName}`, formatted, () => { });
    });
});