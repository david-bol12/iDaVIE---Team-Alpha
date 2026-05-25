cmake_minimum_required(VERSION 3.16)
project(idavie_plugin_tests CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# Path to the built shared library and its headers.
set(PLUGIN_DIR ${CMAKE_SOURCE_DIR}/../native_plugins_cmake)
set(PLUGIN_BUILD_DIR ${PLUGIN_DIR}/build)

find_package(PkgConfig REQUIRED)
pkg_check_modules(CFITSIO REQUIRED cfitsio)

# Default fixture passed to every test binary. Real tests can ignore it or
# build their own fixtures in-test; see LAYOUT.md.
set(SAMPLE_CUBE ${CMAKE_SOURCE_DIR}/../Data/SampleData/test_volume.fits)

enable_testing()

# Layout rationale and per-binary scope: see LAYOUT.md.
#
# Every binary gets the AST graphics-callback workaround
# (--unresolved-symbols=ignore-in-shared-libs) because libidavie_native.so
# pulls AST in transitively — binutils ld checks the whole link graph, even
# for binaries that never call AST themselves.
#
# SKIP_RETURN_CODE 77 lets stub binaries report Skipped to CTest by returning
# 77, so pending tests don't masquerade as passing.
function(add_plugin_test name)
    add_executable(${name} ${name}.cpp)
    target_include_directories(${name} PRIVATE
        ${PLUGIN_DIR}
        ${CFITSIO_INCLUDE_DIRS})
    target_link_directories(${name} PRIVATE
        ${PLUGIN_BUILD_DIR})
    target_link_libraries(${name} PRIVATE
        idavie_native
        ${CFITSIO_LIBRARIES})
    target_link_options(${name} PRIVATE
        -Wl,--unresolved-symbols=ignore-in-shared-libs)
    set_target_properties(${name} PROPERTIES
        BUILD_RPATH "${PLUGIN_BUILD_DIR}")
    add_test(NAME ${name}
             COMMAND ${name} ${SAMPLE_CUBE})
    set_tests_properties(${name} PROPERTIES
        SKIP_RETURN_CODE 77)
endfunction()

add_plugin_test(test_fits_reader)
add_plugin_test(test_ast_tool)
add_plugin_test(test_data_analysis_tool)
