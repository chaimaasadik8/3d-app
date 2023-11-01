using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using OpenCvSharp;
using OpenCvSharp.Aruco;
using OpenCvSharp.Demo;

public class ARManager : MonoBehaviour
{
    public Texture2D textureColor;
    public Texture2D textureBase;
    
    public bool ShowColored = true;
    
    private Mat _matColor, _matBase;
    
    // Start is called before the first frame update
    void Start()
    {
        _matColor = OpenCvTools.ScaleImage(OpenCvSharp.Unity.TextureToMat(textureColor), 5);
        if (_matColor == null)
        {
            Debug.Log("MatColor null");
            return;
        }
        
        _matBase = OpenCvTools.ScaleImage(OpenCvSharp.Unity.TextureToMat(textureBase), 5);
        
        if (_matBase == null)
        {
            Debug.Log("MatBase null");
            return;
        }
        
        ShowZonesIndex(_matBase);
    }

    void ShowZonesIndex(Mat imageBase)
    {
        var contoursArea = OpenCvTools.RetrieveZones(imageBase)
            .OrderByDescending(ca => ca.Area)
            .Take(100)
            .ToList();
        
        for (int i = 0; i < contoursArea.Count; i++)
        {
            var contour = contoursArea[i];
            Cv2.PutText(imageBase, i.ToString(), new Point(contour.Center.X, contour.Center.Y), HersheyFonts.HersheySimplex, 0.3, Scalar.Red);
        }
    }
    
    void CalculateZones(Mat imageBase)
    {
        var contoursArea = OpenCvTools.RetrieveZones(imageBase);

        if (contoursArea.Count < 7)
            return;
        
        contoursArea = contoursArea
            .OrderByDescending(ca => ca.Area)
            .Skip(1)
            .Take(5)
            .ToList();
        
        var maxArea = contoursArea.Max(ca => ca.Area);
        var minArea = contoursArea.Min(ca => ca.Area);
        
        var contourAreaByCy = contoursArea.OrderBy(ca => ca.Center.Y).ToList();
        
        var teteHaut = contourAreaByCy.First();
        teteHaut.PartName = "Tete (Haut)";

        var filtredContourArea = contoursArea.Where(ca => ca != teteHaut).ToList();

        var teteBas = filtredContourArea.Take(2).OrderBy(ca => ca.Center.Y).First();
        teteBas.PartName = "Tete (Bas)";
        filtredContourArea = filtredContourArea.Where(ca => ca != teteBas).ToList();

        var corps = filtredContourArea.First();
        corps.PartName = "Corps";

        filtredContourArea = filtredContourArea
            .Where(ca => ca != corps).ToList()
            .OrderBy(ca => ca.Center.Y)
            .ToList();

        var corne = filtredContourArea.First();
        corne.PartName = "Corne";
        
        var basDuCorps = filtredContourArea.Last();
        basDuCorps.PartName = "Bas du corps";
        
        Debug.Log(maxArea);
        Debug.Log(minArea);
        
        foreach (var contour in contoursArea) {
            //Cv2.DrawContours(imageBase, new [] {contour.Contour}, 0, contour.GetMeanColor(), -1);
            Cv2.PutText(imageBase, contour.PartName, new Point(contour.Center.X - 50, contour.Center.Y), HersheyFonts.HersheySimplex, 1.0, new Scalar(0, 0, 0));
        }
    }

    void ShowTexture(Mat mat)
    {
        var rawImage = gameObject.GetComponent<RawImage> ();
        rawImage.texture = OpenCvSharp.Unity.MatToTexture (mat);
    }
    
    // Update is called once per frame
    void Update()
    {
        if (_matBase == null || _matColor == null)
            return;
        
        ShowTexture(ShowColored ? _matColor : _matBase);
    }
} 