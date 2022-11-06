var ExtraMath = require("lib/extramath");

function main(args) {
	var a = "this is an apple";
	var b = "this is red apple";
	
	var dtm = new ExtraMath.DTM();
	dtm.add(a);
	dtm.add(b);
	var mat = dtm.toArray();

	console.log(mat[0].join(' '));
	console.log(mat[1].join(' '));

	console.log(String(ExtraMath.cos(mat[0], mat[1])));
	console.log(String(ExtraMath.measureSimilarity(a, b)));
}

exports.main = main;
