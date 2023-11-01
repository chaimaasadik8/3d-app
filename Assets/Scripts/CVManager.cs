// using System;
// using System.Collections.Generic;
// using GoogleARCore;
// using GoogleARCore.Examples.ComputerVision;
// using UnityEngine;
// using UnityEngine.UI;
//
// #if UNITY_EDITOR
// // Set up touch input propagation while using Instant Preview in the editor.
// using Input = GoogleARCore.InstantPreviewInput;
// #endif  // UNITY_EDITOR
//
// /// <summary>
// /// Controller for the ComputerVision example that accesses the CPU camera image (i.e. image
// /// bytes), performs edge detection on the image, and renders an overlay to the screen.
// /// </summary>
// public class ComputerVisionController : MonoBehaviour
// {
//     /// <summary>
//     /// The ARCoreSession monobehavior that manages the ARCore session.
//     /// </summary>
//     public ARCoreSession ARSessionManager;
//
//     /// <summary>
//     /// The frame rate update interval.
//     /// </summary>
//     private static float _frameRateUpdateInterval = 2.0f;
//
//     /// <summary>
//     /// A buffer that stores the result of performing edge detection on the camera image each
//     /// frame.
//     /// </summary>
//     private byte[] _edgeDetectionResultImage = null;
//
//     /// <summary>
//     /// Texture created from the result of running edge detection on the camera image bytes.
//     /// </summary>
//     private Texture2D _edgeDetectionBackgroundTexture = null;
//
//     /// <summary>
//     /// These UVs are applied to the background material to crop and rotate
//     /// '_edgeDetectionBackgroundTexture' to match the aspect ratio and rotation of the device
//     /// display.
//     /// </summary>
//     private DisplayUvCoords _cameraImageToDisplayUvTransformation;
//
//     private ScreenOrientation? _cachedOrientation = null;
//     private Vector2 _cachedScreenDimensions = Vector2.zero;
//     private bool _isQuitting = false;
//     private bool _useHighResCPUTexture = false;
//     private ARCoreSession.OnChooseCameraConfigurationDelegate _onChoseCameraConfiguration =
//         null;
//
//     private int _highestResolutionConfigIndex = 0;
//     private int _lowestResolutionConfigIndex = 0;
//     private bool _resolutioninitialized = false;
//     private Text _imageTextureToggleText;
//     private float _renderingFrameRate = 0f;
//     private float _renderingFrameTime = 0f;
//     private int _frameCounter = 0;
//     private float _framePassedTime = 0.0f;
//
//     /// <summary>
//     /// The Unity Awake() method.
//     /// </summary>
//     public void Awake()
//     {
//         // Lock screen to portrait.
//         Screen.autorotateToLandscapeLeft = false;
//         Screen.autorotateToLandscapeRight = false;
//         Screen.autorotateToPortraitUpsideDown = false;
//         Screen.orientation = ScreenOrientation.Portrait;
//
//         // Enable ARCore to target 60fps camera capture frame rate on supported devices.
//         // Note, Application.targetFrameRate is ignored when QualitySettings.vSyncCount != 0.
//         Application.targetFrameRate = 60;
//
//         // Register the callback to set camera config before arcore session is enabled.
//         _onChoseCameraConfiguration = ChooseCameraConfiguration;
//         ARSessionManager.RegisterChooseCameraConfigurationCallback(
//             _onChoseCameraConfiguration);
//     }
//
//     /// <summary>
//     /// The Unity Start() method.
//     /// </summary>
//     public void Start()
//     {
//         Screen.sleepTimeout = SleepTimeout.NeverSleep;
//     }
//
//     /// <summary>
//     /// The Unity Update() method.
//     /// </summary>
//     public void Update()
//     {
//         UpdateFrameRate();
//
//         using (var image = Frame.CameraImage.AcquireCameraImageBytes())
//         {
//             if (!image.IsAvailable)
//             {
//                 return;
//             }
//
//             OnImageAvailable(image.Width, image.Height, image.YRowStride, image.Y, 0);
//         }
//     }
//
//     private void UpdateFrameRate()
//     {
//         _frameCounter++;
//         _framePassedTime += Time.deltaTime;
//         if (_framePassedTime > _frameRateUpdateInterval)
//         {
//             _renderingFrameTime = 1000 * _framePassedTime / _frameCounter;
//             _renderingFrameRate = 1000 / _renderingFrameTime;
//             _framePassedTime = 0f;
//             _frameCounter = 0;
//         }
//     }
//
//     /// <summary>
//     /// Handles a new CPU image.
//     /// </summary>
//     /// <param name="width">Width of the image, in pixels.</param>
//     /// <param name="height">Height of the image, in pixels.</param>
//     /// <param name="rowStride">Row stride of the image, in pixels.</param>
//     /// <param name="pixelBuffer">Pointer to raw image buffer.</param>
//     /// <param name="bufferSize">The size of the image buffer, in bytes.</param>
//     private void OnImageAvailable(
//         int width, int height, int rowStride, IntPtr pixelBuffer, int bufferSize)
//     {
//         if (_edgeDetectionBackgroundTexture == null ||
//             _edgeDetectionResultImage == null ||
//             _edgeDetectionBackgroundTexture.width != width ||
//             _edgeDetectionBackgroundTexture.height != height)
//         {
//             _edgeDetectionBackgroundTexture =
//                 new Texture2D(width, height, TextureFormat.R8, false, false);
//             _edgeDetectionResultImage = new byte[width * height];
//             _cameraImageToDisplayUvTransformation = Frame.CameraImage.ImageDisplayUvs;
//         }
//
//         if (_cachedOrientation != Screen.orientation ||
//             _cachedScreenDimensions.x != Screen.width ||
//             _cachedScreenDimensions.y != Screen.height)
//         {
//             _cameraImageToDisplayUvTransformation = Frame.CameraImage.ImageDisplayUvs;
//             _cachedOrientation = Screen.orientation;
//             _cachedScreenDimensions = new Vector2(Screen.width, Screen.height);
//         }
//
//         // Detect edges within the image.
//         if (EdgeDetector.Detect(
//             _edgeDetectionResultImage, pixelBuffer, width, height, rowStride))
//         {
//             // Update the rendering texture with the edge image.
//             _edgeDetectionBackgroundTexture.LoadRawTextureData(_edgeDetectionResultImage);
//             _edgeDetectionBackgroundTexture.Apply();
//             EdgeDetectionBackgroundImage.material.SetTexture(
//                 "_ImageTex", _edgeDetectionBackgroundTexture);
//
//             const string TOP_LEFT_RIGHT = "_UvTopLeftRight";
//             const string BOTTOM_LEFT_RIGHT = "_UvBottomLeftRight";
//             EdgeDetectionBackgroundImage.material.SetVector(TOP_LEFT_RIGHT, new Vector4(
//                 _cameraImageToDisplayUvTransformation.TopLeft.x,
//                 _cameraImageToDisplayUvTransformation.TopLeft.y,
//                 _cameraImageToDisplayUvTransformation.TopRight.x,
//                 _cameraImageToDisplayUvTransformation.TopRight.y));
//             EdgeDetectionBackgroundImage.material.SetVector(BOTTOM_LEFT_RIGHT, new Vector4(
//                 _cameraImageToDisplayUvTransformation.BottomLeft.x,
//                 _cameraImageToDisplayUvTransformation.BottomLeft.y,
//                 _cameraImageToDisplayUvTransformation.BottomRight.x,
//                 _cameraImageToDisplayUvTransformation.BottomRight.y));
//         }
//     }
//
//     /// <summary>
//     /// Quit the application if there was a connection error for the ARCore session.
//     /// </summary>
//     private void QuitOnConnectionErrors()
//     {
//         if (_isQuitting)
//         {
//             return;
//         }
//
//         // Quit if ARCore was unable to connect and give Unity some time for the toast to
//         // appear.
//         if (Session.Status == SessionStatus.ErrorPermissionNotGranted)
//         {
//             ShowAndroidToastMessage("Camera permission is needed to run this application.");
//             _isQuitting = true;
//             Invoke("DoQuit", 0.5f);
//         }
//         else if (Session.Status == SessionStatus.ErrorInvalidCameraConfig)
//         {
//             ShowAndroidToastMessage(
//                 "Cannot find a valid camera config. " +
//                 "Please try a less restrictive filter and start the app again.");
//             _isQuitting = true;
//             Invoke("DoQuit", 0.5f);
//         }
//         else if (Session.Status == SessionStatus.FatalError)
//         {
//             ShowAndroidToastMessage(
//                 "ARCore encountered a problem connecting.  Please start the app again.");
//             _isQuitting = true;
//             Invoke("DoQuit", 0.5f);
//         }
//     }
//
//     /// <summary>
//     /// Show an Android toast message.
//     /// </summary>
//     /// <param name="message">Message string to show in the toast.</param>
//     private void ShowAndroidToastMessage(string message)
//     {
//         AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
//         AndroidJavaObject unityActivity =
//             unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
//
//         if (unityActivity != null)
//         {
//             AndroidJavaClass toastClass = new AndroidJavaClass("android.widget.Toast");
//             unityActivity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
//             {
//                 AndroidJavaObject toastObject =
//                     toastClass.CallStatic<AndroidJavaObject>(
//                         "makeText", unityActivity, message, 0);
//                 toastObject.Call("show");
//             }));
//         }
//     }
//
//     /// <summary>
//     /// Actually quit the application.
//     /// </summary>
//     private void DoQuit()
//     {
//         Application.Quit();
//     }
//
//     /// <summary>
//     /// Generate string to print the value in CameraIntrinsics.
//     /// </summary>
//     /// <param name="intrinsics">The CameraIntrinsics to generate the string from.</param>
//     /// <param name="intrinsicsType">The string that describe the type of the
//     /// intrinsics.</param>
//     /// <returns>The generated string.</returns>
//     private string CameraIntrinsicsToString(CameraIntrinsics intrinsics, string intrinsicsType)
//     {
//         float fovX = 2.0f * Mathf.Rad2Deg * Mathf.Atan2(
//             intrinsics.ImageDimensions.x, 2 * intrinsics.FocalLength.x);
//         float fovY = 2.0f * Mathf.Rad2Deg * Mathf.Atan2(
//             intrinsics.ImageDimensions.y, 2 * intrinsics.FocalLength.y);
//
//         string frameRateTime = _renderingFrameRate < 1 ? "Calculating..." :
//             string.Format("{0}ms ({1}fps)", _renderingFrameTime.ToString("0.0"),
//                 _renderingFrameRate.ToString("0.0"));
//
//         string message = string.Format(
//             "Unrotated Camera {4} Intrinsics:{0}  Focal Length: {1}{0}  " +
//             "Principal Point: {2}{0}  Image Dimensions: {3}{0}  " +
//             "Unrotated Field of View: ({5}°, {6}°){0}" +
//             "Render Frame Time: {7}",
//             Environment.NewLine, intrinsics.FocalLength.ToString(),
//             intrinsics.PrincipalPoint.ToString(), intrinsics.ImageDimensions.ToString(),
//             intrinsicsType, fovX, fovY, frameRateTime);
//         return message;
//     }
//
//     /// <summary>
//     /// Select the desired camera configuration.
//     /// If high resolution toggle is checked, select the camera configuration
//     /// with highest cpu image and highest FPS.
//     /// If low resolution toggle is checked, select the camera configuration
//     /// with lowest CPU image and highest FPS.
//     /// </summary>
//     /// <param name="supportedConfigurations">A list of all supported camera
//     /// configuration.</param>
//     /// <returns>The desired configuration index.</returns>
//     private int ChooseCameraConfiguration(List<CameraConfig> supportedConfigurations)
//     {
//         if (!_resolutioninitialized)
//         {
//             _highestResolutionConfigIndex = 0;
//             _lowestResolutionConfigIndex = 0;
//             CameraConfig maximalConfig = supportedConfigurations[0];
//             CameraConfig minimalConfig = supportedConfigurations[0];
//             for (int index = 1; index < supportedConfigurations.Count; index++)
//             {
//                 CameraConfig config = supportedConfigurations[index];
//                 if ((config.ImageSize.x > maximalConfig.ImageSize.x &&
//                      config.ImageSize.y > maximalConfig.ImageSize.y) ||
//                     (config.ImageSize.x == maximalConfig.ImageSize.x &&
//                      config.ImageSize.y == maximalConfig.ImageSize.y &&
//                      config.MaxFPS > maximalConfig.MaxFPS))
//                 {
//                     _highestResolutionConfigIndex = index;
//                     maximalConfig = config;
//                 }
//
//                 if ((config.ImageSize.x < minimalConfig.ImageSize.x &&
//                      config.ImageSize.y < minimalConfig.ImageSize.y) ||
//                     (config.ImageSize.x == minimalConfig.ImageSize.x &&
//                      config.ImageSize.y == minimalConfig.ImageSize.y &&
//                      config.MaxFPS > minimalConfig.MaxFPS))
//                 {
//                     _lowestResolutionConfigIndex = index;
//                     minimalConfig = config;
//                 }
//             }
//
//             string lowResConfigText = string.Empty;
//             string highResConfigText = string.Empty;
//             lowResConfigText +=
//                 string.Format("Facing Direction: {0}, ", minimalConfig.FacingDirection);
//             highResConfigText +=
//                 string.Format("Facing Direction: {0}, ", maximalConfig.FacingDirection);
//             lowResConfigText += string.Format(
//               "Low Resolution CPU Image ({0} x {1}), Target FPS: ({2} - {3}), " +
//               "Depth Sensor Usage: {4}",
//               minimalConfig.ImageSize.x, minimalConfig.ImageSize.y, minimalConfig.MinFPS,
//               minimalConfig.MaxFPS, minimalConfig.DepthSensorUsage);
//             highResConfigText = string.Format(
//               "High Resolution CPU Image ({0} x {1}), Target FPS: ({2} - {3}), " +
//               "Depth Sensor Usage: {4}",
//                maximalConfig.ImageSize.x, maximalConfig.ImageSize.y, maximalConfig.MinFPS,
//                maximalConfig.MaxFPS, maximalConfig.DepthSensorUsage);
//             lowResConfigText +=
//               string.Format(", Stereo Camera Usage: {0}", minimalConfig.StereoCameraUsage);
//             highResConfigText +=
//               string.Format(", Stereo Camera Usage: {0}", maximalConfig.StereoCameraUsage);
//             LowResConfigToggle.GetComponentInChildren<Text>().text = lowResConfigText;
//             HighResConfigToggle.GetComponentInChildren<Text>().text = highResConfigText;
//             _resolutioninitialized = true;
//       }
//
//         if (_useHighResCPUTexture)
//         {
//             return _highestResolutionConfigIndex;
//         }
//
//         return _lowestResolutionConfigIndex;
//     }
// }