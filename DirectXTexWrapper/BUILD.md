# Building the DirectXTex wrapper library

To reliably extract DDS image data from DDS containers, TuxDDS uses Microsoft's DirectXTex library.
To prevent name mangling, missing dependencies or version mismatches DirectXTex has a wrapper library, which is built in
this directory.

## Download vcpkg

```
git clone <https://github.com/microsoft/vcpkg/>
cd vcpkg
```

## Bootstrap vcpkg

```
./bootstrap-vcpkg.sh -disableMetrics
```

## Install DirectXTex

```
./vcpkg install directxtex:x64-linux-dynamic
```

## Change into the TuxDDS DirectXWrapper directory

```
cd ../TuxDDS/DirectXTexWrapper
```

## Build with CMake

```
cmake -B build -S . -DCMAKE_TOOLCHAIN_FILE=$HOME/source/vcpkg/scripts/buildsystems/vcpkg.cmake -DVCPKG_TARGET_TRIPLET=x64-linux-dynamic
cmake --build build
```

## Copy the build output to the lib folder of the root project directory

```
cp build/libDirectXTexWrapper.so ../lib
```

## Set the library to be always copied into the output directory

Rightclick libDirectXTexWrapper.so in Rider > Properties... > Copy to output directory > Set to "Copy always"