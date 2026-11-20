# Toolchain for the ARM64 Windows build of CrispEmbed (build-crispembed.ps1 -Arch arm64):
# clang targeting arm64-pc-windows-msvc, run inside the Visual Studio x64_arm64 developer
# environment (the ARM64 MSVC libraries and Windows SDK, the x64-hosted ARM64 linker).
# ggml does not support MSVC on ARM. The flags are those of llama.cpp's
# cmake/arm64-windows-llvm.cmake (MIT, the ggml-org/llama.cpp authors), which the
# WelsonJS llama.cpp ARM64 build uses too: -march=armv8.7-a.

set( CMAKE_SYSTEM_NAME Windows )
set( CMAKE_SYSTEM_PROCESSOR arm64 )

set( target arm64-pc-windows-msvc )

set( CMAKE_C_COMPILER    clang )
set( CMAKE_CXX_COMPILER  clang++ )

set( CMAKE_C_COMPILER_TARGET   ${target} )
set( CMAKE_CXX_COMPILER_TARGET ${target} )
set( CMAKE_ASM_COMPILER_TARGET ${target} )

set( arch_c_flags "-march=armv8.7-a -fvectorize -ffp-model=fast -fno-finite-math-only" )
set( warn_c_flags "-Wno-format -Wno-unused-variable -Wno-unused-function -Wno-gnu-zero-variadic-macro-arguments" )

# CrispEmbed's CMakeLists.txt defines these for MSVC only (if(MSVC)), but clang builds
# against the same Windows SDK and needs them too: <windows.h> without NOMINMAX breaks
# std::numeric_limits<>::max(), and without WIN32_LEAN_AND_MEAN it clashes with winsock2.h.
set( win_defs "-D_CRT_SECURE_NO_WARNINGS -D_USE_MATH_DEFINES -DNOMINMAX -DWIN32_LEAN_AND_MEAN" )

set( CMAKE_C_FLAGS_INIT   "${arch_c_flags} ${warn_c_flags} ${win_defs}" )
set( CMAKE_CXX_FLAGS_INIT "${arch_c_flags} ${warn_c_flags} ${win_defs}" )
