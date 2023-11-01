using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System;
using System.Linq;
using UnityEngine.UI;

public class PhoneCameraEditTest
{
    [Test]
    public void CameraDetectionInEditMode()
    {
        // Créez une instance de la classe PhoneCamera
        var phoneCamera = new GameObject().AddComponent<PhoneCamera>();

        // Appelez la méthode Start en mode édition
        phoneCamera.Start();

        // Vérifiez si une caméra est détectée
        Assert.IsNotNull(phoneCamera.backCam);
    }
}


public class PhoneCamera : MonoBehaviour
{
    public static PhoneCamera Instance;
    public RawImage rawImage;

    public WebCamTexture backCam;

    public void Start()
    {
        var devices = WebCamTexture.devices;

        if (devices.Length == 0)
        {
            Debug.Log("No camera detected");
            return;
        }

        Debug.Log($"Find {devices.Length}");
        var device = devices.FirstOrDefault(d => !d.isFrontFacing);

        backCam = new WebCamTexture(device.name, Screen.width, Screen.height);
        backCam.Play();
        if (rawImage != null)
            rawImage.texture = backCam;
        Instance = this;
    }
}
