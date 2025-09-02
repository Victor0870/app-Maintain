using BansheeGz.BGDatabase;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveData : MonoBehaviour
{
    public static bool HasSavedFile => File.Exists(SaveFilePath);

    public static string SaveFilePath => Path.Combine(Application.persistentDataPath, "bg1_save.dat");

    public static void Save() => File.WriteAllBytes(SaveFilePath, BGRepo.I.Save());

    public static void Load()
    {
        if (!HasSavedFile) return;
        BGRepo.I.Load(File.ReadAllBytes(SaveFilePath));
    }

}
