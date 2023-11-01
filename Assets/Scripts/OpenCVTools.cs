using System;
using System.Collections.Generic;
using System.Linq;
using OpenCvSharp;
using OpenCvSharp.Aruco;
using OpenCvSharp.Demo;
using UnityEngine;
using UnityEngine.UI;

public static class OpenCvTools
{
    private const int Size = 512;
    
    private static Dictionary _arUcoMarkers = CvAruco.GetPredefinedDictionary (PredefinedDictionaryName.Dict6X6_250);
    
    public static Mat ScaleImage(Mat bgr, int markerId)
    {
        var bin = bgr.CvtColor(ColorConversionCodes.BGR2GRAY);
        
        DetectorParameters detectorParameters = DetectorParameters.Create();
        CvAruco.DetectMarkers (
            bin, 
            _arUcoMarkers, 
            out Point2f[][] markersCorners, 
            out int[] ids, 
            detectorParameters, 
            out Point2f[][] _
        );
        Debug.Log($"Ids: {ids.Length}");
        if (!ids.Contains(markerId))
            return null;

        var markerCenter = ContourInfo.GetCounterCenter(markersCorners[Array.IndexOf(ids, markerId)]);

        bin = bin.Threshold(125, 255, ThresholdTypes.Binary);
        Cv2.BitwiseNot(bin, bin);
        bin.FindContours(out var contours, out var h, RetrievalModes.List, ContourApproximationModes.ApproxNone);
        var validCorners = contours
            .Select(c => Cv2.ApproxPolyDP(c, Cv2.ArcLength(c, true) * 0.01f, true))
            .Where(c => c.Length == 4)
            .Where(c => Cv2.PointPolygonTest(c, markerCenter, false) >= 0)
            .ToList();
        
        if (validCorners.Count <= 1)
            return null;
        
        var corners = validCorners
            .OrderBy(c => Cv2.ContourArea(c))
            .Skip(1)
            .First();

        Array.Sort(corners, (a, b) => a.X.CompareTo(b.X));
        if (corners[0].Y > corners[1].Y)
            corners.Swap(0, 1);
        if (corners[3].Y > corners[2].Y)
            corners.Swap(2, 3);
        
        Point2f[] input = { corners[0], corners[1], corners[2], corners[3] };
        Point2f[] square = { new Point2f(0, 0), new Point2f(0, Size), new Point2f(Size, Size), new Point2f(Size, 0) };

        var transformm = Cv2.GetPerspectiveTransform(input, square);
        
        Cv2.WarpPerspective(bgr, bgr, transformm, new Size(Size, Size));

        int s = (int)(Size * 0.03);
        int w = (int)(Size * 0.9);
        
        OpenCvSharp.Rect innerRect = new OpenCvSharp.Rect(s, s, w, w);
        bgr = bgr[innerRect];

        return bgr;
    }

    public static List<ContourInfo> RetrieveZones(Mat imageBase)
    {
        var grayMat = new Mat();
        Cv2.CvtColor (imageBase, grayMat, ColorConversionCodes.BGR2GRAY);
        
        var thresh = new Mat ();
        Cv2.Threshold (grayMat, thresh, 170, 255, ThresholdTypes.BinaryInv);
        
        Cv2.FindContours (thresh, out var contours, out var _, RetrievalModes.Tree, ContourApproximationModes.ApproxNone, null);
        return contours.Select(c => new ContourInfo(c)).ToList();
    }

    public static Color GetAverageColor(IEnumerable<ContourInfo> contours, Mat imageColor)
    {
        var mask = new MatOfByte(imageColor.Clone()).SetTo(Scalar.Black);
        Cv2.FillPoly(mask, contours.Select(c => c.Contour).ToArray(), Scalar.White);
        
        var cvtMask = new Mat();
        Cv2.CvtColor (mask, cvtMask, ColorConversionCodes.BGR2GRAY);
        var thresh = new Mat ();
        Cv2.Threshold (cvtMask, thresh, 10, 255, ThresholdTypes.Binary);
        
        var color = Cv2.Mean(imageColor, thresh);

        return new Color((float)color.Val2 / 255, (float)color.Val1 / 255, (float)color.Val0 / 255);
    }
}

public class ContourInfo
{
    public double Area { get; set; }
    public Point[] Contour { get; set; }
    public Point Center { get; set; }
    public string PartName { get; set; }

    public ContourInfo(Point[] contour)
    {
        Area = Cv2.ContourArea(contour);
        Contour = contour;
        Center = GetCounterCenter(contour);
        PartName = "NOT FIND";
    }

    public static Point GetCounterCenter(Point[] contour)
    {
        var m = Cv2.Moments(contour);
        int cx = (int)(m.M10 / m.M00);
        int cy = (int)(m.M01 / m.M00);
        return new Point(cx, cy);
    }
    
    public static Point2f GetCounterCenter(Point2f[] contour)
    {
        var m = Cv2.Moments(contour);
        int cx = (int)(m.M10 / m.M00);
        int cy = (int)(m.M01 / m.M00);
        return new Point2f(cx, cy);
    }
}  