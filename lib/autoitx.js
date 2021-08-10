////////////////////////////////////////////////////////////////////////
// AutoItX3.Control API
////////////////////////////////////////////////////////////////////////

var AutoItXObject = function() {
    this.interface = null;

    this.create = function() {
        try {
            this.interface = CreateObject("AutoItX3.Control");
            return this;
        } catch (e) {
            console.error("AutoItXObject.create() ->", e.message);
        }
    };

    this.getInterface = function() {
        return this.interface;
    };

    this.create();
};

exports.create = function() {
    return new AutoItXObject();
};
