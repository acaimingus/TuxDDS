# TuxDDS

## Building DirectXTex

### Get vcpkg

git clone <https://github.com/microsoft/vcpkg.git>
cd vcpkg
./bootstrap-vcpkg.sh

### Install DirectXTex using vcpkg

./vcpkg install directxtex:x64-linux-dynamic

### Copy the libraries into TuxDDS

cp -r installed/x64-linux-dynamic/lib ~/source/TuxDDS/
