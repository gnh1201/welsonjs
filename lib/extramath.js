// lib/extramath.js
// https://github.com/gnh1201/welsonjs

// DTM(Document-term Matrix): https://en.wikipedia.org/wiki/Document-term_matrix
function DTM() {
    this.data = [];
    this.terms = [];

    this.add = function(s) {
        var w = s.trim().split(/\s+/);
        for (var i = 0; i < w.length; i++) {
            if (this.terms.indexOf(w[i]) < 0) this.terms.push(w[i]);
        }
        this.data.push(w);
    };

    this.toArray = function() {
        var dtm = [];
        for (var i = 0; i < this.data.length; i++) {
            var dt = [];
            for (var k = 0; k < this.terms.length; k++) {
                dt.push(this.data[i].indexOf(this.terms[k]) < 0 ? 0 : 1);
            }
            dtm.push(dt);
        }
        return dtm;
    };
}

function cos(A, B) {
    var dotproduct = 0;
    var mA = 0;
    var mB = 0;
    for (i = 0; i < A.length; i++) {
        dotproduct += (A[i] * B[i]);
        mA += (A[i] * A[i]);
        mB += (B[i] * B[i]);
    }
    mA = Math.sqrt(mA);
    mB = Math.sqrt(mB);
    var similarity = (dotproduct) / ((mA) * (mB))
    return similarity;
}

exports.DTM = DTM;
exports.cos = cos;
exports.measureSimilarity = function(s1, s2) {
    var dtm = new DTM();
    dtm.add(s1);
    dtm.add(s2);
    var mat = dtm.toArray();
    return cos(mat[0], mat[1]);
};

exports.VERSIONINFO = "ExtraMath module (extramath.js) version 0.0.3";
exports.AUTHOR = "catswords@protonmail.com";
exports.global = global;
exports.require = global.require;
