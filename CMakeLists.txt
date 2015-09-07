cmake_minimum_required (VERSION 2.8)

project (Tools)
find_package(PkgConfig)

pkg_check_modules(libftdi REQUIRED libftdi>=0.19)
include(CheckIncludeFiles)
add_subdirectory(papilio_prog)
add_subdirectory(romgen)


