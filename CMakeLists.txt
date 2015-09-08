cmake_minimum_required (VERSION 2.8)

SET(CMAKE_INSTALL_PREFIX "$ENV{HOME}")

project (Tools)
find_package(PkgConfig)

pkg_check_modules(libftdi REQUIRED libftdi>=0.19)
include(CheckIncludeFiles)
add_subdirectory(papilio_prog)
add_subdirectory(romgen)

INSTALL(DIRECTORY ROMVault2/Stage/datroot ROMVault2/Stage/images ROMVault2/Stage/papilio ROMVault2/Stage/ROMRoot ROMVault2/Stage/Unsorted DESTINATION ROMVault-Papilio  USE_SOURCE_PERMISSIONS)
INSTALL(FILES ROMVault2/Stage/ROMVault22.exe PERMISSIONS OWNER_EXECUTE OWNER_READ DESTINATION ROMVault-Papilio)

