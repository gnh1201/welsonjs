exports.main = function() {
    console.log(JSON.stringify({test: 1}));
    console.log(squel.select("test"));
}
