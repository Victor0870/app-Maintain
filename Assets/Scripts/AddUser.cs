using MySpace;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class AddUser : MonoBehaviour
{
    public TMP_Text a;
    public TMP_Text b;

    private List<string> BlendingOrderList;

    [SerializeField]
    private string BASE_URL = "https://docs.google.com/forms/d/e/1FAIpQLSfzanWSbO51WFBMhCD1Wz8p0xpHJEcofORlFHsOpBesPeqdQQ/formResponse";
                                                                

    public void AddUserBt()
    {
        PlayerPrefs.SetString("UserName1", a.text);
        PlayerPrefs.SetString("Section", b.text);
        
     
        loadMenuScenes();

    }

    void loadMenuScenes()
    {
        SceneManager.LoadScene("Menu");
    }

    public void test1()
    {
      
        StartCoroutine(Post("11111", "2222", "3333"));

    }

    IEnumerator Post(string name, string BlendingOrder, string Additivelot)
    {
        WWWForm form = new WWWForm();
        form.AddField("entry.1827975019", name); //lot
        form.AddField("entry.115739622", BlendingOrder);  //btn weight
        form.AddField("entry.2091915599", Additivelot); // lineleader

        using (UnityWebRequest www = UnityWebRequest.Post(BASE_URL, form))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success) 
            
            {
                Debug.Log("ok done");
            }
            else
            {
                Debug.LogError("Errorrrrrrr " + www.error);
            }


        }    
       
    }
}
