using BansheeGz.BGDatabase;
using BansheeGz.BGDatabase.Editor;
using Google.GData.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Xml.Linq;
using TMPro;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;
using UnityEngine.UIElements;
using UnityEngine.XR;


public class Calculate : MonoBehaviour
{
    public TMP_Dropdown FillingLine, PackingType, BotQty;
    public TMP_Text BotUnder, BotPass, BotOver;
    public TMP_Text CtnUnder, CtnPass, CtnOver;
    public TMP_Text NetBottle, NetCtn;
    public TMP_InputField BotWeight, CtnWeight, Density, LotNum;
    public GameObject warning;
    

    [SerializeField]
    private string BASE_URL = "https://docs.google.com/forms/u/0/d/e/1FAIpQLSch64zmJ9asPq0i9x0s4ewMX0B6Z9eyekx-_dyyhZ-mYpXclA/formResponse";

    //---------
    private string fLot, fLine, fType;
    private double underB, passB, overB, underC, passC, overC, c, d, e1, e, f, h, f1;
    private int u, p, o, botQty, test1;
    private string uType, pType, oType, linkForm, fftype;
    private List<string> fillingOrder;
    private BGEntity Row1;

    public void GeneratedClasses()
    {
        //number of tables
        int numberOfTables = BGRepo.I.CountMeta;


        //print table names
        BGRepo.I.ForEachMeta(meta => print(meta.Name));


        //create new table
        BGMetaEntity newTable = new BGMetaRow(BGRepo.I, "NewTable");


        //create new field
        BGField newField = new BGFieldInt(newTable, "NewField");


        //number of columns (fields) for table "Table"
        int numberOfFields = BGRepo.I["NewTable"].CountFields;


        //print field names for table "NewTable"
        BGRepo.I["NewTable"].ForEachField(field => print(field.Name));


        //type of field "Field" from Table "NewTable" (Int32)
        Type fieldType = BGRepo.I["NewTable"].GetField("NewField").ValueType;

        //get reference to table NewTable
        BGMetaEntity table = BGRepo.I["NewTable"];

        //create new row
        BGEntity row = table.NewEntity();

        //get a row by index (row.Index=0)
        BGEntity firstRow = table.GetEntity(row.Index);

        //get a row by ID (alternative)
        firstRow = table.GetEntity(row.Id);

        //read "NewField" field value (first row)
        int firstRowNewFieldValue = firstRow.Get<int>("NewField");

        //write "NewField" field value (first row)
        firstRow.Set<int>("NewField", 7);

        //populate new table with 10 records
        for (var i = 0; i < 10; i++) newTable.NewEntity().Name = "Entity #" + i;


        //number of rows for table "NewTable"
        int numberOfRows = BGRepo.I["NewTable"].CountEntities;


        //find entities, which names contains '5'
        List<BGEntity> entities = newTable.FindEntities(
                entity => !string.IsNullOrEmpty(entity.Name) && entity.Name.IndexOf('5') != -1);


        //find entities, which names contains '5' and sort them by NewField
        List<BGEntity> entities2 = newTable.FindEntities(
                entity => !string.IsNullOrEmpty(entity.Name) && entity.Name.IndexOf('5') != -1, null,
                (e1, e2) => e1.Get<int>("NewField").CompareTo(e2.Get<int>("NewField")));

        //the same as above, (faster version)
        BGField<int> fieldWithType = (BGField<int>)BGRepo.I["NewTable"].GetField("NewField");
        List<BGEntity> entities3 = newTable.FindEntities(
                entity => !string.IsNullOrEmpty(entity.Name) && entity.Name.IndexOf('5') != -1, null,
                (e1, e2) => fieldWithType[e1.Index].CompareTo(fieldWithType[e2.Index]));

        //the same as above with code generation, (faster and better version)
        //List<NewTable> entities4 = NewTable.FindEntities(
        //        entity => !string.IsNullOrEmpty(entity.Name) && entity.Name.IndexOf('5') != -1, null,
        //       (e1, e2) => e1.NewField.CompareTo(e2.NewField));


        //print all entities names
        newTable.ForEachEntity(entity => print(entity.Name));


        //print all entities names, which contains "5"
        newTable.ForEachEntity(entity => print(entity.Name),
                entity => !string.IsNullOrEmpty(entity.Name) && entity.Name.IndexOf('5') != -1);


        // query new table and increase NewField by 1, for each NewField value which is lesser than 5,
        // iteration is sorted by NewField
        newTable.ForEachEntity(entity =>
        {
            entity.Set("NewField", entity.Get<int>("NewField") + 1);
        }, entity => entity.Get<int>("NewField") < 5,
           (e1, e2) => e1.Get<int>("NewField").CompareTo(e2.Get<int>("NewField")));

        //the same as above (faster version)
        BGField<int> fieldWithType2 = (BGField<int>)BGRepo.I["NewTable"].GetField("NewField");
        newTable.ForEachEntity(entity =>
        {
            var val = fieldWithType2[entity.Index] + 1;
            fieldWithType2[entity.Index] = val;
        }, entity => fieldWithType2[entity.Index] < 5,
           (e1, e2) => fieldWithType2[e1.Index].CompareTo(fieldWithType2[e2.Index]));

        //the same as above (version with code generation, faster and better)
        // NewTable.ForEachEntity(entity => entity.NewField++, entity => entity.NewField < 5,
        //    (e1, e2) => e1.NewField.CompareTo(e2.NewField));


    }

    private void Start()
    {
        PlayerPrefs.SetString("lotNum", "");

    }

 


