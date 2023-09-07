var ExtraMath = require("lib/extramath");

function main(args) {
	var a = "this is an apple";
    var b = "this is red apple";
    
    var dtm = new ExtraMath.DTM();
    dtm.add(a);
    dtm.add(b);
    var mat = dtm.toArray();

    console.log("This is a Cosine similarity calculator");
	console.log("Original sentance");
    console.log(a);
    console.log(b);
	console.log("Done");

	console.log("Create a DTM(Document-Term Matrix)");
    console.log(mat[0].join(' '));
    console.log(mat[1].join(' '));
    console.log("Done");

    console.log("Measure Cosine Similarity");
    console.log('' + ExtraMath.arrayCos(mat[0], mat[1]));
    console.log('' + ExtraMath.measureSimilarity(a, b));
    console.log("Done");
}

exports.main = main;
