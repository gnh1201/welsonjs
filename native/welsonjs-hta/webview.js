// Runs in the host-side ChakraCore runtime, not in the page's browser engine.
webview.on("ready", function () {
  console.log("WelsonJS HTA is controlled by ChakraCore");
  webview.resizeTo(1100, 760);
});

webview.on("navigate", function (url) {
  console.log("Navigating to " + url);
});

console.log("Use webview.navigate(url), webview.eval(js), and webview.resizeTo(w, h)");
