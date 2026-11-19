// patch.js
// Copyright 2019-2026, Namhyeon Go <gnh1201@catswords.re.kr> and the WelsonJS contributors.
// SPDX-License-Identifier: GPL-3.0-or-later
// https://github.com/gnh1201/welsonjs
//
// Source patches for the module loader. app.js keeps the patches in one place,
// "patches/<module path>.patch" (a unified diff for the module's source) and
// "patches/<module path>.post.js" (a script that runs after the module), and calls
// this module to apply them in memory: a patch never changes the file on disk.
//
// See docs/require-patches.md for the layout and an example.

// Splits a source text into lines, remembering the line ending to write back.
// The patch is matched line by line, so a CRLF source is compared as LF and
// turned back into CRLF before it is returned.
function splitSource(text) {
    var eol = text.indexOf("\r\n") > -1 ? "\r\n" : "\n";
    var normalized = text.replace(/\r\n/g, "\n");
    var trailingNewline = normalized.length > 0 && normalized.charAt(normalized.length - 1) === "\n";

    if (trailingNewline) {
        normalized = normalized.substring(0, normalized.length - 1);
    }

    return {
        eol: eol,
        trailingNewline: trailingNewline,
        lines: normalized.split("\n")
    };
}

// Applies a unified diff for one file to a source text and returns the result.
// Every hunk must match the source exactly; a difference raises an Error that
// names the line, so the caller can report which patch did not apply.
// The "---" and "+++" paths of the diff are not used: the patch file under
// patches/ already says which module it belongs to.
function apply(text, diff) {
    if (typeof text !== "string") {
        throw new Error("The source text is not a string");
    }
    if (typeof diff !== "string" || diff.length < 1) {
        throw new Error("The patch is empty");
    }

    var source = splitSource(text);
    var lines = source.lines;
    var patchLines = diff.replace(/\r\n/g, "\n").split("\n");
    var output = [];
    var trailingNewline = source.trailingNewline;
    var pos = 0;
    var i = 0;

    // Skip the description up to the file header: a "--- " line followed by "+++ ".
    while (i + 1 < patchLines.length) {
        if (patchLines[i].indexOf("--- ") === 0 && patchLines[i + 1].indexOf("+++ ") === 0) {
            break;
        }
        i++;
    }
    if (i + 1 >= patchLines.length) {
        throw new Error("The patch has no file header (a \"--- \" line followed by \"+++ \")");
    }
    i += 2;

    while (i < patchLines.length) {
        var line = patchLines[i];

        // A patch file normally ends with a newline, which leaves one empty line.
        if (line.length === 0 && i === patchLines.length - 1) {
            break;
        }

        var hunkLine = i + 1;
        var header = /^@@ -(\d+)(?:,(\d+))? \+(\d+)(?:,(\d+))? @@/.exec(line);
        if (!header) {
            throw new Error("Unexpected line " + (i + 1) + " of the patch (a patch holds the hunks of one file): " + line);
        }

        var oldStart = parseInt(header[1], 10);
        var oldCount = header[2] === undefined ? 1 : parseInt(header[2], 10);
        var newCount = header[4] === undefined ? 1 : parseInt(header[4], 10);
        var index = oldCount === 0 ? oldStart : oldStart - 1;
        if (index < pos || index > lines.length) {
            throw new Error("The hunk at line " + (i + 1) + " of the patch is out of order or out of range");
        }
        while (pos < index) {
            output.push(lines[pos]);
            pos++;
        }
        i++;

        var oldSeen = 0, newSeen = 0, lastTag = "";
        while (i < patchLines.length && patchLines[i].indexOf("@@") !== 0) {
            line = patchLines[i];
            if (line.length === 0) {
                if (i === patchLines.length - 1) {
                    break;
                }
                throw new Error("Empty line " + (i + 1) + " of the patch (a context line starts with a space)");
            }

            var tag = line.charAt(0);
            var body = line.substring(1);

            if (tag === " ") {
                if (pos >= lines.length || lines[pos] !== body) {
                    throw new Error("The patch does not match the source at line " + (pos + 1) + " (patch line " + (i + 1) + ")");
                }
                output.push(body);
                pos++;
                oldSeen++;
                newSeen++;
            } else if (tag === "-") {
                if (pos >= lines.length || lines[pos] !== body) {
                    throw new Error("The patch does not match the source at line " + (pos + 1) + " (patch line " + (i + 1) + ")");
                }
                pos++;
                oldSeen++;
            } else if (tag === "+") {
                output.push(body);
                newSeen++;
            } else if (tag !== "\\") {
                // The remaining tag is "\", the "no newline at end of file" marker.
                throw new Error("Unexpected line " + (i + 1) + " of the patch: " + line);
            }

            if (tag === "\\") {
                // It follows a line of the new file that has no line ending.
                if (lastTag === "+" || lastTag === " ") {
                    trailingNewline = false;
                }
            } else {
                lastTag = tag;
            }
            i++;
        }

        if (oldSeen !== oldCount || newSeen !== newCount) {
            throw new Error("The hunk at line " + hunkLine + " of the patch holds " + oldSeen + " old and " + newSeen + " new lines, not " + oldCount + " and " + newCount);
        }
    }

    while (pos < lines.length) {
        output.push(lines[pos]);
        pos++;
    }

    var result = output.join(source.eol);
    if (trailingNewline) {
        result += source.eol;
    }

    return result;
}

// Runs a post-processing script on a module that has already executed. The script
// sees the module as the loader would give it to a module (exports, require, module,
// __filename, __dirname) and may add to or replace module.exports.
function run(text, globalObject, module, requireFunction, filename, dirname) {
    if (typeof text !== "string" || text.length < 1) {
        throw new Error("The post-processing script is empty");
    }

    var compiled = new Function(
        "global",
        "exports",
        "require",
        "module",
        "__filename",
        "__dirname",
        '"use strict";\n'
        + text
        + "\n\nreturn module.exports;"
        + "\n//# sourceURL=" + filename
    );

    return compiled(globalObject, module.exports, requireFunction, module, filename, dirname);
}

exports.apply = apply;
exports.run = run;
