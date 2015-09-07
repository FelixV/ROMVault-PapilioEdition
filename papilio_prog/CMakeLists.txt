

list(APPEND papilio-prog_sources
 bitfile.cpp
 devicedb.cpp
 iobase.cpp
 jtag.cpp
 progalgspi.cpp
 progalgxc3s.cpp
 tools.cpp
 ioftdi.cpp
 butterfly.cpp
)

add_executable(papilio-prog  ${papilio-prog_sources})
target_link_libraries(papilio-prog ftdi)
install(TARGETS papilio-prog DESTINATION PapilioArcade/programmer/linux32)
