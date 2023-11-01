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
#include <android/log.h>
#include <cstring>
#include <stdexcept>

#include "camera_utility.h"
#include "gl_utility.h"
#include "texture_reader.h"

namespace {
static const int COORDS_PER_VERTEX = 3;
static const int TEXCOORDS_PER_VERTEX = 2;
static const int FLOAT_SIZE = 4;
static const GLfloat QUAD_COORDS[16] = {
    -1.0f, -1.0f, 0.0f, -1.0f, +1.0f, 0.0f,
    +1.0f, -1.0f, 0.0f, +1.0f, +1.0f, 0.0f,
};

static const GLfloat QUAD_TEXCOORDS[8] = {
    0.0f, 0.0f, 0.0f, 1.0f, 1.0f, 0.0f, 1.0f, 1.0f,
};
static const char* QUAD_RENDERING_VERTEX_SHADER =
    "attribute vec4 a_Position;\n"
    "attribute vec2 a_TexCoord;\n"
    "varying vec2 v_TexCoord;\n"
    "void main() {\n"
    "   gl_Position = a_Position;\n"
    "   v_TexCoord = a_TexCoord;\n"
    "}";

static const char* QUAD_RENDERING_FRAGMENT_SHADER_RGBA =
    "#extension GL_OES_EGL_image_external : require\n"
    "precision mediump float;\n"
    "varying vec2 v_TexCoord;\n"
    "uniform samplerExternalOES sTexture;\n"
    "void main() {\n"
    "    gl_FragColor = texture2D(sTexture, v_TexCoord);\n"
    "}";

static const char* QUAD_RENDERING_FRAGMENT_SHADER_I8 =
    "#extension GL_OES_EGL_image_external : require\n"
    "precision mediump float;\n"
    "varying vec2 v_TexCoord;\n"
    "uniform samplerExternalOES sTexture;\n"
    "void main() {\n"
    "    vec4 color = texture2D(sTexture, v_TexCoord);\n"
    "    gl_FragColor.r = "
    "        color.r * 0.299 + color.g * 0.587 + color.b * 0.114;\n"
    "}";
};  // namespace

TextureReader::TextureReader() { mPixelBuffer = nullptr; }
TextureReader::~TextureReader() {}

void TextureReader::create(int format, int width, int height,
                           bool keepAspectRatio) {
  if (format != IMAGE_FORMAT_RGBA && format != IMAGE_FORMAT_I8) {
    LOGE("Image format not supported. Set to default format: IMAGE_FORMAT_I8");
    format = IMAGE_FORMAT_I8;
  }

  mKeepAspectRatio = keepAspectRatio;
  mPixelFormat = format;
  mImageWidth = width;
  mImageHeight = height;
  mFrontIndex = -1;
  mBackIndex = -1;

  if (mPixelFormat == IMAGE_FORMAT_RGBA) {
    mPixelBufferSize = mImageWidth * mImageHeight * 4;
  } else if (mPixelFormat == IMAGE_FORMAT_I8) {
    mPixelBufferSize = mImageWidth * mImageHeight;
  }

  glGenFramebuffers(BUFFER_COUNT, mFrameBuffer);
  glGenTextures(BUFFER_COUNT, mTexture);
  glGenBuffers(BUFFER_COUNT, mPBO);

  for (int i = 0; i < BUFFER_COUNT; i++) {
    mBufferUsed[i] = false;

    glBindFramebuffer(GL_FRAMEBUFFER, mFrameBuffer[i]);

    glBindTexture(GL_TEXTURE_2D, mTexture[i]);
    glTexImage2D(GL_TEXTURE_2D, 0,
                 mPixelFormat == IMAGE_FORMAT_I8 ? GL_R8 : GL_RGBA, mImageWidth,
                 mImageHeight, 0,
                 mPixelFormat == IMAGE_FORMAT_I8 ? GL_RED : GL_RGBA,
                 GL_UNSIGNED_BYTE, 0);

    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
    glFramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_TEXTURE_2D,
                           mTexture[i], 0);

    int status = glCheckFramebufferStatus(GL_FRAMEBUFFER);
    if (status != GL_FRAMEBUFFER_COMPLETE) {
      LOGE("Failed to create OpenGL frame buffer.");
    }

    glBindBuffer(GL_PIXEL_PACK_BUFFER, mPBO[i]);
    glBufferData(GL_PIXEL_PACK_BUFFER, mPixelBufferSize, 0, GL_DYNAMIC_READ);
    glBindBuffer(GL_PIXEL_PACK_BUFFER, 0);
  }

  // Load shader program.
  mQuadProgram = CreateProgram(QUAD_RENDERING_VERTEX_SHADER,
                               mPixelFormat == IMAGE_FORMAT_I8
                                   ? QUAD_RENDERING_FRAGMENT_SHADER_I8
                                   : QUAD_RENDERING_FRAGMENT_SHADER_RGBA);

  glUseProgram(mQuadProgram);

  mQuadPositionAttrib = glGetAttribLocation(mQuadProgram, "a_Position");
  mQuadTexCoordAttrib = glGetAttribLocation(mQuadProgram, "a_TexCoord");
  mQuadTextureUniform = glGetUniformLocation(mQuadProgram, "sTexture");
}

