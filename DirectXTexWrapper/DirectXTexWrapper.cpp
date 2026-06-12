#include <DirectXTex.h>
#include <string>

#define EXPORT __attribute__((visibility("default")))

extern "C" {
    EXPORT bool LoadDDSTexture(const char* filePath, int* outWidth, int* outHeight, int* outFormat) {
        DirectX::ScratchImage image;
        std::wstring wFilePath = std::wstring(filePath, filePath + strlen(filePath));
        
        HRESULT hr = DirectX::LoadFromDDSFile(wFilePath.c_str(), DirectX::DDS_FLAGS_NONE, nullptr, image);
        
        if (SUCCEEDED(hr)) {
            const DirectX::TexMetadata& metadata = image.GetMetadata();
            *outWidth = static_cast<int>(metadata.width);
            *outHeight = static_cast<int>(metadata.height);
            *outFormat = static_cast<int>(metadata.format);
            return true;
        }
        return false;
    }
}