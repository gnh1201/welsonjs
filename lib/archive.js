// archive.js

var ArchiveObject = function() {
    this.engine = "HOW_TO_COMPRESS_AND_DECOMPRES_A_FILE
  
    this.create = function(engine) {
        this.engine = engine;
    }

    this.extractTo = function(targetDirectory) {
        if (this.engine == "METHOD_1") {
            // how to extract a file with METHOD_1
        } else (this.engine == "METHOD_2") {
            // how to extract a file with METHOD_2
        } // ...
    }

    this.compressDirectory = function(sourceDirectory) {
        if (this.engine == "METHOD_1") {
            // how to compress a file with METHOD_1
        } else (this.engine == "METHOD_2") {
            // how to compress a file with METHOD_2
        } // ...
    }

    // A good practice is lib/http.js
}
