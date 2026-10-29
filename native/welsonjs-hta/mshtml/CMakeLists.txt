cmake_minimum_required(VERSION 3.15)
project(welsonjs_hta_mshtml LANGUAGES CXX)

set(WEBVIEW_USE_EDGE OFF CACHE BOOL "Use the WebView2 backend" FORCE)
set(WEBVIEW_USE_MSHTML ON CACHE BOOL "Use the MSHTML backend" FORCE)
set(WEBVIEW_ENABLE_CHAKRACORE ON CACHE BOOL
    "Enable the ChakraCore host scripting API" FORCE)
set(WEBVIEW_CHAKRACORE_USE_PREBUILT ON CACHE BOOL
    "Use prebuilt ChakraCore binaries" FORCE)
set(WEBVIEW_BUILD_CHAKRA_EXAMPLE OFF CACHE BOOL
    "Build only the WelsonJS app controller" FORCE)
set(WEBVIEW_ENABLE_SCINTILLA_CONSOLE OFF CACHE BOOL
    "Use a plain text ChakraCore console" FORCE)
set(CMAKE_RUNTIME_OUTPUT_DIRECTORY "${WELSONJS_HTA_RUNTIME_OUTPUT_DIRECTORY}")

add_subdirectory("${CMAKE_CURRENT_SOURCE_DIR}/../../yaww"
                 "${CMAKE_CURRENT_BINARY_DIR}/yaww")

add_executable(welsonjs-hta-mshtml WIN32 ../welsonjs_hta.cpp)
target_compile_features(welsonjs-hta-mshtml PRIVATE cxx_std_17)
target_compile_definitions(welsonjs-hta-mshtml PRIVATE UNICODE _UNICODE)
target_link_libraries(welsonjs-hta-mshtml PRIVATE webview shell32 shlwapi)
add_dependencies(welsonjs-hta-mshtml webview_chakracore_build)
