using MySpace;
using UnityEngine;
using UnityEngine.SceneManagement;
using Firebase.Firestore;
using System.Threading.Tasks;
using TMPro;
using BansheeGz.BGDatabase;
using System;
using System.Linq;

public class LoadScenes : MonoBehaviour
{
    public GameObject updateWarningPanel;
    public TMP_Text warningText;
    
    private FirebaseFirestore db;
    private string canvasAppId;
    
    void Start()
    {
        // Kiểm tra và tải dữ liệu cục bộ từ BGDatabase
        SaveData.Load();

        if (FirebaseManager.Instance == null || FirebaseManager.Instance.db == null)
        {
            Debug.LogError("Firebase không sẵn sàng. Thử lại sau 1 giây.");
            Invoke("Start", 1f);
            return;
        }
        
        db = FirebaseManager.Instance.db;
        canvasAppId = FirebaseManager.Instance.GetCanvasAppId();

        CheckAppVersionAsync();
    }

    private async void CheckAppVersionAsync()
    {
        int localVersion = -1; // -1 là giá trị mặc định nếu không tìm thấy
        
        // Lấy phiên bản cục bộ từ bảng E_Version
        var versionEntity = E_Version.FindEntity(entity => true);
        if (versionEntity != null)
        {
            localVersion = versionEntity.f_current;
        }
        else
        {
            Debug.LogWarning("Không tìm thấy dữ liệu phiên bản cục bộ trong BGDatabase.");
        }

        // Lấy phiên bản từ Firebase
        DocumentReference versionDocRef = db.Collection("artifacts")
            .Document(canvasAppId)
            .Collection("public")
            .Document("data")
            .Collection("version")
            .Document("current");
            
        try
        {
            DocumentSnapshot snapshot = await versionDocRef.GetSnapshotAsync();
            if (snapshot.Exists && snapshot.TryGetValue("version", out object serverVersionObj))
            {
                int serverVersion;
                if (int.TryParse(serverVersionObj.ToString(), out serverVersion))
                {
                    Debug.Log($"Phiên bản cục bộ: {localVersion}, Phiên bản máy chủ: {serverVersion}");
                    
                    if (localVersion >= serverVersion)
                    {
                        CheckAndLoadUserScene();
                    }
                    else
                    {
                        ShowUpdateWarning();
                    }
                }
                else
                {
                    Debug.LogWarning("Giá trị phiên bản trên Firebase không phải là số nguyên. Vẫn cho phép vào.");
                    CheckAndLoadUserScene();
                }
            }
            else
            {
                Debug.LogWarning("Không tìm thấy phiên bản trên Firebase. Vẫn cho phép vào.");
                CheckAndLoadUserScene();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Lỗi khi lấy phiên bản từ Firebase: {ex.Message}");
            CheckAndLoadUserScene();
        }
    }

    private void CheckAndLoadUserScene()
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

    private void ShowUpdateWarning()
    {
        if (updateWarningPanel != null && warningText != null)
        {
            updateWarningPanel.SetActive(true);
            warningText.text = "Phiên bản ứng dụng đã cũ. Vui lòng cập nhật lên phiên bản mới nhất.";
        }
        else
        {
            Debug.LogError("Panel cảnh báo hoặc Text chưa được gán.");
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