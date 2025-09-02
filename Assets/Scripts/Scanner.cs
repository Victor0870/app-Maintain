using BansheeGz.BGDatabase;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Scanner : MonoBehaviour
{
    public QRCodeDecodeController qrcodeWorker;
    public TMP_Text codeDrum, codeMaterial, prd1, prd2, code11, code22;
    public GameObject warning1;
    string code1, code2, a,b, drumType;
    int drum, material, check, checkbtn1, checkbtn2;
    private BGEntity Row1;
       

    // Start is called before the first frame update
    void Start()
    {
        qrcodeWorker.Reset();
        qrcodeWorker.StopWork();
        drum = 0;
        material = 0;
        check = 0;
       
       
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void QrcodeBack(string result)
    {
               

        if (drum == 1)
        {
            drum = 0;

            if (result.Length != 22)
            {
                drumType = "Sai Code";
                
                codeDrum.text = drumType;
                prd1.text = "Kiểm tra lại";
                
            }
            else
            {   
                code11.text = result;
                code1 = result.Substring(0, 8);
                a = result.Substring(12, 1);

                switch (a)
                {
                    case "1":
                        drumType = "Phuy Vàng";
                        check = 1;
                        break;
                    case "2":
                        check = 1;
                        drumType = "Phuy Xanh";
                        break;
                    case "3":
                        drumType = "Phuy Vàng";
                        check = 1;
                        break;

                    default:
                        drumType = "Sai Code";
                        check = 0;
                        break;

                }

                codeDrum.text = drumType;

                if (code22.text != "")
                {
                    if ((code11.text.Substring(12, 1) == "1") & (code22.text.Substring(12, 1) == "2"))
                    {
                        warning1.SetActive(true);
                    }

                    if ((code11.text.Substring(12, 1) == "2") & (code22.text.Substring(12, 1) == "1"))
                    {
                        warning1.SetActive(true);
                    }
                    if ((code11.text.Substring(12, 1) == "2") & (code22.text.Substring(12, 1) == "3"))
                    {
                        warning1.SetActive(true);
                    }
                }

                if (check == 1)
                {
                    BGMetaEntity table = BGRepo.I["CodeBulk"];

                    int numberOfRows = BGRepo.I["CodeBulk"].CountEntities;

                    for (int i = 0; i < numberOfRows; i++)
                    {
                        Row1 = table.GetEntity(i);
                        code2 = Row1.Get<string>("code").Substring(0, 8);
                        
                        if (code2 == code1)
                        {
                            prd1.text = Row1.Get<string>("name");
                            checkbtn1 = 1;

                        }
                       

                        

                    }
                    
                }
                else
                {
                    prd1.text = "Kiểm tra lại";
                    
                    checkbtn1 = 0;
                }

                
            }

        }

        if (material == 1)
        {
            material = 0;
            if (result.Length != 28)
            {
                drumType = "Sai Code";
                codeMaterial.text = drumType;
                prd2.text = "Kiểm tra lại tem to";
                

            }
            else
            {
                code22.text = result;
                code1 = result.Substring(0, 8);
                a = result.Substring(12, 1);

                switch (a)
                {
                    case "1":
                        drumType = "MP";
                        check = 1;
                        break;
                    case "2":
                        check = 1;
                        drumType = "CL";
                        break;
                    case "3":
                        drumType = "LF";
                        check = 1;
                        break;

                    default:
                        drumType = "Sai Code";
                        check = 0;
                        
                        break;

                }
                codeMaterial.text = drumType;

                if (code11.text != "")
                {
                    if ((code11.text.Substring(12, 1) == "1") & (code22.text.Substring(12, 1) == "2"))
                    {
                        warning1.SetActive(true);
                    }

                    if ((code11.text.Substring(12, 1) == "2") & (code22.text.Substring(12, 1) == "1"))
                    {
                        warning1.SetActive(true);
                    }
                    if ((code11.text.Substring(12, 1) == "2") & (code22.text.Substring(12, 1) == "3"))
                    {
                        warning1.SetActive(true);
                    }
                }

                if (check == 1)
                {
                    BGMetaEntity table = BGRepo.I["CodeBulk"];

                    int numberOfRows = BGRepo.I["CodeBulk"].CountEntities;

                    for (int i = 0; i < numberOfRows; i++)
                    {
                        Row1 = table.GetEntity(i);
                        code2 = Row1.Get<string>("code").Substring(0, 8);

                        if (code1 == code2)
                        {
                            prd2.text = Row1.Get<string>("name");
                            checkbtn2 = 1;
                        }

                    }
                  
                }
                else
                {
                    prd2.text = "Kiểm tra lại";
                    
                    checkbtn2 = 0;
                }
                
               

            }
           
        }
        
        StopScan();
    }

    public void resetScanner()
    {
        qrcodeWorker.Reset();
    }

    public void StartScanDrum()
    {   
        drum = 1;
        codeDrum.text = "vui lòng chờ";
        prd1.text = "";
        qrcodeWorker.StartWork();
    }
    public void StartScanMaterial()
    {
        material = 1;
        prd2.text = "";
        codeMaterial.text = "vui lòng chờ";
        qrcodeWorker.StartWork();

    }

    public void StopScan()
    {
        qrcodeWorker.StopWork();
    }
}
