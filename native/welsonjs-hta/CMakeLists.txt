cmake_minimum_required(VERSION 3.15)
project(welsonjs_hta LANGUAGES CXX)

if(NOT WIN32)
  message(FATAL_ERROR "The WelsonJS YAWW app is available only on Windows.")
endif()

# Use YAWW's MSHTML backend for local HTA/HTML compatibility. The app target
# below is implemented here; YAWW's example executables are not added.
set(WEBVIEW_USE_EDGE OFF CACHE BOOL "Disable the WebView2 backend" FORCE)
set(WEBVIEW_USE_MSHTML ON CACHE BOOL "Enable the MSHTML backend" FORCE)
set(WEBVIEW_ENABLE_CHAKRACORE ON CACHE BOOL
    "Enable the ChakraCore host scripting API" FORCE)
set(WEBVIEW_CHAKRACORE_USE_PREBUILT ON CACHE BOOL
    "Use prebuilt ChakraCore binaries" FORCE)
set(WEBVIEW_BUILD_CHAKRA_EXAMPLE OFF CACHE BOOL
    "Build only the WelsonJS app controller" FORCE)
set(WEBVIEW_ENABLE_SCINTILLA_CONSOLE OFF CACHE BOOL
    "Use a plain text ChakraCore console" FORCE)

add_subdirectory("${CMAKE_CURRENT_LIST_DIR}/../yaww"
                 "${CMAKE_CURRENT_BINARY_DIR}/yaww")

add_executable(welsonjs-hta WIN32 welsonjs_hta.cpp)
set_target_properties(welsonjs-hta PROPERTIES
  RUNTIME_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/bin")
target_compile_features(welsonjs-hta PRIVATE cxx_std_17)
target_compile_definitions(welsonjs-hta PRIVATE UNICODE _UNICODE)
target_link_libraries(welsonjs-hta PRIVATE webview comdlg32 shlwapi shell32)

add_custom_command(TARGET welsonjs-hta POST_BUILD
  COMMAND ${CMAKE_COMMAND} -E copy_if_different
    "${CMAKE_CURRENT_SOURCE_DIR}/webview.js"
    "$<TARGET_FILE_DIR:welsonjs-hta>/webview.js"
  VERBATIM)

webview_deploy_chakracore(welsonjs-hta)
