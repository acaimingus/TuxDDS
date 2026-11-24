/*
 * Copyright (c) 2026 Adam Martula
 * This source code is licensed under the MIT license found in the LICENSE file in the root of this source tree.
 */

#include "DirectXTexWrapper.h"

#include <gtest/gtest.h>

#include <cstring>
#include <filesystem>
#include <fstream>
#include <string>
#include <vector>

#include <DirectXTex.h>

namespace {

class DirectXTexWrapperTest : public ::testing::Test {
protected:
    std::filesystem::path tempDir;

    void SetUp() override {
        const auto *testInfo = ::testing::UnitTest::GetInstance()->current_test_info();
        std::string dirName = std::string("tuxdds_cpp_test_") + testInfo->test_suite_name() + "_" + testInfo->name();
        tempDir = std::filesystem::temp_directory_path() / dirName;
        std::filesystem::remove_all(tempDir);
        std::filesystem::create_directories(tempDir);
    }

    void TearDown() override {
        std::error_code ec;
        std::filesystem::remove_all(tempDir, ec);
    }

    std::string CreateRgba8Dds(const std::string &fileName, size_t width, size_t height,
                               const std::vector<uint8_t> &pixels) {
        DirectX::ScratchImage scratch;
        HRESULT hr = scratch.Initialize2D(DXGI_FORMAT_R8G8B8A8_UNORM, width, height, 1, 1);
        EXPECT_TRUE(SUCCEEDED(hr));

        const DirectX::Image *img = scratch.GetImage(0, 0, 0);
        EXPECT_NE(img, nullptr);
        std::memcpy(img->pixels, pixels.data(), pixels.size());

        auto outPath = tempDir / fileName;
        auto widePath = outPath.wstring();
        hr = DirectX::SaveToDDSFile(*img, DirectX::DDS_FLAGS_NONE, widePath.c_str());
        EXPECT_TRUE(SUCCEEDED(hr));
        return outPath.string();
    }

    std::string CreateBgra8Dds(const std::string &fileName, size_t width, size_t height,
                               const std::vector<uint8_t> &pixels) {
        DirectX::ScratchImage scratch;
        HRESULT hr = scratch.Initialize2D(DXGI_FORMAT_B8G8R8A8_UNORM, width, height, 1, 1);
        EXPECT_TRUE(SUCCEEDED(hr));

        const DirectX::Image *img = scratch.GetImage(0, 0, 0);
        EXPECT_NE(img, nullptr);
        std::memcpy(img->pixels, pixels.data(), pixels.size());

        auto outPath = tempDir / fileName;
        auto widePath = outPath.wstring();
        hr = DirectX::SaveToDDSFile(*img, DirectX::DDS_FLAGS_NONE, widePath.c_str());
        EXPECT_TRUE(SUCCEEDED(hr));
        return outPath.string();
    }

