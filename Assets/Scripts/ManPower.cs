using BansheeGz.BGDatabase;
using MySpace;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ManPower : MonoBehaviour
{
    public TMP_InputField ILVcode;
    public TMP_Text ILVname,ILVsection;
    private BGEntity Row;
    private string idCode;
    public GameObject btOK; 


   public void CodeToName() 
    {

        if (ILVcode.text != "")

        {
            //get reference to table NewTable
            BGMetaEntity table = BGRepo.I["ManPower"];
            BGRepo.I["ManPower"].ForEachField(field => print(field.Name));

            int numberOfRows = BGRepo.I["ManPower"].CountEntities;


            for (int i = 0; i < numberOfRows; i++)
            {
                Row = table.GetEntity(i);
                idCode = Row.Get<string>("IdCode");
                if (int.Parse(idCode) == int.Parse(ILVcode.text))
                {
                    ILVname.text = Row.Get<string>("name");
                    ILVsection.text = E_ManPower._f_Section[i];
                    btOK.SetActive(true);
                    
                    break;
                }
                else
                {
                    ILVname.text = "";
                    ILVsection.text = "";
                    btOK.SetActive(false);
                }




            }


        }
       


    }
}
