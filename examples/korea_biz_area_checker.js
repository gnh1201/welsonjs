// korea_biz_area_checker.js
// Namhyeon Go <gnh1201@catswords.re.kr>, WelsonJS OSS team
// https://github.com/gnh1201/welsonjs
// 
var FILE = require("lib/file");
var Office = require("lib/msoffice");

function main(args) {
	// Business Area Code in Korea
	console.log("> Business Area Code in Korea");
	var data_biz_areacode = JSON.parse(FILE.readFile("data/korea_business_areacode.json")).data;
	console.log(JSON.stringify(data_biz_areacode));
	
	// Open the Microsoft Excel file
	console.log("> Open the Microsoft Excel file");
	var excel = new Office.Excel();
    excel.open("data\\example.xlsx");
    
    // Retrieve the business area code
    for (var i = 3; i < 1233; i++) {
        try {
            // check the biz area name
            console.log(">> check the biz area name");
            var areaname = excel.getCellByPosition(i, 16).getValue();
            if (!!areaname)
                continue;
            
            // check the biz number
            console.log(">> check the biz number");
            var biznumber = excel.getCellByPosition(i, 8).getValue();
            if (!biznumber)
                continue;
            
            // match the biznumber to biz area code data
            console.log(">> match the biznumber to biz area code data: " + biznumber);
            var matched_areaname = "unknown";
            for (var k in data_biz_areacode) {
                var areacode = parseInt(biznumber.substring(0, 3));
                if (data_biz_areacode[k].indexOf(areacode) > -1) {
                    matched_areaname = k;
                    break;
                }
            }
            
            console.log(">> write the matched area name: " + matched_areaname);
            excel.getCellByPosition(i, 16).setValue(matched_areaname);
        } catch (e) {
            excel.getCellByPosition(i, 16).setValue("unknown");
        }
    }
    
    excel.saveAs("example_edited.xlsx");
    
    excel.close();
}

exports.main = main;