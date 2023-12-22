// lz77.js
// https://github.com/gnh1201/welsonjs

function compress(input) {
    var compressed = '';
    var searchBufferIndex = 0;

    while (searchBufferIndex < input.length) {
        var longestMatchLength = 0;
        var longestMatchOffset = 0;

        // Search for the longest match in the look-ahead buffer
        for (var i = 0; i < searchBufferIndex; i++) {
            var matchLength = 0;
            while (matchLength < input.length - searchBufferIndex && input.charAt(i + matchLength) === input.charAt(searchBufferIndex + matchLength)) {
                matchLength++;
            }

            if (matchLength > longestMatchLength) {
                longestMatchLength = matchLength;
                longestMatchOffset = searchBufferIndex - i;
            }
        }

        // Output the token (offset, length)
        if (longestMatchLength > 0) {
            compressed += '(' + longestMatchOffset + ',' + longestMatchLength + ')';
            searchBufferIndex += longestMatchLength;
        } else {
            compressed += '(0,' + input.charAt(searchBufferIndex) + ')';
            searchBufferIndex++;
        }
    }

    return compressed;
}

function decompress(compressedData) {
    var decompressed = '';
    var currentIndex = 0;

    while (currentIndex < compressedData.length) {
        if (compressedData.charAt(currentIndex) === '(') {
            // Match case
            var commaIndex = compressedData.indexOf(',', currentIndex);
            var offset = parseInt(compressedData.substring(currentIndex + 1, commaIndex), 10);
            var closingParenIndex = compressedData.indexOf(')', commaIndex);
            var length = parseInt(compressedData.substring(commaIndex + 1, closingParenIndex), 10);

            for (var i = 0; i < length; i++) {
                var copyIndex = decompressed.length - offset;
                decompressed += decompressed.charAt(copyIndex);
            }

            currentIndex = closingParenIndex + 1;
        } else {
            // Literal case
            decompressed += compressedData.charAt(currentIndex);
            currentIndex++;
        }
    }

    return decompressed;
}

exports.compress = compress;
exports.decompress = decompress;

exports.VERSIONINFO = "LZ77 (MsCompress) algorithm implementation version 0.1";
exports.AUTHOR = "abuse@catswords.net";
exports.global = global;
exports.require = global.require;
