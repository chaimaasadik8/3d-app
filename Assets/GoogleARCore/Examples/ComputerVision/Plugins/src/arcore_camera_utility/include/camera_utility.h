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
#ifndef CAMERA_UTILITY_H_
#define CAMERA_UTILITY_H_

#ifdef __cplusplus
extern "C" {
#endif

/**
 * Image format definitions.
 */
#define IMAGE_FORMAT_RGBA 0  // Image format for RGBA8888.
#define IMAGE_FORMAT_I8 1    // Image format for Grayscale.

/**
 * Creates the texture reader. This function needs to be called from the OpenGL
 * rendering thread.
 *
 * @param format the format of the output pixel buffer. It can be one of the
 *   two values:
 *   IMAGE_FORMAT_RGBA or IMAGE_FORMAT_I8.
 *
 * @param width the width of the output image.
 * @param height the height of the output image.
 * @param keepAspectRatio whether or not to keep aspect ratio. If true, the
 *   output image may be cropped if the image aspect ratio is different from
 *   the texture aspect ratio. If false, the output image covers the entire
 *   texture scope and no cropping is applied.
 */
void TextureReader_create(int format, int width, int height,
                          bool keepAspectRatio);

/** Destroy the texture reader. */
void TextureReader_destroy();

/**
 * Submits a frame reading request. This routine does not return the result
 * frame buffer immediately. Instead, it returns a frame buffer index, which
 * can be used to acquire the frame buffer later through
 * TextureReader_acquireFrame().
 *
 * @param textureId the id of the input OpenGL texture.
 * @param textureWidth width of the texture in pixels.
 * @param textureHeight height of the texture in pixels.
 * @return the index to the frame buffer this request is associated to.
 *   This index can be used to acquire the frame later through
 *   TextureReader_acquireFrame();
 *   You should release the frame buffer using TextureReader_releaseBuffer()
 *   routine after finish using of the frame.
 *
 * Exception is thrown if there is no enough buffer.
 */
int TextureReader_submitFrame(int textureId, int textureWidth,
                              int textureHeight);

/**
 * Acquires the frame requested earlier through TextureReader_submitFrame().
 * This routine returns an image buffer that contains the pixels mapped to the
 * frame buffer requested previously through TextureReader_submitFrame().
 *
 * @param bufferIndex the index to the frame buffer to be acquired. It has to
 *   be a frame index returned from TextureReader_submitFrame().
 * @return an image buffer that contains the pixels of the output image if
 *   succeed. Null otherwise.
 *
 * Exception is thrown if invalid buffer index if provided.
 */
unsigned char* TextureReader_acquireFrame(int bufferIndex, int* bufferSize);

/**
 * Release a previously requested frame buffer.
 *
 * @param bufferIndex the index to the frame buffer to be acquired. It has to
 *   be a frame index returned from TextureReader_submitFrame().
 *
 * Exception is thrown if invalid buffer index if provided.
 */
void TextureReader_releaseFrame(int bufferIndex);

/**
 * Reads pixels using dual buffers. This function sends the reading request to
 * GPU and returns the result from the previous call. Thus, the first call
 * always returns null. The returned image buffer maps to the internal buffer,
 * which cannot be overwrote. The returned buffer becomes invalid after next
 * call to TextureReader_submitAndAcquire().
 *
 * There is no need to release the returned buffer.
 *
 * @param textureId the OpenGL texture Id.
 * @param textureWidth width of the texture in pixels.
 * @param textureHeight height of the texture in pixels.
 * @return an image buffer that contains the pixels of the output image if
 *   succeed. Null otherwise.
 */
unsigned char* TextureReader_submitAndAcquire(int textureId, int textureWidth,
                                              int textureHeight,
                                              int* bufferSize);

#ifdef __cplusplus
}
#endif

#endif  // CAMERA_UTILITY_H_
