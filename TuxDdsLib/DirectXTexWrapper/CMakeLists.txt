cmake_minimum_required(VERSION 3.14)
project(DirectXTexWrapper LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

add_library(DirectXTexWrapper SHARED DirectXTexWrapper.cpp)
target_include_directories(DirectXTexWrapper PUBLIC ${CMAKE_CURRENT_SOURCE_DIR})

find_package(directxtex CONFIG REQUIRED)
target_link_libraries(DirectXTexWrapper PRIVATE Microsoft::DirectXTex)

find_package(magic_enum CONFIG REQUIRED)
target_link_libraries(DirectXTexWrapper PRIVATE magic_enum::magic_enum)

include(CTest)
if(BUILD_TESTING)
    find_package(GTest CONFIG REQUIRED)
    include(GoogleTest)

    add_executable(DirectXTexWrapperTests tests/DirectXTexWrapperTests.cpp)
    target_link_libraries(DirectXTexWrapperTests PRIVATE
        DirectXTexWrapper
        Microsoft::DirectXTex
        GTest::gtest
        GTest::gtest_main
    )

    gtest_discover_tests(DirectXTexWrapperTests)
endif()
