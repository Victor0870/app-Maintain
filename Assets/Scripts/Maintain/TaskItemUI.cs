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

    [Header("UI References - Interaction")]
    public Button taskItemButton;

    public void SetBasicTaskData(string content, string location, string createdBy, string timestamp, string status)
    {
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
            statusText.text = status; // Removed "Trạng thái: "
            switch (status)
            {
                case "Đang chờ":
                    statusText.color = Color.gray;
                    break;
                case "Đang làm":
                    statusText.color = Color.blue;
                    break;
                case "Đã xong":
                    statusText.color = Color.green;
                    break;
                default:
                    statusText.color = Color.black;
                    break;
            }
        }
    }
}