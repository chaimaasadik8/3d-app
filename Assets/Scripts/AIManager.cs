using System;
using System.Collections;
using System.Collections.Generic;
using GoogleARCore;
using GoogleARCore.Examples.AugmentedImage;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using GraphQlClient.Core;
using UnityEngine.Serialization;

public class AIManager : MonoBehaviour
{
    public GameObject SnakePrefab;
    public GameObject RhinoPrefab;
    private Dictionary<int, GameObject> _visualizers = new Dictionary<int, GameObject>();
    private List<AugmentedImage> _tempAugmentedImages = new List<AugmentedImage>();
    public TMP_InputField mailInput;
    public GraphApi mondayApi;

    public void Awake()
    {
        Application.targetFrameRate = 60;
    }

    public void Update()
    {
        if (Input.GetKey(KeyCode.Escape))
            Application.Quit();

        Screen.sleepTimeout = Session.Status != SessionStatus.Tracking
            ? SleepTimeout.SystemSetting
            : SleepTimeout.NeverSleep;
        Session.GetTrackables(_tempAugmentedImages, TrackableQueryFilter.Updated);
        foreach (var image in _tempAugmentedImages)
        {
            _visualizers.TryGetValue(image.DatabaseIndex, out var visualizer);
            if (image.TrackingState == TrackingState.Tracking && visualizer == null)
            {
                var anchor = image.CreateAnchor(image.CenterPose);
                visualizer = Instantiate(image.Name == "Rhino" ? RhinoPrefab : SnakePrefab, anchor.transform);
                _visualizers.Add(image.DatabaseIndex, visualizer);
            }
            else if (image.TrackingState == TrackingState.Stopped && visualizer != null)
            {
                _visualizers.Remove(image.DatabaseIndex);
                Destroy(visualizer.gameObject);
            }
        }
    }

    public void OnScreenshot()
    {
        StartCoroutine(nameof(CoroutineScreenshot));
    }

    private IEnumerator CoroutineScreenshot()
    {
        string timestamp = DateTime.Now.ToString("dd-MM-yyyy-HH-mm-ss");
        string fileName = $"Cerealis{timestamp}.png";
        ScreenCapture.CaptureScreenshot(fileName);

        yield return new WaitWhile(() => !System.IO.File.Exists($"{Application.persistentDataPath}/{fileName}"));

        new NativeShare()
            .AddFile($"{Application.persistentDataPath}/{fileName}")
            .SetSubject("Partage ton screenshot!")
            .SetText("Salut ! Regardez mon beau dessin")
            .Share();
    }

    public void OnSendEmail()
    {
        StartCoroutine(nameof(SendEmail));
    }
    
    public IEnumerator SendEmail()
    {
        var createUser = mondayApi.GetQueryByName("CreateItem", GraphApi.Query.Type.Mutation);
        createUser.SetArgs(new { board_id = 2402004949, item_name = mailInput.text });
        yield return mondayApi.Post(createUser).ContinueWith(t =>
        {
            if (!string.IsNullOrEmpty(t.Result.error))
                Debug.Log(t.Result.error);
            else
                Debug.Log("Request sucess");
        });
    }
}