void TextureReader::destroy() {
  glDeleteFramebuffers(BUFFER_COUNT, mFrameBuffer);
  glDeleteTextures(BUFFER_COUNT, mTexture);
  glDeleteBuffers(BUFFER_COUNT, mPBO);

  if (mPixelBuffer != nullptr) {
    delete[] mPixelBuffer;
    mPixelBuffer = nullptr;
  }
}

int TextureReader::getBufferCount() { return BUFFER_COUNT; }

int TextureReader::submitFrame(int textureId, int textureWidth,
                               int textureHeight) {
  // Find next buffer.
  int bufferIndex = -1;
  for (int i = 0; i < BUFFER_COUNT; i++) {
    if (!mBufferUsed[i]) {
      bufferIndex = i;
      break;
    }
  }

  if (bufferIndex < 0) {
    LOGE("No buffer available.");
    return -1;
  }

  mBufferUsed[bufferIndex] = true;

  // Bind both read and write to framebuffer.
  glBindFramebuffer(GL_FRAMEBUFFER, mFrameBuffer[bufferIndex]);

  // Save and setup viewport
  GLint viewport[4];
  glGetIntegerv(GL_VIEWPORT, viewport);
  glViewport(0, 0, mImageWidth, mImageHeight);

  // Draw texture to framebuffer.
  drawTexture(textureId, textureWidth, textureHeight);

  // Start reading into PBO
  glBindBuffer(GL_PIXEL_PACK_BUFFER, mPBO[bufferIndex]);

  // glReadBuffer(GL_COLOR_ATTACHMENT0);
  glReadPixels(0, 0, mImageWidth, mImageHeight,
               mPixelFormat == IMAGE_FORMAT_I8 ? GL_RED : GL_RGBA,
               GL_UNSIGNED_BYTE, 0);

  // Restore viewport.
  glViewport(viewport[0], viewport[1], viewport[2], viewport[3]);

  glBindFramebuffer(GL_FRAMEBUFFER, 0);
  glBindBuffer(GL_PIXEL_PACK_BUFFER, 0);
  return bufferIndex;
}

unsigned char* TextureReader::acquireFrame(int bufferIndex, int* bufferSize) {
  if (bufferIndex < 0 || bufferIndex >= BUFFER_COUNT ||
      !mBufferUsed[bufferIndex]) {
    LOGE("Invalid buffer index.");
    return nullptr;
  }

  if (bufferSize != nullptr) {
    *bufferSize = mPixelBufferSize;
  }

  // Bind the current PB and acquire the pixel buffer.
  glBindBuffer(GL_PIXEL_PACK_BUFFER, mPBO[bufferIndex]);
  unsigned char* pixelBuffer = (unsigned char*)glMapBufferRange(
      GL_PIXEL_PACK_BUFFER, 0, mPixelBufferSize, GL_MAP_READ_BIT);
  if (pixelBuffer == nullptr && bufferSize != nullptr) {
    *bufferSize = 0;
  }
  glBindBuffer(GL_PIXEL_PACK_BUFFER, 0);

  return pixelBuffer;
}

