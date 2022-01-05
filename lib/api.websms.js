var HTTP = require("lib/http");

var WebSMSObject = function() {
    this.host = "localhost";
    this.token = "";
    this.country = "any";
    this.operator = "any";
    this.product = "";

    this.setHost = function(host) {
        this.host = host;
        return this;
    };

    this.setToken = function(token) {
        this.token = token;
        return this;
    };

    this.setCountry = function(country) {
        this.country = country;
        return this;
    };
    
    this.setOperator = function(operator) {
        this.operator = operator;
        return this;
    };

    this.setProduct = function(product) {
        this.product = product;
        return this;
    };

    this.buy = function() {
        try {
            var response = HTTP.create()
                .setBearerAuth(this.token)
                .setUseCache(false)
                .setHeader("Accept", "application/json")
                .setParameter("country", this.country)
                .setParameter("operator", this.operator)
                .setParameter("product", this.product)
                .open("GET", "https://" + this.host + "/v1/user/buy/activation/:country/:operator/:product")
                .send(function(res) {
                    console.log("Got the number: " + res.phone);
                }).responseBody
            ;
            return {
                "id": response.id,
                "number": response.phone
            };
        } catch(e) {
            console.error(e.message);
        }
    };

    this.get = function(id) {
        try {
            var response = HTTP.create()
                .setBearerAuth(this.token)
                .setUseCache(false)
                .setHeader("Accept", "application/json")
                .setParameter("id", id)
                .open("GET", "https://" + this.host + "/v1/user/check/:id")
                .send(function(res) {
                    var messages = res.sms;
                    messages.forEach(function(x) {
                        console.log("Got the code: " + x.code);
                    });
                }).responseBody
            ;
            return response.sms.reduce(function(a, x) {
                a = x.code;
                return a;
            }, null);
        } catch (e) {
            console.error(e.message);
        }
    };
};

exports.create = function() {
    return new WebSMSObject();
};
