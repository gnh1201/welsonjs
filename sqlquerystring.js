exports.main = function() {
	console.log(squel.select({ separator: "\n" })
        .from("students")
        .field("name")
        .field("MIN(test_score)")
        .field("MAX(test_score)")
        .field("GROUP_CONCAT(DISTINCT test_score ORDER BY test_score DESC SEPARATOR ' ')")
        .group("name")
        .toString());
};