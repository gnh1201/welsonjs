// aviation.js
// Namhyeon Go <abuse@catswords.net>
// https://github.com/gnh1201/welsonjs
// 
// ***SECURITY NOTICE***
// Aviation Data Integration requires an internet connection, and data may be transmitted externally. Users must adhere to the terms of use and privacy policy.
// - AviationStack website: https://aviationstack.com/?utm_source=FirstPromoter&utm_medium=Affiliate&fpr=namhyeon71
// 
var HTTP = require("lib/http");

var API_BASE_URL = "https://api.aviationstack.com/v1";
var API_ACCESS_KEY = "";

function loadApiKey() {
    var s = FILE.readFile("data/aviationstack-apikey.txt", FILE.CdoCharset.CdoUTF_8);
    return s.trim();
}

function getData(type, params, limit, offset) {
    var params = params || {};
    var limit = (function(n) {
        return n > 0 ? n : 100;
    }(parseInt(limit));
    var offset = (function(n) {
        return n > -1 ? n : 0;
    }(parseInt(limit));
    
    if (API_ACCESS_KEY == "") {
        API_ACCESS_KEY = loadApiKey();
    }
    
    params["limit"] = limit;
    params["offset"] = offset;
    params["access_key"] = API_ACCESS_KEY;
    
    var response = HTTP.create()
        .setParameters(params)
        .open("GET", API_BASE_URL + "/" + type)
        .send();

    return response.responseBody;
}

function getFlights(params, limit, offset) {
    return getData("flights", params, limit, offset);
}

function getRoutes(params, limit, offset) {
    return getData("routes", params, limit, offset);
}

function getAirports(params, limit, offset) {
    return getData("airports", params, limit, offset);
}

function getAirlines(params, limit, offset) {
    return getData("airlines", params, limit, offset);
}

function getAircraftTypes(params, limit, offset) {
    return getData("aircraft_types", params, limit, offset);
}

function getCities(params, limit, offset) {
    return getData("cities", params, limit, offset);
}

function getCountries(params, limit, offset) {
    return getData("countries", params, limit, offset);
}

function getFlightSchedules(params, limit, offset) {
    return getData("timetable", params, limit, offset);
}

function getFlightsFuture(params, limit, offset) {
    return getData("flightsFuture", params, limit, offset);
}

exports.getData = getData;
exports.getFlights = getFlights;
exports.getRoutes = getRoutes;
exports.getAirports = getAirports;
exports.getAirlines = getAirlines;
exports.getAircraftTypes = getAircraftTypes;
exports.getCities = getCities;
exports.getCountries = getCountries;
exports.getFlightSchedules = getFlightSchedules;
exports.getFlightsFuture = getFlightsFuture;

exports.VERSIONINFO = "Aviation Data Integration (aviation.js) version 0.1.1";
exports.AUTHOR = "abuse@catswords.net";
exports.global = global;
exports.require = global.require;
