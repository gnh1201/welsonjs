// fix_excel_format.js
// This script requires the WelsonJS framework.
var Office = require("lib/msoffice");

function main(args) {
	var excel = new Office.Excel();   // Create a Excel instance
	excel.open("data\\fulllist.xlsx");   // Open a Excel window
	
	// select the worksheet
	excel.selectWorksheet(2);
	
	for (var i = 0; i < 34; i++) {
		var rownum = 2 + i;
		
		var company_name = excel.getCellByPosition(rownum, 3).getValue();
		var reservation_time = excel.getCellByPosition(rownum, 7).getValue();
		var resource_name = excel.getCellByPosition(rownum, 8).getValue();
		var port_number = excel.getCellByPosition(rownum, 9).getValue();
		var bandwidth = excel.getCellByPosition(rownum, 10).getValue();
		
		console.log("================");
        console.log("Company Name: " + company_name);
        console.log("Reservation Time: " + reservation_time);
        console.log("Resource Name: " + resource_name);
        console.log("Port Number: " + port_number);
        console.log("Bandwidth: " + bandwidth);
        console.log("================");
        
        make_card_file({
            "company_name": company_name,
            "reservation_time": reservation_time,
            "resource_name": resource_name,
            "port_number": port_number,
            "bandwidth": bandwidth
        });
    }
}

function make_card_file(data) {
    var excel = new Office.Excel();
    
    excel.open("card_format.xlsx");
    
    excel.getCellByPosition(2, 1).setValue(data.company_name);
    excel.getCellByPosition(2, 2).setValue(!data.resource_name ? "127.0.0.1" : data.resource_name);
    excel.getCellByPosition(2, 3).setValue(!data.port_number ? "80" : data.port_number);
    excel.getCellByPosition(2, 4).setValue(!data.bandwidth ? "100M" : data.bandwidth);
    excel.getCellByPosition(2, 5).setValue(data.reservation_time);
    
    var file_name = String(data.company_name) + "_card.xlsx";
    excel.saveAs("data\\" + file_name);
    
    excel.close();
}

exports.main = main;
