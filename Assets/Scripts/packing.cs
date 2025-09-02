using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class packing : MonoBehaviour
{
    // Start is called before the first frame update

   
    public TMP_Dropdown packing1;
    public TMP_Dropdown packing2;



    public void SetOption(int a)
    {
        
        List<string> messages = new List<string>();
        List<string> messages2 = new List<string>();

        if (a == 0)
        {
            
            packing1.ClearOptions();
                      
            messages = new List<string> { "4000", "1000", "800", "3000", "5000", "7000" };
            packing1.AddOptions(messages);

            packing2.ClearOptions();
            messages2 = new List<string> { "6", "24","4" };
            packing2.AddOptions(messages2);

        }
        if (a == 1)
        {
           
            packing1.ClearOptions();
            messages = new List<string> { "800", "1000", "1200" ,"650", "700" };
            packing1.AddOptions(messages);

            packing2.ClearOptions();
            messages2 = new List<string> { "24", "12", };
            packing2.AddOptions(messages2);

        }
        if (a == 2)
        {
            
            packing1.ClearOptions();
            
            messages = new List<string> { "800" };
            packing1.AddOptions(messages);
            packing2.ClearOptions();
            messages2 = new List<string> { "24" };
            packing2.AddOptions(messages2);
        }
        if (a == 3)
        {
            packing1.ClearOptions();

            messages = new List<string> { "800", "1000", "1200", "650", "700" };
            packing1.AddOptions(messages);
            packing2.ClearOptions();
            messages2 = new List<string> { "24","12" };
            packing2.AddOptions(messages2);

        }
        if (a == 4)
        {
            packing1.ClearOptions();

            messages = new List<string> {  "800", "700" };
            packing1.AddOptions(messages);
            packing2.ClearOptions();
            messages2 = new List<string> { "24" };
            packing2.AddOptions(messages2);
        }
        if (a == 5)
        {
            packing1.ClearOptions();

            messages = new List<string> { "120", "150", "180", "200" };
            packing1.AddOptions(messages);
            packing2.ClearOptions();
            messages2 = new List<string> { "12", "24", "48" };
            packing2.AddOptions(messages2);
        }
        if (a == 6)
        {
            packing1.ClearOptions();

            messages = new List<string> { "4000", "1000", "800",  "3000",  "5000", "7000" };
            packing1.AddOptions(messages);

            packing2.ClearOptions();
            messages2 = new List<string> { "6", "24", "4" };
            packing2.AddOptions(messages2);
        }
        if (a == 7)
        {
            packing1.ClearOptions();

            messages = new List<string> {  "120", "150", "100" };
            packing1.AddOptions(messages);
            packing2.ClearOptions();
            messages2 = new List<string> { "24", "48" };
            packing2.AddOptions(messages2);
        }
        if (a == 8)
        {
            packing1.ClearOptions();

            messages = new List<string> { "100", "120", "150" };
            packing1.AddOptions(messages);
            packing2.ClearOptions();
            messages2 = new List<string> { "24", "48" };
            packing2.AddOptions(messages2);
        }
       
    }

}
