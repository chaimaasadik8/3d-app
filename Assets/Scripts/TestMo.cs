using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQlClient.Core;
using UnityEngine;

public class TestMo : MonoBehaviour
{
    
    public GraphApi mondayApi;
    
    void Start()
    {
        StartCoroutine(nameof(SendEmail));
    }

    public IEnumerator SendEmail()
    {
        var createUser = mondayApi.GetQueryByName("CreateItem", GraphApi.Query.Type.Mutation);
        
        createUser.SetArgs(new { board_id = 2402004949, item_name = "teeest" });
        yield return mondayApi.Post(createUser).ContinueWith(t =>
        {
            if (!string.IsNullOrEmpty(t.Result.error))
                Debug.Log(t.Result.error);
            else
                Debug.Log("Request sucess");
        });
    }
}
