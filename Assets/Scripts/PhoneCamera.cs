
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
public class PhoneCamera : MonoBehaviour
{
    public static PhoneCamera Instance;
    public RawImage rawImage;
    
    public WebCamTexture backCam;

    private void Start()
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
