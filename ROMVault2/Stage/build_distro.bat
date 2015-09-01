@echo off
cls
rmdir /s/q distro
mkdir distro
mkdir distro\Unsorted
mkdir distro\papilio
mkdir distro\datroot
mkdir distro\images
mkdir distro\romroot
mkdir distro\library
copy ROMVault22.exe distro\
xcopy /e papilio distro\papilio\
xcopy /e datroot distro\datroot\
xcopy /e images distro\images\
xcopy /e romroot distro\romroot\
xcopy /e library distro\library\