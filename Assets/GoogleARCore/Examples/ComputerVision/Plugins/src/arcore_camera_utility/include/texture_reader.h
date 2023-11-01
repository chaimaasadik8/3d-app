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

#ifndef THIRD_PARTY_REDWOOD_PLATFORM_SDK_ARCORE_CAMERA_UTILITY_INCLUDE_TEXTURE_READER_H_
#define THIRD_PARTY_REDWOOD_PLATFORM_SDK_ARCORE_CAMERA_UTILITY_INCLUDE_TEXTURE_READER_H_

#include <GLES3/gl3.h>

/**
 * Texture reader using PBO.
 */
class TextureReader {
 public:
  TextureReader();
  ~TextureReader();

  /**
   * Creates the texture reader. This function needs to be called from the
   * OpenGL rendering thread.
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
   *
   * Exception is thrown if failed to create OpenGL frame buffers.
   */
  void create(int format, int width, int height, bool keepAspectRatio);

  /** Destroy the texture reader. */
  void destroy();

  /**
   * Submits a frame reading request. This routine does not return the result
   * frame buffer immediately. Instead, it returns a frame buffer index, which
   * can be used to acquire the frame buffer later through acquireFrame().
   *
   * @param textureId the id of the input OpenGL texture.
   * @param textureWidth width of the texture in pixels.
   * @param textureHeight height of the texture in pixels.
   * @return the index to the frame buffer this request is associated to.
   *   This index can be used to acquire the frame later through acquireFrame();
   *   You should release the frame buffer using releaseBuffer() routine after
   *   finish using of the frame.
   *
   * Exception is thrown if there is no enough buffer.
   */
  int submitFrame(int textureId, int textureWidth, int textureHeight);

  /**
   * Acquires the frame requested earlier through submitFrame().
   * This routine returns an image buffer that contains the pixels mapped to the
   * frame buffer requested previously through submitFrame().
   *
   * @param bufferIndex the index to the frame buffer to be acquired. It has to
   *   be a frame index returned from submitFrame().
   * @return an image buffer that contains the pixels of the output image if
   *   succeed. Null otherwise.
   *
   * Exception is thrown if invalid buffer index if provided.
   */
  unsigned char* acquireFrame(int bufferIndex, int* bufferSize);

  /**
   * Release a previously requested frame buffer.
   *
   * @param bufferIndex the index to the frame buffer to be acquired. It has to
   *   be a frame index returned from submitFrame().
   *
   * Exception is thrown if invalid buffer index if provided.
   */
  void releaseFrame(int bufferIndex);

  /**
   * Reads pixels using dual buffers. This function sends the reading request to
   * GPU and returns the result from the previous call. Thus, the first call
   * always returns null. The returned image buffer maps to the internal buffer,
   * which cannot be overwrote. The returned buffer becomes invalid after next
   * call to submitAndAcquire().
   *
   * There is no need to release the returned buffer.
   *
   * @param textureId the OpenGL texture Id.
   * @param textureWidth width of the texture in pixels.
   * @param textureHeight height of the texture in pixels.
   * @return an image buffer that contains the pixels of the output image if
   *   succeed. Null otherwise.
   */
  unsigned char* submitAndAcquire(int textureId, int textureWidth, int textureHeight,
                         int* bufferSize);

  /**
   * Gets the internal buffer count.
   * @return the internal buffer count.
   */
  static int getBufferCount();

 private:
  /**
   * Renders the input OpenGL texture to internal buffer.
   *
   * @param textureId the input OpenGL texture Id.
   * @param textureWidth width of the texture in pixels.
   * @param textureHeight height of the texture in pixels.
   */
  void drawTexture(int textureId, int textureWidth, int textureHeight);

  static const int BUFFER_COUNT = 2;
  GLuint mFrameBuffer[BUFFER_COUNT];
  GLuint mTexture[BUFFER_COUNT];
  GLuint mPBO[BUFFER_COUNT];
  bool mBufferUsed[BUFFER_COUNT];
  unsigned char* mPixelBuffer;

  int mPixelFormat = 0;
  int mImageWidth = 0;
  int mImageHeight = 0;
  int mPixelBufferSize = 0;
  int mFrontIndex = -1;
  int mBackIndex = -1;
  bool mKeepAspectRatio = false;

  int mQuadProgram;
  int mQuadPositionAttrib;
  int mQuadTexCoordAttrib;
  int mQuadTextureUniform;
};
#endif  // THIRD_PARTY_REDWOOD_PLATFORM_SDK_ARCORE_CAMERA_UTILITY_INCLUDE_TEXTURE_READER_H_
