using BansheeGz.BGDatabase;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AddManPower : MonoBehaviour
{
    public TMP_Text cn1;
    public TMP_Text cn2;
    public TMP_Text cn3;
    public TMP_Text cn4;
    public TMP_Text cn5;
    public TMP_Text cn6;
    public TMP_Text cn7;
    string lotNum;
 
    [SerializeField]
    private string BASE_URL = "https://docs.google.com/forms/d/e/1FAIpQLSfPjNGwCRKjOYCx8p8-yJJnsLenjtlZ7FQq2i727-79Tf-CHg/formResponse";

    private void Start()
    {
        cn7.text = PlayerPrefs.GetString("UserName1");
        lotNum = PlayerPrefs.GetString("lotNum");

    }

    public void SaveName()
    {

        StartCoroutine(Post(lotNum,cn7.text, cn1.text, cn2.text, cn3.text, cn4.text, cn5.text, cn6.text));
        SceneManager.LoadScene("Menu");

    }

    IEnumerator Post(string Lot,string Leader, string cn12, string cn2, string cn3, string cn4, string cn5, string cn6)
    {

        WWWForm form = new WWWForm();
        form.AddField("entry.1752213290", Lot);
        form.AddField("entry.531368122", Leader);
        form.AddField("entry.1092192676", cn12);
        form.AddField("entry.62665560", cn2);
        form.AddField("entry.1447190872", cn3);
        form.AddField("entry.90342914", cn4);
        form.AddField("entry.1983893046", cn5);
        form.AddField("entry.753090869", cn6);

        byte[] rawData = form.data;
        WWW www = new WWW(BASE_URL, rawData);
        yield return www;

        //SceneManager.LoadScene("Menu");
    }

}
