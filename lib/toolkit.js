////////////////////////////////////////////////////////////////////////
// WelsonJS.Toolkit API
////////////////////////////////////////////////////////////////////////

var ToolkitObject = function() {
    this.interface = null;

    this.create = function() {
        try {
            this.interface = CreateObject("WelsonJS.Toolkit");
            return this;
        } catch (e) {
            console.error("ToolkitObject.create() ->", e.message);
        }
    };

    this.getInterface = function() {
        return this.interface;
    };
};

exports.create = function() {
    return new ToolkitObject();
};
