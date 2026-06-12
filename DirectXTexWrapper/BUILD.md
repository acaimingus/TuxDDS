# Getting DirectXTex

## Download vcpkg

git clone <https://github.com/microsoft/vcpkg/>
cd vcpkg

## Bootstrap vcpkg

./bootstrap-vcpkg.sh -disableMetrics

## Install DirectXTex

./vcpkg install directxtex:x64-linux-dynamic

# Build with CMake

cmake -B build -S . -DCMAKE_TOOLCHAIN_FILE=$HOME/source/vcpkg/scripts/buildsystems/vcpkg.cmake -DVCPKG_TARGET_TRIPLET=x64-linux-dynamic
cmake --build build