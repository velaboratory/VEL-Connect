const path = require('path');

module.exports = {
    entry: './dist/index.js',
    output: {
        filename: 'velconnect.min.js',
        path: path.resolve(__dirname, 'dist'),
        library: 'velconnect',
        libraryTarget: 'umd',
    },
};