#include <DirectXTex.h>
#include <string>
#include <magic_enum/magic_enum.hpp>

#define EXPORT __attribute__((visibility("default")))

extern "C" {
    EXPORT int ExtractDdsTextureInfo(const char* filePath, int* outWidth, int* outHeight, char* outImageFormat, int* outBytesPerPixel)
    {
        // Create the needed objects for loading the DDS file
        DirectX::ScratchImage image;
        std::wstring wideFilePath = std::wstring(filePath, filePath + strlen(filePath));

        // Load the DDS file
        HRESULT hr = DirectX::LoadFromDDSFile(wideFilePath.c_str(), DirectX::DDS_FLAGS_NONE, nullptr, image);

        if (SUCCEEDED(hr))
        {
            // Get the metadata
            const DirectX::TexMetadata& metadata = image.GetMetadata();
            
            // Set image size and format info
            *outWidth = static_cast<int>(metadata.width);
            *outHeight = static_cast<int>(metadata.height);

            // Extract the format name using magic_enum
            int bufferSize = 64;
            auto formatName = magic_enum::enum_name(metadata.format);
            strncpy(outImageFormat, formatName.data(), bufferSize - 1);
            outImageFormat[bufferSize - 1] = '\0';
            
            // Extract the amount of bytes needed per pixel
            if (DirectX::IsCompressed(metadata.format))
            {
                *outBytesPerPixel = 4;
            }
            else
            {
                size_t bitsPerPixel = DirectX::BitsPerPixel(metadata.format);
                *outBytesPerPixel = static_cast<int>(bitsPerPixel / 8);
            }

            // Successfully finished
            return 0;
        }
        // Error occured; return the HRESULT error code
        return static_cast<int>(hr);
    }

    EXPORT int ExtractDdsTextureData(const char* filePath, int bufferSize, uint8_t* outImageData)
    {
        // Create the needed objects for loading the DDS file
        DirectX::ScratchImage compressedImage;
        std::wstring wideFilePath = std::wstring(filePath, filePath + strlen(filePath));

        // Load the DDS file
        HRESULT hr = DirectX::LoadFromDDSFile(wideFilePath.c_str(), DirectX::DDS_FLAGS_NONE, nullptr, compressedImage);

        if (SUCCEEDED(hr))
        {
            // Get the metadata
            const DirectX::TexMetadata& metadata = compressedImage.GetMetadata();

            const DirectX::Image* ddsImage = nullptr;

            // Create the scratch image for the decompressed image
            DirectX::ScratchImage decompressedImage;

            // Check if the image is compressed
            if (DirectX::IsCompressed(metadata.format))
            {
                // Decompress the image
                HRESULT hr2 = DirectX::Decompress(compressedImage.GetImages(), compressedImage.GetImageCount(), metadata, DXGI_FORMAT_R8G8B8A8_UNORM, decompressedImage);
                if (FAILED(hr2))
                {
                    // Set output pointer to null on failure
                    outImageData = nullptr;
                    return static_cast<int>(hr2);
                }
                // Get the image data itself
                ddsImage = decompressedImage.GetImage(0,0,0);
            }
            else
            {
                // Image is not compressed, get the image data directly
                ddsImage = compressedImage.GetImage(0,0,0);
            }

            // Get the image size
            size_t imageSize = ddsImage->slicePitch;
            
            // Check if the provided buffer is large enough to hold the image data
            if (static_cast<size_t>(bufferSize) < imageSize)
            {
                outImageData = nullptr;
                return -1;
            }

            // Get the actual image data
            uint8_t* imageData = ddsImage->pixels;

            // Copy the image data to the output pointer
            memcpy(outImageData, imageData, imageSize);

            // Successfully finished
            return 0;
        }
        // Error occured; return the HRESULT error code
        return static_cast<int>(hr);
    }
}