/*
 * Copyright (c) 2026 Adam Martula
 * This source code is licensed under the MIT license found in the LICENSE file in the root of this source tree.
 */

#pragma once

#include <cstdint>

#define EXPORT __attribute__((visibility("default")))

#ifdef __cplusplus
extern "C" {
#endif

EXPORT int ExtractDdsTextureInfo(const char *filePath, int *outWidth, int *outHeight, char *outImageFormat,
                                 int *outBitsPerPixel, int *outBitsPerColor);

EXPORT int ExtractDdsTextureData(const char *filePath, int rawBufferSize, int previewBufferSize,
                                 uint8_t *outRawImageData, uint8_t *outPreviewData);

#ifdef __cplusplus
}
#endif
