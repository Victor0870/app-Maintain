using BansheeGz.BGDatabase;
using MySpace;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using static BansheeGz.BGDatabase.BGJsonRepoModel;

public class BlendingCheck : MonoBehaviour
{
    public TMP_Text BlendingLot;
    public TMP_Text LotMaterial;
    public QRCodeDecodeController qrcodeWorker;
    public TMP_Text xxx, numOfLot;
    public Button scan1;
    public Button scan2;
    public GameObject BLlot;
    public GameObject Matelot,nOfLot;

    private List<string> BlendingOrderList;

    [SerializeField]
    private string BASE_URL = "https://docs.google.com/forms/u/0/d/e/1FAIpQLSe7lSgVsCc_zOJaxzx4jtrD6wMb7mKjnyyMqKc1lqxyKhuYuw/formResponse";

    bool BlendingOrderCheck = false;
    bool AdditiveCheck = false;
    bool BlendingOrder = false;
    bool Additive = false;

    // Start is called before the first frame update
    void Start()
    {
        SaveData.Load();
        scan2.gameObject.SetActive(false);
        scan1.gameObject.SetActive(true);
        BLlot.gameObject.SetActive(false);
        Matelot.gameObject.SetActive(false);
        nOfLot.gameObject.SetActive(false);
        //numOfLot.gameObject.SetActive(false);   

        //qrcodeWorker.Reset();
        //qrcodeWorker.StopWork();
        StopScan();

      
       
       
        bool daCo = false;
        
        if (E_BlendingLot.CountEntities == 0)
        {
            E_BlendingLot.NewEntity();
            E_BlendingLot._f_name[0] = E_Blending._f_name[0];
            E_BlendingLot._f_LotAdditive[0] = E_Blending._f_Lot[0];
        }

        for (int i = 0; i < E_Blending.CountEntities - 1; i++)
        {
                      

            for (int jj = 0; jj < E_BlendingLot.CountEntities; jj++)
            {
                if ((E_Blending._f_name[i] == E_BlendingLot._f_name[jj]) & (E_BlendingLot._f_LotAdditive[jj] == E_Blending._f_Lot[i]))
                {
                    daCo = true;
                    break;
                }
                if ((E_Blending._f_name[i] == E_BlendingLot._f_name[jj]) & (E_BlendingLot._f_LotAdditive[jj] != E_Blending._f_Lot[i]))
                {
                    if (E_Blending._f_New1[i] == "1")
                    {
                        E_BlendingLot.GetEntity(jj).Delete();
                        break;
                    }
                    
                }


            }

            if (!daCo)
            {
                E_BlendingLot.NewEntity();
                E_BlendingLot._f_name[E_BlendingLot.CountEntities-1] = E_Blending._f_name[i];
                E_BlendingLot._f_LotAdditive[E_BlendingLot.CountEntities-1] = E_Blending._f_Lot[i];
                SaveData.Save();
            }
            else
            {
                daCo=false;
            }
            
        }

       // LotMaterial.text = E_BlendingLot.CountEntities.ToString();

    }
    public void QrcodeBack(string result)
    {

        if (BlendingOrderCheck)
        {
            BlendingLot.text = result;

            for (int i = 0; i < E_BlendingLot.CountEntities; ++i)
            {
                if (result == E_BlendingLot._f_name[i])
                {
                    BlendingOrder = true;
                    break;
                }
            }

            if (BlendingOrder)
            {
                scan2.gameObject.SetActive(true);
                scan1.gameObject.SetActive(false);
                BLlot.gameObject.SetActive(true);
                int kk = 0;
                for (int i = 0; i < E_BlendingLot.CountEntities; i++)
                {
                    if (result == E_BlendingLot._f_name[i])
                    {
                        if (E_BlendingLot._f_checked1[i] == 0)
                        {
                            kk++;
                        }
                    }
                }
                nOfLot.gameObject.SetActive(true);
                numOfLot.text = kk.ToString();
            }
            else
            {
                xxx.text = " Số Lot Blending không có trong danh sách kiểm tra";
            }
            
        }

        if (AdditiveCheck)
        {
            LotMaterial.text = result;

            for (int i = 0; i < E_BlendingLot.CountEntities; i++)
            {
                if (result == E_BlendingLot._f_LotAdditive[i])
                {
                    Additive = true;
                    E_BlendingLot._f_checked1[i] = 1;
                    SaveData.Save();
                    break;
                }
            }

            if (!Additive)
            {
                xxx.text = " Số Lot phụ gia không đúng";

                    xxx.color = Color.red;
            }
            else
            {
                xxx.text = "OK";

                int kk = 0;
                for (int i = 0; i < E_BlendingLot.CountEntities; ++i)
                {
                    if (BlendingLot.text == E_BlendingLot._f_name[i])
                    {
                        if (E_BlendingLot._f_checked1[i] == 0)
                        {
                            kk++;
                        }
                    }
                }
                numOfLot.text = kk.ToString();
            }

            //-----------------------------



            BlendingOrderList = new List<string>()
            {
                PlayerPrefs.GetString("UserName1"),
                BlendingLot.text,
                LotMaterial.text,        
            };
        StartCoroutine(Post(PlayerPrefs.GetString("UserName1"), BlendingLot.text, LotMaterial.text));
        
        }

    

    //---------------------------------


        BlendingOrderCheck = false;
        AdditiveCheck = false;
        BlendingOrder =false;
        Additive = false;
        StopScan();
    }

    public void StopScan()
    {
        qrcodeWorker.StopWork();
    }

    public void resetScanner()
    {
        qrcodeWorker.Reset();
    }

IEnumerator Post(string name, string BlendingOrder, string Additivelot)
{
    WWWForm form = new WWWForm();
    form.AddField("entry.2005620554", name); //lot
    form.AddField("entry.1045781291", BlendingOrder);  //btn weight
    form.AddField("entry.1065046570", Additivelot); // lineleader


    byte[] rawData = form.data;
    WWW www = new WWW(BASE_URL, rawData);
    yield return www;
}
public void StartScanBlendingOrder()
    {

        //codeDrum.text = "vui lòng chờ";
        BlendingOrderCheck = true;
        qrcodeWorker.StartWork();
        xxx.text = "";
    }
    public void StartScanMaterial()
    {
       AdditiveCheck = true;
        // prd2.text = "";
        //  codeMaterial.text = "vui lòng chờ";
        xxx.text = "";
        qrcodeWorker.StartWork();

    }

}
