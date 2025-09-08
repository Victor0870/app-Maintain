using BansheeGz.BGDatabase;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveData : MonoBehaviour
{
/* Phương thức cũ
    public static bool HasSavedFile => File.Exists(SaveFilePath);

    public static string SaveFilePath => Path.Combine(Application.persistentDataPath, "bg1_save.dat");

    public static void Save() => File.WriteAllBytes(SaveFilePath, BGRepo.I.Save());

    public static void Load()
    {
        if (!HasSavedFile) return;
        BGRepo.I.Load(File.ReadAllBytes(SaveFilePath));
    }
*/
 public static bool HasSavedFile => File.Exists(SaveFilePath);

    public static string SaveFilePath => Path.Combine(Application.persistentDataPath, "bg1_save.dat");

    public static void Save()
    {
        // Sử dụng addon SaveLoad để lưu dữ liệu đã được cấu hình
        byte[] bytes = BGRepo.I.Addons.Get<BGAddonSaveLoad>().Save();
        File.WriteAllBytes(SaveFilePath, bytes);
        Debug.Log("Đã lưu dữ liệu bằng addon SaveLoad.");
    }

    public static void Load()
    {
        if (!HasSavedFile) return;
        byte[] bytes = File.ReadAllBytes(SaveFilePath);
        // Sử dụng addon SaveLoad để tải dữ liệu đã được cấu hình
        // ReloadDatabase = false để tránh tải lại toàn bộ database và làm hỏng các tham chiếu
        BGRepo.I.Addons.Get<BGAddonSaveLoad>().Load(
            new BGSaveLoadAddonLoadContext(new BGSaveLoadAddonLoadContext.LoadRequest(BGAddonSaveLoad.DefaultSettingsName, bytes))
            { ReloadDatabase = false }
        );
        Debug.Log("Đã tải dữ liệu bằng addon SaveLoad.");
    }


}
