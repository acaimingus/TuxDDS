cmake_minimum_required(VERSION 3.10)
project(DirectXTexWrapper)

add_library(DirectXTexWrapper SHARED DirectXTexWrapper.cpp)

find_package(directxtex CONFIG REQUIRED)
target_link_libraries(DirectXTexWrapper PRIVATE Microsoft::DirectXTex)