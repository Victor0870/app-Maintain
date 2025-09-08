using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TaskItemUI : MonoBehaviour
{
    [Header("UI References - Basic Display")]
    public TextMeshProUGUI contentText;
    public TextMeshProUGUI locationText;
    public TextMeshProUGUI createdByText;
    public TextMeshProUGUI timestampText;
    public TextMeshProUGUI statusText;
    public Image statusTextBackground;

    [Header("UI References - Interaction")]
    public Button taskItemButton;

    public void SetBasicTaskData(string content, string location, string createdBy, string timestamp, string status)
    {
        Color32 waitingBG  = new Color32(224, 224, 224, 255); // #E0E0E0
        Color32 waitingFG  = new Color32(66, 66, 66, 255);    // #424242

        Color32 doingBG    = new Color32(66, 165, 245, 255);  // #42A5F5
        Color32 doingFG    = new Color32(255, 255, 255, 255); // White

        Color32 doneBG     = new Color32(102, 187, 106, 255); // #66BB6A
        Color32 doneFG     = new Color32(255, 255, 255, 255); // White

        Color32 errorBG    = new Color32(239, 83, 80, 255);   // #EF5350
        Color32 errorFG    = new Color32(255, 255, 255, 255); // White
        if (contentText != null)
        {
            contentText.text = content; // Removed "Tên công việc: "
        }
        if (locationText != null)
        {
            locationText.text = location; // Removed "Vị trí: "
        }
        if (createdByText != null)
        {
            createdByText.text = createdBy; // Removed "Người yêu cầu: "
        }
        if (timestampText != null)
        {
            timestampText.text = timestamp; // Removed "Thời gian: "
        }
        if (statusText != null)
        {
            statusText.text = status;
            switch (status)
            {
                case "Đang chờ":
                    statusText.color = waitingFG;
                    statusTextBackground.color = waitingBG;
                break;
                case "Đang làm":
                    statusText.color = doingFG;
                    statusTextBackground.color = doingBG;
                break;
                case "Đã xong":
                    statusText.color = doneFG;
                    statusTextBackground.color = doneBG;
                break;
                default:
                    statusText.color = Color.black;
                    statusTextBackground.color = new Color32(245, 245, 245, 255); // Màu xám rất nhạt
                break;
            }
        }
    }
}