    public void Calculate1()
    {
       if (LotNum.text == "")
        {
            warning.SetActive(true);

        }
        else
        {
        TMP_Text captionText = FillingLine.captionText;
        fLine = captionText.text;
     
        TMP_Text captionText1 = PackingType.captionText;
        fType = captionText1.text;

        TMP_Text captionText2 = BotQty.captionText;
        botQty = int.Parse(captionText2.text);

        fLot = LotNum.text.ToUpper();

        PlayerPrefs.SetString("lotNum", fLot);

        //get reference to table NewTable
        BGMetaEntity table = BGRepo.I[fLine];

        int numberOfRows = BGRepo.I[fLine].CountEntities;

        for (int i = 0; i < numberOfRows; i++)
        {
            Row1 = table.GetEntity(i);
            fftype = Row1.Get<string>("name");

            if (fftype == fType)
            {
                u = Row1.Get<int>("Under");
                o = Row1.Get<int>("Over");
                p = Row1.Get<int>("Pass");
                              
            }
        }

        c = int.Parse(BotWeight.text);
        d = int.Parse(CtnWeight.text);
        e1 = double.Parse(Density.text);
        e = e1 / 10000;
        
        if ((fLine == "FB08") || (fLine == "FB09") || (fLine == "FB06"))
            {
               f1 = Math.Round(double.Parse(fType) * e, 1, MidpointRounding.AwayFromZero);

            }
            else
            {
                f1 = Mathf.RoundToInt((float)(double.Parse(fType) * e));
            }
        
       
        underB = f1 + u + c;
        passB = f1 + p + c;
        overB = f1 + o + c;

            if ((fLine == "FB08") || (fLine == "FB09") || (fLine == "FB06"))
            {
                NetBottle.text = (f1 + p).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
            }
            else
            {
                NetBottle.text = (f1 + p).ToString("0", System.Globalization.CultureInfo.InvariantCulture);
            }




        h = (f1 + p) * botQty;
        NetCtn.text = (h / 1000).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);

        underC = (underB * botQty + d) / 1000;
        passC = (passB * botQty + d) / 1000;
        overC = (overB * botQty + d) / 1000;

            if ((fLine == "FB08") || (fLine == "FB09") || (fLine == "FB06"))
            {
                BotUnder.text = underB.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
                BotPass.text = passB.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
                BotOver.text = overB.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
            }
            else
            {
                BotUnder.text = underB.ToString("0", System.Globalization.CultureInfo.InvariantCulture);
                BotPass.text = passB.ToString("0", System.Globalization.CultureInfo.InvariantCulture);
                BotOver.text = overB.ToString("0", System.Globalization.CultureInfo.InvariantCulture);
            }



            if ((fLine == "FB08") || (fLine == "FB09"))
            {
                CtnUnder.text = (underC * 1000).ToString("0", System.Globalization.CultureInfo.InvariantCulture) + " gr";
                CtnPass.text = (passC * 1000).ToString("0", System.Globalization.CultureInfo.InvariantCulture) + " gr";
                CtnOver.text = (overC * 1000).ToString("0", System.Globalization.CultureInfo.InvariantCulture) + " gr";
            }
            else
            {
                CtnUnder.text = underC.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
                CtnPass.text = passC.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
                CtnOver.text = overC.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
            }

        

        fillingOrder = new List<string>()
        {
         fLot,
         PlayerPrefs.GetString("UserName1"),
         fLine,
         fType+"mL",
         botQty.ToString(),
         BotWeight.text,
         CtnWeight.text,
         Density.text,
         NetBottle.text,
         NetCtn.text,
         BotUnder.text,
         BotPass.text,
         BotOver.text,
         CtnUnder.text,
         CtnPass.text,
         CtnOver.text
         };

         StartCoroutine(Post(fLot, PlayerPrefs.GetString("UserName1"), fLine, fType + "mL", botQty.ToString(), BotWeight.text, CtnWeight.text, Density.text, NetBottle.text, NetCtn.text, BotUnder.text, BotPass.text, BotOver.text, CtnUnder.text, CtnPass.text, CtnOver.text));

        }
    }

    IEnumerator Post(string Lot,string Leader, string Line, string Type, string botQty, string botW, string CtnW, string Den,string netB, string NetCtn, string BotU, string BotP, string BotO, string CtnU, string CtnP, string CtnO)
    {
        WWWForm form = new WWWForm();
        form.AddField("entry.2130584288", Lot); //lot
        form.AddField("entry.651929462", botW);  //btn weight
        form.AddField("entry.2110449477", Leader); // lineleader
        form.AddField("entry.2121958626", Line); //line
        form.AddField("entry.372943636", Type); //type
        form.AddField("entry.1407805609", botQty);// bot qty
        form.AddField("entry.48508433", CtnW);// ctn weight
        form.AddField("entry.635811114", Den); // Density 
        form.AddField("entry.52466103", netB);// netbottle
        form.AddField("entry.1787453343", NetCtn);//net ctn
        form.AddField("entry.1526193065", BotU);//under bot
        form.AddField("entry.1057899892", CtnU); //Under ctn
        form.AddField("entry.1495021021", BotO); //over bot
        form.AddField("entry.610635474", CtnP); //over bot
        form.AddField("entry.1936159370", CtnO); // pass ctn
        form.AddField("entry.335066292", BotP); // pas bot
       

        byte[] rawData = form.data;
        WWW www = new WWW(BASE_URL, rawData);
        yield return www;
    }

    [Obsolete]
    private void Update()
    {
        if ( Input.GetMouseButtonDown(0) && (warning.active == true) )
        {
            warning.SetActive(false);
        }
    }




}