void TextureReader::releaseFrame(int bufferIndex) {
  if (bufferIndex < 0 || bufferIndex >= BUFFER_COUNT ||
      !mBufferUsed[bufferIndex]) {
    LOGE("Invalid buffer index.");
    return;
  }

  glBindBuffer(GL_PIXEL_PACK_BUFFER, mPBO[bufferIndex]);
  glUnmapBuffer(GL_PIXEL_PACK_BUFFER);
  glBindBuffer(GL_PIXEL_PACK_BUFFER, 0);

  mBufferUsed[bufferIndex] = false;
}

void TextureReader::drawTexture(int textureId, int textureWidth,
                                int textureHeight) {
  // Disable features that we don't use.
  glDisable(GL_DEPTH_TEST);
  glDisable(GL_CULL_FACE);
  glDisable(GL_SCISSOR_TEST);
  glDisable(GL_STENCIL_TEST);
  glDisable(GL_BLEND);
  glDepthMask(GL_FALSE);
  glBindBuffer(GL_ARRAY_BUFFER, 0);
  glBindBuffer(GL_ELEMENT_ARRAY_BUFFER, 0);
  glBindVertexArray(0);

  // Clear buffers.
  glClearColor(0, 0, 0, 0);
  glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);

  glUseProgram(mQuadProgram);

  // Set the vertex positions.
  glVertexAttribPointer(mQuadPositionAttrib, COORDS_PER_VERTEX, GL_FLOAT, false,
                        0, QUAD_COORDS);

  // Set the texture coordinates.
  if (mKeepAspectRatio) {
    int renderWidth = 0;
    int renderHeight = 0;
    float textureAspectRatio = static_cast<float>(textureWidth) / textureHeight;
    float imageAspectRatio = static_cast<float>(mImageWidth) / mImageHeight;
    if (textureAspectRatio < imageAspectRatio) {
      renderWidth = mImageWidth;
      renderHeight = textureHeight * mImageWidth / textureWidth;
    } else {
      renderWidth = textureWidth * mImageHeight / textureHeight;
      renderHeight = mImageHeight;
    }
    float offsetU =
        static_cast<float>(renderWidth - mImageWidth) / renderWidth / 2;
    float offsetV =
        static_cast<float>(renderHeight - mImageHeight) / renderHeight / 2;

    GLfloat texCoords[] = {
      offsetU, offsetV,
      offsetU, 1 - offsetV,
      1 - offsetU, offsetV,
      1 - offsetU, 1 - offsetV
    };

    glVertexAttribPointer(mQuadTexCoordAttrib, TEXCOORDS_PER_VERTEX, GL_FLOAT,
                          false, 0, texCoords);
  } else {
    glVertexAttribPointer(mQuadTexCoordAttrib, TEXCOORDS_PER_VERTEX, GL_FLOAT,
                          false, 0, QUAD_TEXCOORDS);
  }

  // Enable vertex arrays
  glEnableVertexAttribArray(mQuadPositionAttrib);
  glEnableVertexAttribArray(mQuadTexCoordAttrib);

  // Select input texture.
  glUniform1i(mQuadTextureUniform, 0);
  glActiveTexture(GL_TEXTURE0);
  glBindTexture(GL_TEXTURE_EXTERNAL_OES, textureId);

  // Draw a quad with texture.
  glDrawArrays(GL_TRIANGLE_STRIP, 0, 4);

  // Disable vertex arrays
  glDisableVertexAttribArray(mQuadPositionAttrib);
  glDisableVertexAttribArray(mQuadTexCoordAttrib);

  // Reset texture binding.
  glBindTexture(GL_TEXTURE_EXTERNAL_OES, 0);
}

unsigned char* TextureReader::submitAndAcquire(int textureId, int textureWidth,
                                               int textureHeight,
                                               int* bufferSize) {
  // Release previously used front buffer.
  if (mFrontIndex != -1) {
    releaseFrame(mFrontIndex);
  }

  // Move previous back buffer to front buffer.
  mFrontIndex = mBackIndex;

  // Submit new request on back buffer.
  mBackIndex = submitFrame(textureId, textureWidth, textureHeight);

  // Acquire frame from the new front buffer.
  if (mFrontIndex != -1) {
    return acquireFrame(mFrontIndex, bufferSize);
  }

  return nullptr;
}
