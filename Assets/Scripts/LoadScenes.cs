
using MySpace;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadScenes : MonoBehaviour
{
    
    void Start()
    {
        
      
        if (PlayerPrefs.GetString("UserName1") == "")
        {
            loadAddUserScenes();
        }

        else
        {
            for (int i = 0; i < E_ManPower.CountEntities; i++)
            {
                
                if (PlayerPrefs.GetString("UserName1") == E_ManPower._f_name[i])
                {

                    PlayerPrefs.SetString("Section", E_ManPower._f_Section[i]);

                    break;
                }
                
            }
            loadMenuScenes();
        }


    }

    
    void loadMenuScenes()
    {
        SceneManager.LoadScene("Menu");
    }

    void loadAddUserScenes()
    {
        SceneManager.LoadScene("Add User");
    }
}
