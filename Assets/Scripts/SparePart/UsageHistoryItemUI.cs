using UnityEngine;
using TMPro;
using System;

public class UsageHistoryItemUI : MonoBehaviour
{
    public TextMeshProUGUI quantityText;
    public TextMeshProUGUI timestampText;
    public TextMeshProUGUI taskNameText;
    public TextMeshProUGUI createdByText; // Thêm biến mới

    public void SetData(int quantity, DateTime timestamp, string taskName, string createdBy) // Thêm tham số createdBy
    {
        if (quantityText != null)
        {
            quantityText.text = $"Số lượng: {quantity}";
        }
        if (timestampText != null)
        {
            timestampText.text = $"Thời gian: {timestamp.ToString("dd/MM/yyyy HH:mm")}";
        }
        if (taskNameText != null)
        {
            taskNameText.text = $"Công việc: {taskName}";
        }
        // Hiển thị tên người yêu cầu
        if (createdByText != null)
        {
            createdByText.text = $"Người yêu cầu: {createdBy}";
        }
    }
}
