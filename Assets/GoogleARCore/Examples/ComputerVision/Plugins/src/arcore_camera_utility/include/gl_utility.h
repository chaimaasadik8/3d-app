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
#ifndef GL_UTILITY_H_
#define GL_UTILITY_H_

#include <android/log.h>
#include <GLES3/gl3.h>
#include <GLES2/gl2ext.h>

#ifdef __cplusplus
extern "C" {
#endif

#ifndef LOGE
#define LOGI(...) \
  __android_log_print(ANDROID_LOG_INFO, "camera_utility", __VA_ARGS__)
#define LOGE(...) \
  __android_log_print(ANDROID_LOG_ERROR, "camera_utility", __VA_ARGS__)
#endif

void CheckGlError(const char* operation);
GLuint LoadShader(GLenum shader_type, const char* shader_source);
GLuint CreateProgram(const char* vertex_source, const char* fragment_source);

#ifdef __cplusplus
}
#endif

#endif // GL_UTILITY_H_
