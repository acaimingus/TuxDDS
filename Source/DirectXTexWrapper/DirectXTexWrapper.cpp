#include <DirectXTex.h>
#include <string>
#include <magic_enum/magic_enum.hpp>

#define EXPORT __attribute__((visibility("default")))

extern "C"
{
    EXPORT int ExtractDdsTextureInfo(const char *filePath, int *outWidth, int *outHeight, char *outImageFormat, int *outBitsPerPixel, int *outBitsPerColor)
    {
        // Create the needed objects for loading the DDS file
        DirectX::ScratchImage image;
        std::wstring wideFilePath = std::wstring(filePath, filePath + strlen(filePath));

        // Load the DDS file
        HRESULT hr = DirectX::LoadFromDDSFile(wideFilePath.c_str(), DirectX::DDS_FLAGS_NONE, nullptr, image);

        if (SUCCEEDED(hr))
        {
            // Get the metadata
            const DirectX::TexMetadata &metadata = image.GetMetadata();

            // Set image size and format info
            *outWidth = static_cast<int>(metadata.width);
            *outHeight = static_cast<int>(metadata.height);

            // Extract the format name using magic_enum
            int bufferSize = 64;
            auto formatName = magic_enum::enum_name(metadata.format);
            strncpy(outImageFormat, formatName.data(), bufferSize - 1);
            outImageFormat[bufferSize - 1] = '\0';

            // Extract the amount of bits needed per pixel and per color channel
            if (DirectX::IsCompressed(metadata.format))
            {
                *outBitsPerPixel = 32;
                *outBitsPerColor = 8;
            }
            else
            {
                *outBitsPerPixel = static_cast<int>(DirectX::BitsPerPixel(metadata.format));
                *outBitsPerColor = static_cast<int>(DirectX::BitsPerColor(metadata.format));
            }

            // Successfully finished
            return 0;
        }
        // Error occured; return the HRESULT error code
        return static_cast<int>(hr);
    }

    EXPORT int ExtractDdsTextureData(const char *filePath, int rawBufferSize, int previewBufferSize, uint8_t *outRawImageData, uint8_t *outPreviewData)
    {
        // Create the needed scratch images for loading the DDS file
        DirectX::ScratchImage compressedScratchImage;
        DirectX::ScratchImage decompressedScratchImage;
        DirectX::ScratchImage previewScratchImage;

        // Create a wide string for the file path
        std::wstring wideFilePath = std::wstring(filePath, filePath + strlen(filePath));

        // Load the DDS file
        HRESULT hr = DirectX::LoadFromDDSFile(wideFilePath.c_str(), DirectX::DDS_FLAGS_NONE, nullptr, compressedScratchImage);

        if (SUCCEEDED(hr))
        {
            // Get the metadata
            const DirectX::TexMetadata &metadata = compressedScratchImage.GetMetadata();

            // Create objects for the images
            const DirectX::Image *rawImage = nullptr;
            const DirectX::Image *previewImage = nullptr;

            // Check if the image is compressed
            if (DirectX::IsCompressed(metadata.format))
            {
                // Decompress the image using R8G8B8A8
                HRESULT hr2 = DirectX::Decompress(compressedScratchImage.GetImages(), compressedScratchImage.GetImageCount(), metadata, DXGI_FORMAT_R8G8B8A8_UNORM, decompressedScratchImage);
                if (FAILED(hr2))
                {
                    // Set output pointer to null on failure
                    outRawImageData = nullptr;
                    outPreviewData = nullptr;
                    return static_cast<int>(hr2);
                }

                // Get the image data itself
                rawImage = decompressedScratchImage.GetImage(0, 0, 0);
            }
            else
            {
                // Image is not compressed, get the image data directly
                rawImage = compressedScratchImage.GetImage(0, 0, 0);

                // Image might be in an exotic format not possible to display in the UI, generate a R8G8B8A8 version for the UI
                if (metadata.format != DXGI_FORMAT_R8G8B8A8_UNORM)
                {
                    // Convert the image to R8G8B8A8
                    HRESULT hr2 = DirectX::Convert(*rawImage, DXGI_FORMAT_R8G8B8A8_UNORM, DirectX::TEX_FILTER_DEFAULT, DirectX::TEX_THRESHOLD_DEFAULT, previewScratchImage);
                    if (FAILED(hr2))
                    {
                        // Set output pointer to null on failure
                        outRawImageData = nullptr;
                        outPreviewData = nullptr;
                        return static_cast<int>(hr2);
                    }

                    previewImage = previewScratchImage.GetImage(0, 0, 0);
                }
            }

            // Get the image size
            size_t imageSize = rawImage->slicePitch;

            // Check if the provided buffer is large enough to hold the image data
            if (static_cast<size_t>(rawBufferSize) < imageSize)
            {
                outRawImageData = nullptr;
                outPreviewData = nullptr;
                return -1;
            }

            // Get the actual image data
            uint8_t *rawImageData = rawImage->pixels;

            // Copy the image data to the output pointer
            memcpy(outRawImageData, rawImageData, imageSize);

            if (previewImage != nullptr)
            {
                size_t previewImageSize = previewImage->slicePitch;

                // Check if the provided buffer is large enough to hold the image data
                if (static_cast<size_t>(previewBufferSize) < previewImageSize)
                {
                    outRawImageData = nullptr;
                    outPreviewData = nullptr;
                    return -1;
                }

                // Copy the converted data to the preview pointer
                uint8_t *previewImageData = previewImage->pixels;
                memcpy(outPreviewData, previewImageData, previewImageSize);
            }
            else
            {
                // Copy the image data also to the preview pointer
                memcpy(outPreviewData, rawImageData, imageSize);
            }

            // Successfully finished
            return 0;
        }
        // Error occured; return the HRESULT error code
        return static_cast<int>(hr);
    }
}