// aviation.js
// Namhyeon Go <abuse@catswords.net>
// https://github.com/gnh1201/welsonjs
// 
// ***SECURITY NOTICE***
// Aviation Data Integration requires an internet connection, and data may be transmitted externally. Users must adhere to the terms of use and privacy policy.
// - AviationStack website: https://aviationstack.com/?utm_source=FirstPromoter&utm_medium=Affiliate&fpr=namhyeon71
// - SearchApi website: https://www.searchapi.io/?via=namhyeon
// 
var HTTP = require("lib/http");
var APIKEY = require("lib/apikey");

function getData(type, params, limit, offset) {
    var params = params || {};
    var limit = (function(n) {
        return n > 0 ? n : 100;
    }(parseInt(limit));
    var offset = (function(n) {
        return n > -1 ? n : 0;
    }(parseInt(limit));
    
    params["limit"] = limit;
    params["offset"] = offset;
    params["access_key"] = APIKEY.getApiKey("aviationstack");
    
    var response = HTTP.create()
        .setParameters(params)
        .open("GET", "https://api.aviationstack.com/v1/" + type)
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

function getRoundTrip(arrival_id, departure_id, outbound_date, return_date) {
    var response = HTTP.create()
        .setParameters({
            "api_key": APIKEY.getApiKey("searchapi"),
            "arrival_id": arrival_id,
            "departure_id": departure_id,
            "engine": "google_flights",
            "flight_type": "round_trip",
            "outbound_date": outbound_date,
            "return_date": return_date
        })
        .open("GET", "https://www.searchapi.io/api/v1/search")
        .send();

    return response.responseBody;
}

function getOneWay(arrival_id, departure_id, outbound_date) {
    var response = HTTP.create()
        .setParameters({
            "api_key": APIKEY.getApiKey("searchapi"),
            "arrival_id": arrival_id,
            "departure_id": departure_id,
            "engine": "google_flights",
            "flight_type": "one_way",
            "outbound_date": outbound_date
        })
        .open("GET", "https://www.searchapi.io/api/v1/search")
        .send();

    return response.responseBody;
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
exports.getRoundTrip = getRoundTrip;
exports.getOneWay = getOneWay;

exports.VERSIONINFO = "Aviation Data Integration (aviation.js) version 0.1.2";
exports.AUTHOR = "abuse@catswords.net";
exports.global = global;
exports.require = global.require;
