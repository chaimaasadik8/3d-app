/*
 * Copyright 2017 Google Inc. All Rights Reserved.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
#include "camera_utility.h"
#include "texture_reader.h"

namespace {
static bool gTextureReaderInitialized = false;
static TextureReader* gTextureReader = nullptr;
};  // namespace

#ifdef __cplusplus
extern "C" {
#endif

void TextureReader_create(int format, int width, int height,
                          bool keepAspectRatio) {
  if (gTextureReaderInitialized) {
    TextureReader_destroy();
  }

  gTextureReader = new TextureReader();
  gTextureReader->create(format, width, height, keepAspectRatio);
  gTextureReaderInitialized = true;
}

void TextureReader_destroy() {
  if (gTextureReaderInitialized) {
    gTextureReader->destroy();
    delete gTextureReader;

    gTextureReader = nullptr;
    gTextureReaderInitialized = false;
  }
}

int TextureReader_submitFrame(int textureId, int textureWidth,
                              int textureHeight) {
  return gTextureReader->submitFrame(textureId, textureWidth, textureHeight);
}

unsigned char* TextureReader_acquireFrame(int bufferIndex, int* bufferSize) {
  return gTextureReader->acquireFrame(bufferIndex, bufferSize);
}

void TextureReader_releaseFrame(int bufferIndex) {
  return gTextureReader->releaseFrame(bufferIndex);
}

unsigned char* TextureReader_submitAndAcquire(int textureId, int textureWidth,
                                              int textureHeight,
                                              int* bufferSize) {
  return gTextureReader->submitAndAcquire(textureId, textureWidth,
                                          textureHeight, bufferSize);
}

#ifdef __cplusplus
}
#endif
