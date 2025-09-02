// File: ExcelFilePicker.cs
using UnityEngine;
using NativeFilePickerNamespace;
using BansheeGz.BGDatabase;
using System;

public class ExcelFilePicker : MonoBehaviour
{
    // Kéo và thả component BGExcelImportGo vào trường này trong Inspector
    public BGExcelImportGo excelImporter;

    void Start()
    {
        // Đảm bảo rằng component BGExcelImportGo đã được gán
        if (excelImporter == null)
        {
            Debug.LogError("Import Component (BGExcelImportGo) is not assigned. Please assign it in the Inspector.");
            return;
        }

        // Đăng ký phương thức OnImportCompleted vào sự kiện của BGExcelImportGo
        excelImporter.OnImportUnityEvent.AddListener(OnImportCompleted);
    }

    /// <summary>
    /// Gọi phương thức này từ một nút UI để mở trình chọn tệp.
    /// </summary>
    public void OpenFilePickerAndImport()
    {
        // Sử dụng các đuôi tệp đã được bạn xác nhận là hoạt động
        string[] fileTypes = new string[] { "xlsx", "xls" };

        NativeFilePicker.PickFile((path) =>
        {
            // Kiểm tra xem người dùng có hủy chọn tệp không
            if (string.IsNullOrEmpty(path))
            {
                Debug.Log("Không có tệp nào được chọn.");
                return;
            }

            // Gán đường dẫn tệp đã chọn vào BGExcelImportGo
            excelImporter.ExcelFile = path;
            Debug.Log($"Đường dẫn tệp đã được thiết lập: {path}");

            // Gọi phương thức Import của BGExcelImportGo
            excelImporter.Import();
        }, fileTypes);
    }

    /// <summary>
    /// Phương thức này được gọi khi quá trình import hoàn thành.
    /// </summary>
    private void OnImportCompleted()
    {
        Debug.Log("Import đã hoàn thành thành công!");
    }
}