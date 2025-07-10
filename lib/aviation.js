// aviation.js
// Copyright 2019-2025, Namhyeon Go <gnh1201@catswords.re.kr> and the WelsonJS contributors.
// SPDX-License-Identifier: GPL-3.0-or-later
// https://github.com/gnh1201/welsonjs
// 
// SECURITY NOTICE
// AviationStack, SerpApi requires an internet connection, and data may be transmitted externally. Please check the terms of use and privacy policy.
// https://aviationstack.com/?utm_source=FirstPromoter&utm_medium=Affiliate&fpr=namhyeon71
// https://serpapi.com/security?utm_source=welsonjs
// 
var HTTP = require("lib/http");
var CRED = require("lib/credentials");

var DEFAULT_CURRENCY = "USD";
var DEFAULT_LANGUAGE_CODE = "en";

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
    params["access_key"] = CRED.get("apikey", "aviationstack");
    
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
            engine: "google_flights",
            departure_id: departure_id,
            arrival_id: arrival_id,
            outbound_date: outbound_date,
            return_date: return_date,
            currency: DEFAULT_CURRENCY,
            hl: DEFAULT_LANGUAGE_CODE,
            api_key: CRED.get("apikey", "serpapi")
        })
        .open("GET", "https://serpapi.com/search.json")
        .send();

    return response.responseBody;
}

function getOneWay(arrival_id, departure_id, outbound_date) {
    var response = HTTP.create()
        .setParameters({
            engine: "google_flights",
            departure_id: departure_id,
            arrival_id: arrival_id,
            outbound_date: outbound_date,
            return_date: "",
            currency: DEFAULT_CURRENCY,
            hl: DEFAULT_LANGUAGE_CODE,
            api_key: CRED.get("apikey", "serpapi")
        })
        .open("GET", "https://serpapi.com/search.json")
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

exports.VERSIONINFO = "Aviation Data Integration (aviation.js) version 0.1.4";
exports.AUTHOR = "gnh1201@catswords.re.kr";
exports.global = global;
exports.require = global.require;
