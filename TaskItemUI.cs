using UnityEngine;
using UnityEngine.UI; // For Button
using TMPro; // Important: Add this if you are using TextMeshPro for your UI Text components

public class TaskItemUI : MonoBehaviour
{
    [Header("UI References - Basic Display")]
    public TextMeshProUGUI contentText; // Tên công việc
    public TextMeshProUGUI locationText; // Vị trí làm việc
    public TextMeshProUGUI createdByText; // Người yêu cầu
    public TextMeshProUGUI timestampText; // Thời gian
    public TextMeshProUGUI statusText; // NEW: Nhãn hiển thị trạng thái công việc

    [Header("UI References - Interaction")]
    public Button taskItemButton; 

    // Phương thức này sẽ được gọi bởi TaskManager để thiết lập dữ liệu cơ bản cho mục
    public void SetBasicTaskData(string content, string location, string createdBy, string timestamp, string status) // NEW: Thêm tham số status
    {
        if (contentText != null)
        {
            contentText.text = "Tên công việc: " + content;
        }
        if (locationText != null)
        {
            locationText.text = "Vị trí: " + location;
        }
        if (createdByText != null)
        {
            createdByText.text = "Người yêu cầu: " + createdBy;
        }
        if (timestampText != null)
        {
            timestampText.text = "Thời gian: " + timestamp;
        }
        if (statusText != null) // NEW: Hiển thị trạng thái
        {
            statusText.text = "Trạng thái: " + status;
            // Tùy chọn: Thay đổi màu sắc dựa trên trạng thái
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
