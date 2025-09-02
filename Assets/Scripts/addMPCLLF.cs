using Google.GData.Extensions;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class addMPCLLF : MonoBehaviour
{
    public TMP_Text cn1;
    public TMP_Text cn2, prd1, prd2,code1,code2;
    public TMP_Dropdown cn3;
    public TMP_InputField cn4, remark;
    string original,volume;
 
    int input;

    [SerializeField]
    private string BASE_URL = "https://docs.google.com/forms/d/e/1FAIpQLSdQuz2kR52mJH2aLGcWwKKaWZTqNXrlWN2I7CHkS5DFW8dn0g/formResponse";

    public void addMP()
    {
        TMP_Text captionText = cn3.captionText;
        original = captionText.text;
        volume = cn4.text;

        StartCoroutine(Post(PlayerPrefs.GetString("UserName1"), code1.text, code2.text, original, volume, remark.text ));
        input = 1;

        cn1.text = "";
        cn2.text = "";
        cn4.text = "";
        prd1.text = "";
        prd2.text = "";
        remark.text = "";
        code1.text = "";
        code2.text = "";


    }

    IEnumerator Post(string name,  string drumCode, string materialCode, string ori, string volume, string remark)
    {
        WWWForm form = new WWWForm();
        form.AddField("entry.366697730", name);
        form.AddField("entry.228858519", drumCode);
        form.AddField("entry.960428087", materialCode);
        form.AddField("entry.1106015690", ori);
        form.AddField("entry.1047735871", volume);
        form.AddField("entry.2070146134", remark);


        byte[] rawData = form.data;
        WWW www = new WWW(BASE_URL, rawData);
        yield return www;

        //SceneManager.LoadScene("Menu");
    }
    void Start()
    {

        input = 0;

    }
}
