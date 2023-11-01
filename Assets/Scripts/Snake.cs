
using System;
using System.Collections.Generic;
using System.Linq;
using GoogleARCore;
using GoogleARCore.Examples.ComputerVision;
using GoogleARCoreInternal;
using OpenCvSharp;
using OpenCvSharp.Aruco;
using UnityEngine;
using UnityEngine.Apple;
using UnityEngine.UI;
using Rect = UnityEngine.Rect;

enum SnakePart
{
    HautTete,
    BasTete,
    Dos,
    Ventre,
    Langue
}

public class Snake : MonoBehaviour
{
    private const int MarkerId = 23;
    public Texture2D textureBase;
    // public Texture2D textureColor;
    
    public Material hautTeteMaterial, basTeteMaterial, dosMaterial, ventreMaterial, langueMaterial; 
    
    private Dictionary<SnakePart, List<ContourInfo>> _contourByPart;
    private Dictionary<SnakePart, Material> _materialByPart;
    
    private Camera _camera;
    
    private Texture2D texture;
    private int _imageWidth;
    private int _imageHeight;
    private byte[] _image;
    
    private int updateReset = 0;

    void Start()
    {
        _camera = GameObject.FindGameObjectWithTag("DeviceCam").GetComponent<Camera>();
        
        _materialByPart = new Dictionary<SnakePart, Material>
        {
            { SnakePart.HautTete, hautTeteMaterial },
            { SnakePart.BasTete, basTeteMaterial },
            { SnakePart.Dos, dosMaterial },
            { SnakePart.Ventre, ventreMaterial },
            { SnakePart.Langue, langueMaterial },
        };
        
        var matBase = OpenCvTools.ScaleImage(OpenCvSharp.Unity.TextureToMat(textureBase), MarkerId);
        var zones =  OpenCvTools.RetrieveZones(matBase)
            .OrderByDescending(ca => ca.Area)
            .ToList();
        _contourByPart = RetrieveParts(zones);
    }

    private Dictionary<SnakePart, List<ContourInfo>> RetrieveParts(List<ContourInfo> contoursInfos)
    {
        int[] ventreZones = { 56, 45, 47, 35, 36, 30, 25, 64, 60, 41, 42, 44, 37, 18, 12, 9, 11, 40 };
        int[] dosZones = { 45, 43, 38, 34, 32, 5, 33, 15, 16, 4, 22, 21, 24, 79,17, 6, 19, 28, 54, 64, 74, 57, 51, 53, 48, 29, 26, 3, 14, 20, 23 };
        
        return new Dictionary<SnakePart, List<ContourInfo>>
        {
            { SnakePart.HautTete, new List<ContourInfo> { contoursInfos[1] } },
            { SnakePart.BasTete, new List<ContourInfo> { contoursInfos[2] } },
            { SnakePart.Langue, new List<ContourInfo> { contoursInfos[13] } },
            { SnakePart.Ventre, ventreZones.Select(i => contoursInfos[i]).ToList() },
            { SnakePart.Dos, dosZones.Select(i => contoursInfos[i]).ToList() }
        };
    }

    private void Update()
    {
        updateReset--;
        if (updateReset > 0)
            return;
        updateReset = 25;
        
        ComputeColors();
        Resources.UnloadUnusedAssets();
    }

    private void ComputeColors()
    {
        var camResult = OpenCvTools.ScaleImage(OpenCvSharp.Unity.TextureToMat(GetCamAsTexture2D()), MarkerId);

        if (camResult == null)
        {
            Debug.Log("Can't find snake picture");
            return;
        }
        else
            Debug.Log("Snake picture retrieve!");
        foreach (var contourPart in _contourByPart)
        {
            var color = OpenCvTools.GetAverageColor(contourPart.Value, camResult);
            _materialByPart[contourPart.Key].color = color;
        }
    }
    
    private Texture2D GetCamAsTexture2D()
    {
        var mWidth = RoundUpToNearestMultipleOf16(Screen.width);
        var mHeight = RoundUpToNearestMultipleOf16(Screen.height);
        Rect rect = new Rect(0, 0, mWidth, mHeight);
        RenderTexture renderTexture = new RenderTexture(mWidth, mHeight, 24);
        Texture2D screenShot = new Texture2D(mWidth, mHeight, TextureFormat.RGBA32, false);
 
        _camera.targetTexture = renderTexture;
        _camera.Render();
 
        RenderTexture.active = renderTexture;
        screenShot.ReadPixels(rect, 0, 0);
        screenShot.Apply();
        
        _camera.targetTexture = null;
        RenderTexture.active = null;
 
        Destroy(renderTexture);
        renderTexture = null;
        return screenShot;
    }
    
    private static int RoundUpToNearestMultipleOf16(int value)
    {
        return (value + 15) & ~15;
    }
}
