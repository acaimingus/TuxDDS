# Building the DirectXTex wrapper library

To reliably extract DDS image data from DDS containers, TuxDDS uses Microsoft's DirectXTex library.
To prevent name mangling, missing dependencies or version mismatches DirectXTex has a wrapper library, which is built in
this directory.

## Download vcpkg

```bash
git clone https://github.com/microsoft/vcpkg/
cd vcpkg
```

## Bootstrap vcpkg

```bash
./bootstrap-vcpkg.sh -disableMetrics
```

## Install DirectXTex

```bash
./vcpkg install directxtex:x64-linux
```

## Install Magic Enum

Sadly, DirectXTex does not offer a method for getting file format names. It only returns a number that has to be resolved.
To reduce the amount of needed effort to maintain a list of DDS format names I'm just not gonna bother and use static reflection.
However C++ does not support static reflection by default, so magic-enum must be installed to get the enum names.

```bash
./vcpkg install magic-enum:x64-linux
```

## Change into the TuxDDS DirectXWrapper directory

```bash
cd ../TuxDDS/TuxDdsLib/DirectXTexWrapper
```

## Build with CMake

```bash
cmake -B build -S . -DCMAKE_TOOLCHAIN_FILE=$HOME/source/vcpkg/scripts/buildsystems/vcpkg.cmake -DVCPKG_TARGET_TRIPLET=x64-linux
cmake --build build
```

## Copy the build output to the lib folder of the root project directory

```bash
mkdir ../lib
cp build/libDirectXTexWrapper.so ../lib
```