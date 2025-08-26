using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class TaskStatusController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private ToggleGroup statusToggleGroup;
    [SerializeField] private Toggle pendingToggle;
    [SerializeField] private Toggle inProgressToggle;
    [SerializeField] private Toggle doneToggle;

    public delegate void StatusChangedAction(string newStatus);
    public event StatusChangedAction OnStatusChanged;
    
    // Biến để lưu trạng thái cũ trước khi thay đổi
    private string _previousStatus;

    private void Awake()
    {
        if (statusToggleGroup == null)
        {
            Debug.LogError("Toggle Group not assigned on TaskStatusController!");
            return;
        }

        pendingToggle.onValueChanged.AddListener(delegate { OnToggleValueChanged(pendingToggle); });
        inProgressToggle.onValueChanged.AddListener(delegate { OnToggleValueChanged(inProgressToggle); });
        doneToggle.onValueChanged.AddListener(delegate { OnToggleValueChanged(doneToggle); });
    }

    private void OnToggleValueChanged(Toggle changedToggle)
    {
        if (changedToggle.isOn)
        {
            string newStatus = "";
            if (changedToggle == pendingToggle)
            {
                newStatus = TaskConstants.STATUS_PENDING;
            }
            else if (changedToggle == inProgressToggle)
            {
                newStatus = TaskConstants.STATUS_IN_PROGRESS;
            }
            else if (changedToggle == doneToggle)
            {
                newStatus = TaskConstants.STATUS_DONE;
            }

            OnStatusChanged?.Invoke(newStatus);
        }
    }

    public void SetStatus(string status)
    {
        _previousStatus = status;

        // Bỏ đăng ký sự kiện để tránh gọi lại OnToggleValueChanged
        pendingToggle.onValueChanged.RemoveAllListeners();
        inProgressToggle.onValueChanged.RemoveAllListeners();
        doneToggle.onValueChanged.RemoveAllListeners();
        
        switch (status)
        {
            case TaskConstants.STATUS_PENDING:
                pendingToggle.isOn = true;
                break;
            case TaskConstants.STATUS_IN_PROGRESS:
                inProgressToggle.isOn = true;
                break;
            case TaskConstants.STATUS_DONE:
                doneToggle.isOn = true;
                break;
        }

        // Vô hiệu hóa Toggle khi công việc đã xong
        bool isDone = (status == TaskConstants.STATUS_DONE);
        pendingToggle.interactable = !isDone;
        inProgressToggle.interactable = !isDone;
        doneToggle.interactable = true;

        // Đăng ký lại sự kiện sau khi đã đặt trạng thái
        pendingToggle.onValueChanged.AddListener(delegate { OnToggleValueChanged(pendingToggle); });
        inProgressToggle.onValueChanged.AddListener(delegate { OnToggleValueChanged(inProgressToggle); });
        doneToggle.onValueChanged.AddListener(delegate { OnToggleValueChanged(doneToggle); });
    }

    public void RevertToPreviousStatus()
    {
        if (!string.IsNullOrEmpty(_previousStatus))
        {
            SetStatus(_previousStatus);
        }
    }
}
