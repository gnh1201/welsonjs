cmake_minimum_required(VERSION 3.15)
project(welsonjs_hta LANGUAGES CXX)

if(NOT WIN32)
  message(FATAL_ERROR "The WelsonJS YAWW app is available only on Windows.")
endif()

include(ExternalProject)

set(CMAKE_RUNTIME_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/bin")

add_executable(welsonjs-hta WIN32 dispatcher.cpp)
set_target_properties(welsonjs-hta PROPERTIES
  RUNTIME_OUTPUT_DIRECTORY "${CMAKE_RUNTIME_OUTPUT_DIRECTORY}")
target_compile_features(welsonjs-hta PRIVATE cxx_std_17)
target_compile_definitions(welsonjs-hta PRIVATE
  UNICODE _UNICODE _SILENCE_EXPERIMENTAL_COROUTINE_DEPRECATION_WARNINGS)
target_link_libraries(welsonjs-hta PRIVATE comdlg32 shell32 windowsapp)

foreach(backend IN ITEMS webview2 edgehtml mshtml)
  ExternalProject_Add(welsonjs_hta_${backend}
    SOURCE_DIR "${CMAKE_CURRENT_SOURCE_DIR}/${backend}"
    BINARY_DIR "${CMAKE_CURRENT_BINARY_DIR}/build-${backend}"
    CMAKE_GENERATOR "${CMAKE_GENERATOR}"
    CMAKE_ARGS
      "-DWELSONJS_HTA_RUNTIME_OUTPUT_DIRECTORY:PATH=${CMAKE_RUNTIME_OUTPUT_DIRECTORY}"
      "-DCMAKE_BUILD_TYPE:STRING=${CMAKE_BUILD_TYPE}"
    BUILD_COMMAND ${CMAKE_COMMAND} --build <BINARY_DIR> --config $<CONFIG>
    INSTALL_COMMAND ""
  )
  add_dependencies(welsonjs-hta welsonjs_hta_${backend})
endforeach()