    std::string CreateBc1CompressedDds(const std::string &fileName) {
        // BC1 requires multiples of 4x4 pixels
        constexpr size_t width = 4;
        constexpr size_t height = 4;
        std::vector<uint8_t> rgba(width * height * 4, 255);
        for (size_t i = 0; i < width * height; ++i) {
            rgba[i * 4 + 0] = 255; // R
            rgba[i * 4 + 1] = 0;   // G
            rgba[i * 4 + 2] = 0;   // B
            rgba[i * 4 + 3] = 255; // A
        }

        DirectX::ScratchImage src;
        HRESULT hr = src.Initialize2D(DXGI_FORMAT_R8G8B8A8_UNORM, width, height, 1, 1);
        EXPECT_TRUE(SUCCEEDED(hr));
        std::memcpy(src.GetImage(0, 0, 0)->pixels, rgba.data(), rgba.size());

        DirectX::ScratchImage compressed;
        hr = DirectX::Compress(*src.GetImage(0, 0, 0), DXGI_FORMAT_BC1_UNORM, DirectX::TEX_COMPRESS_DEFAULT,
                               DirectX::TEX_THRESHOLD_DEFAULT, compressed);
        EXPECT_TRUE(SUCCEEDED(hr));

        auto outPath = tempDir / fileName;
        auto widePath = outPath.wstring();
        hr = DirectX::SaveToDDSFile(*compressed.GetImage(0, 0, 0), DirectX::DDS_FLAGS_NONE, widePath.c_str());
        EXPECT_TRUE(SUCCEEDED(hr));
        return outPath.string();
    }
};

TEST_F(DirectXTexWrapperTest, ExtractInfo_UncompressedRgba8_ReturnsExpectedMetadata) {
    std::vector<uint8_t> pixels(2 * 3 * 4, 128);
    const std::string ddsPath = CreateRgba8Dds("rgba8.dds", 2, 3, pixels);

    int width = 0;
    int height = 0;
    char format[64] = {};
    int bpp = 0;
    int bpc = 0;

    const int result = ExtractDdsTextureInfo(ddsPath.c_str(), &width, &height, format, &bpp, &bpc);

    EXPECT_EQ(result, 0);
    EXPECT_EQ(width, 2);
    EXPECT_EQ(height, 3);
    EXPECT_STREQ(format, "DXGI_FORMAT_R8G8B8A8_UNORM");
    EXPECT_EQ(bpp, 32);
    EXPECT_EQ(bpc, 8);
}

TEST_F(DirectXTexWrapperTest, ExtractData_UncompressedRgba8_CopiesExactPixelsToRawAndPreview) {
    // 2x2 RGBA8 image with distinct pixel values
    const std::vector<uint8_t> expected = {
        255, 0,   0,   255, // Red
        0,   255, 0,   255, // Green
        0,   0,   255, 255, // Blue
        255, 255, 0,   128  // Semi-transparent yellow
    };
    const std::string ddsPath = CreateRgba8Dds("pixels.dds", 2, 2, expected);

    std::vector<uint8_t> raw(expected.size(), 0);
    std::vector<uint8_t> preview(expected.size(), 0);

    const int result = ExtractDdsTextureData(ddsPath.c_str(), static_cast<int>(raw.size()),
                                             static_cast<int>(preview.size()), raw.data(), preview.data());

    EXPECT_EQ(result, 0);
    EXPECT_EQ(raw, expected);
    EXPECT_EQ(preview, expected);
}

TEST_F(DirectXTexWrapperTest, ExtractData_UncompressedBgra8_ConvertsPreviewToRgba8) {
    // Single 1x1 pixel in BGRA order: Blue=10, Green=20, Red=200, Alpha=255
    const std::vector<uint8_t> bgra = {10, 20, 200, 255};
    const std::string ddsPath = CreateBgra8Dds("bgra8.dds", 1, 1, bgra);

    int width = 0, height = 0, bpp = 0, bpc = 0;
    char format[64] = {};
    ASSERT_EQ(ExtractDdsTextureInfo(ddsPath.c_str(), &width, &height, format, &bpp, &bpc), 0);
    EXPECT_STREQ(format, "DXGI_FORMAT_B8G8R8A8_UNORM");
    EXPECT_EQ(bpp, 32);
    EXPECT_EQ(bpc, 8);

    std::vector<uint8_t> raw(4, 0);
    std::vector<uint8_t> preview(4, 0);
    ASSERT_EQ(ExtractDdsTextureData(ddsPath.c_str(), 4, 4, raw.data(), preview.data()), 0);

    // Raw preserves BGRA; preview is converted to RGBA
    EXPECT_EQ(raw, bgra);
    const std::vector<uint8_t> expectedRgba = {200, 20, 10, 255};
    EXPECT_EQ(preview, expectedRgba);
}

TEST_F(DirectXTexWrapperTest, ExtractInfoAndData_CompressedBc1_DecompressesToRgba8) {
    const std::string ddsPath = CreateBc1CompressedDds("bc1.dds");

    int width = 0, height = 0, bpp = 0, bpc = 0;
    char format[64] = {};
    ASSERT_EQ(ExtractDdsTextureInfo(ddsPath.c_str(), &width, &height, format, &bpp, &bpc), 0);
    EXPECT_EQ(width, 4);
    EXPECT_EQ(height, 4);
    EXPECT_STREQ(format, "DXGI_FORMAT_BC1_UNORM");
    // Compressed formats report 32 bpp / 8 bpc because they decompress to R8G8B8A8_UNORM
    EXPECT_EQ(bpp, 32);
    EXPECT_EQ(bpc, 8);

    std::vector<uint8_t> raw(4 * 4 * 4, 0);
    std::vector<uint8_t> preview(4 * 4 * 4, 0);
    ASSERT_EQ(ExtractDdsTextureData(ddsPath.c_str(), static_cast<int>(raw.size()),
                                    static_cast<int>(preview.size()), raw.data(), preview.data()),
              0);

    // Decompressed solid red block should have high red channel and full alpha
    EXPECT_GT(raw[0], 240);
    EXPECT_LT(raw[1], 15);
    EXPECT_LT(raw[2], 15);
    EXPECT_EQ(raw[3], 255);
    EXPECT_EQ(raw, preview);
}

TEST_F(DirectXTexWrapperTest, ExtractInfoAndData_MissingFile_ReturnsNonZeroError) {
    const std::string missingPath = (tempDir / "does_not_exist.dds").string();

    int width = 0, height = 0, bpp = 0, bpc = 0;
    char format[64] = {};
    EXPECT_NE(ExtractDdsTextureInfo(missingPath.c_str(), &width, &height, format, &bpp, &bpc), 0);

    std::vector<uint8_t> buf(16, 0);
    EXPECT_NE(ExtractDdsTextureData(missingPath.c_str(), 16, 16, buf.data(), buf.data()), 0);
}

TEST_F(DirectXTexWrapperTest, ExtractInfoAndData_CorruptFile_ReturnsNonZeroError) {
    const auto corruptPath = tempDir / "corrupt.dds";
    {
        std::ofstream out(corruptPath, std::ios::binary);
        out << "NOT_A_VALID_DDS_FILE";
    }

    int width = 0, height = 0, bpp = 0, bpc = 0;
    char format[64] = {};
    EXPECT_NE(ExtractDdsTextureInfo(corruptPath.string().c_str(), &width, &height, format, &bpp, &bpc), 0);

    std::vector<uint8_t> buf(64, 0);
    EXPECT_NE(ExtractDdsTextureData(corruptPath.string().c_str(), 64, 64, buf.data(), buf.data()), 0);
}

TEST_F(DirectXTexWrapperTest, ExtractData_InsufficientBufferSizes_ReturnsMinusOne) {
    const std::vector<uint8_t> bgra = {10, 20, 200, 255, 10, 20, 200, 255,
                                       10, 20, 200, 255, 10, 20, 200, 255};
    const std::string ddsPath = CreateBgra8Dds("buffer_check.dds", 2, 2, bgra);

    std::vector<uint8_t> raw(16, 0);
    std::vector<uint8_t> preview(16, 0);

    // Raw buffer too small
    EXPECT_EQ(ExtractDdsTextureData(ddsPath.c_str(), 4, 16, raw.data(), preview.data()), -1);

    // Preview buffer too small (triggered when format != R8G8B8A8_UNORM)
    EXPECT_EQ(ExtractDdsTextureData(ddsPath.c_str(), 16, 4, raw.data(), preview.data()), -1);
}

} // namespace
