(function() {
    return {
        setUp: function() {
            inform('Starting...');
        },

        tearDown: function() {
            inform('Finishing...')
        },

        testGreenTest: function () {
            assert("this is true", true);
        },

        testRedTest: function () {
            assert("this is false", false);
        }
    };
})();
