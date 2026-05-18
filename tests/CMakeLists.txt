cmake_minimum_required(VERSION 3.16)
project(idavie_plugin_tests CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# Path to the built shared library and its headers.
set(PLUGIN_DIR ${CMAKE_SOURCE_DIR}/../native_plugins_cmake)
set(PLUGIN_BUILD_DIR ${PLUGIN_DIR}/build)

find_package(PkgConfig REQUIRED)
pkg_check_modules(CFITSIO REQUIRED cfitsio)

add_executable(test_fits_reader test_fits_reader.cpp)
target_include_directories(test_fits_reader PRIVATE
    ${PLUGIN_DIR}
    ${CFITSIO_INCLUDE_DIRS})
target_link_directories(test_fits_reader PRIVATE
    ${PLUGIN_BUILD_DIR})
target_link_libraries(test_fits_reader PRIVATE
    idavie_native
    ${CFITSIO_LIBRARIES})

# Starlink AST's debian package leaves the graphics back-end (astGLine, astGMark, ...)
# undefined — host apps are expected to supply their own plotting layer. The plugin
# never calls those symbols, so tell the linker not to fail on transitively-undefined
# references that come in via .so dependencies.
target_link_options(test_fits_reader PRIVATE -Wl,--unresolved-symbols=ignore-in-shared-libs)

# Make sure the harness can find the .so at run time without `make install`.
set_target_properties(test_fits_reader PROPERTIES
    BUILD_RPATH "${PLUGIN_BUILD_DIR}")
