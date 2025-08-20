// extramath.js
// Copyright 2019-2025, Namhyeon Go <gnh1201@catswords.re.kr> and the WelsonJS contributors.
// SPDX-License-Identifier: GPL-3.0-or-later
// https://github.com/gnh1201/welsonjs
// 
// Documentation: https://app.rdbl.io/T0AB411UHDW9/S0VLKP4HKVGC/P0TL9IQZOUFT
// 
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

// Cosine similarity: https://en.wikipedia.org/wiki/Cosine_similarity
function arrayCos(A, B) {
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

function measureSimilarity(s1, s2) {
    var dtm = new DTM();
    dtm.add(s1);
    dtm.add(s2);
    var mat = dtm.toArray();
    return arrayCos(mat[0], mat[1]);
}

function export_measureSimilarity() {
    return "var ExtraMath=function(){};ExtraMath.DTM=function(){this.data=[],this.terms=[],this.add=function(t){for(var r=t.trim().split(/\s+/),a=0;a<r.length;a++)0>this.terms.indexOf(r[a])&&this.terms.push(r[a]);this.data.push(r)},this.toArray=function(){for(var t=[],r=0;r<this.data.length;r++){for(var a=[],s=0;s<this.terms.length;s++)a.push(0>this.data[r].indexOf(this.terms[s])?0:1);t.push(a)}return t}},ExtraMath.arrayCos=function(t,r){var a=0,s=0,h=0;for(i=0;i<t.length;i++)a+=t[i]*r[i],s+=t[i]*t[i],h+=r[i]*r[i];return a/((s=Math.sqrt(s))*(h=Math.sqrt(h)))},ExtraMath.measureSimilarity=function(t,r){var a=new ExtraMath.DTM;a.add(t),a.add(r);var s=a.toArray();return ExtraMath.arrayCos(s[0],s[1])};";
}

// Cartesian product: https://en.wikipedia.org/wiki/Cartesian_product
function cartesianProduct(arr) {
    return arr.reduce(function(a,b){
        return a.map(function(x){
            return b.map(function(y){
                return x.concat([y]);
            })
        }).reduce(function(a,b){ return a.concat(b) },[])
    }, [[]])
}

function clusteredCellsDensity(numbers, size, minDensity) {
    if (!numbers || !numbers.length) return false;
    if (typeof size !== 'number' || size <= 0) size = 4; // default grid size = 4
    if (typeof minDensity !== 'number' || isNaN(minDensity)) minDensity = 0.6;

    var maxIndex = size * size; // because it's 1-based (1..size*size)

    // 1. Convert cell number → (x,y) coordinates with 1-based check
    var coords = [];
    for (var i = 0; i < numbers.length; i++) {
    var n = numbers[i];
    // must be within 1..maxIndex
    if (typeof n !== 'number' || n < 1 || n > maxIndex) {
      return false; // invalid index -> immediately return false
    }
    var idx = n - 1; // shift to 0-based
        coords.push({ x: idx % size, y: Math.floor(idx / size) });
    }

    var xs = coords.map(function (c) { return c.x; });
    var ys = coords.map(function (c) { return c.y; });

    // 2. Compute bounding box of selected cells
    var minX = Math.min.apply(Math, xs), maxX = Math.max.apply(Math, xs);
    var minY = Math.min.apply(Math, ys), maxY = Math.max.apply(Math, ys);

    // 3. Compute area and density
    var w = maxX - minX + 1;
    var h = maxY - minY + 1;
    var boxArea = w * h;

    var density = coords.length / boxArea;
    return density >= minDensity;
}

function estimateTileStartPosition(index, tiles, spreadSize, gap) {  
    var tileSize = (spreadSize - gap * (tiles - 1)) / tiles;

    var i = index - 1;
    var col = i % tiles;
    var row = Math.floor(i / tiles);
  
    return {  
        x: col * (tileSize + gap),
        y: row * (tileSize + gap)
    };
}

exports.DTM = DTM;
exports.arrayCos = arrayCos;
exports.measureSimilarity = measureSimilarity;
exports.export_measureSimilarity = export_measureSimilarity;
exports.cartesianProduct = cartesianProduct;
exports.clusteredCellsDensity = clusteredCellsDensity;
exports.estimateTileStartPosition = estimateTileStartPosition;

exports.VERSIONINFO = "ExtraMath module (extramath.js) version 1.0.3";
exports.AUTHOR = "gnh1201@catswords.re.kr";
exports.global = global;
exports.require = global.require;
