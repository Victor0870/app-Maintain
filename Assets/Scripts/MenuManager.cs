using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void loadCalculateScenes()
    {
        SceneManager.LoadScene("Calculate");
    }

    public void Quitgame()
    {
        Application.Quit();
    }

    public void loadMenuScenes()
    {
        SceneManager.LoadScene("Menu");
    }

    public void loadFillingLineScenes()
    {
        SceneManager.LoadScene("FillingLine");
    }

    public void loadManPowerScenes()
    {
        SceneManager.LoadScene("ManPower");
    }

    public void loadScanScenes()
    {
        SceneManager.LoadScene("ScanQrCode");
    }

    public void loadBledingScenes()
    {
        SceneManager.LoadScene("Blending");
    }

}
