using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum RhinoPart
{
    TeteHaut,
    TeteBas,
    Corne,
    Corps,
    BasCorps
}

public class Rhino : MonoBehaviour
{
    private const int MarkerId = 5;
    public Texture2D textureBase;
    // public Texture2D textureColor;
    
    public Material hautTeteMaterial, basTeteMaterial, corneMaterial, corpsMaterial, basCorpsMaterial; 
    
    private Dictionary<RhinoPart, List<ContourInfo>> _contourByPart;
    private Dictionary<RhinoPart, Material> _materialByPart;
    
    private Camera _camera;
    
    private Texture2D texture;
    private int _imageWidth;
    private int _imageHeight;
    private byte[] _image;
    
    private int updateReset = 0;

    void Start()
    {
        _camera = GameObject.FindGameObjectWithTag("DeviceCam").GetComponent<Camera>();
        
        _materialByPart = new Dictionary<RhinoPart, Material>
        {
            { RhinoPart.TeteHaut, hautTeteMaterial },
            { RhinoPart.TeteBas, basTeteMaterial },
            { RhinoPart.Corne, corneMaterial },
            { RhinoPart.BasCorps, basCorpsMaterial },
            { RhinoPart.Corps, corpsMaterial },
        };
        
        var matBase = OpenCvTools.ScaleImage(OpenCvSharp.Unity.TextureToMat(textureBase), MarkerId);
        var zones =  OpenCvTools.RetrieveZones(matBase)
            .OrderByDescending(ca => ca.Area)
            .ToList();
        _contourByPart = RetrieveParts(zones);
    }
    private Dictionary<RhinoPart, List<ContourInfo>> RetrieveParts(List<ContourInfo> contoursInfos)
    {
        int[] corpsZone = { 8, 12, 3 };
        int[] basCorpsZones = { 13, 5 };
        
        return new Dictionary<RhinoPart, List<ContourInfo>>
        {
            { RhinoPart.TeteHaut, new List<ContourInfo> { contoursInfos[2] } },
            { RhinoPart.TeteBas, new List<ContourInfo> { contoursInfos[1] } },
            { RhinoPart.Corps, corpsZone.Select(i => contoursInfos[i]).ToList() },
            { RhinoPart.Corne, new List<ContourInfo> { contoursInfos[4] } },
            { RhinoPart.BasCorps, basCorpsZones.Select(i => contoursInfos[i]).ToList() }
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
            Debug.Log("Can't find rhino picture");
            return;
        }
        else
            Debug.Log("Rhino picture retrieve!");
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
