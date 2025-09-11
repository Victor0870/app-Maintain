using UnityEngine;
using TMPro;
using System;
using UnityEngine.UI;

public class UsageHistoryItemUI : MonoBehaviour
{
    public TextMeshProUGUI quantityText;
    public TextMeshProUGUI timestampText;
    public TextMeshProUGUI taskNameText;
    public TextMeshProUGUI createdByText;
    public Button itemButton;

    private string _taskId;

    public void SetData(int quantity, DateTime timestamp, string taskName, string createdBy, string taskId)
    {
        _taskId = taskId;
        if (quantityText != null)
        {
            quantityText.text = $"{quantity}";
        }
        if (timestampText != null)
        {
            timestampText.text = $"{timestamp.ToString("dd/MM/yyyy HH:mm")}";
        }
        if (taskNameText != null)
        {
            taskNameText.text = $"{taskName}";
        }
        if (createdByText != null)
        {
            createdByText.text = $"{createdBy}";
        }
    }

    public string GetTaskId()
    {
        return _taskId;
    }
}