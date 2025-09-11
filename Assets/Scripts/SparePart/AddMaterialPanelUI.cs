using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using MySpace;

public class AddMaterialPanelUI : MonoBehaviour
{
    public TMP_InputField nameInput;
    public TMP_InputField purposeInput;
    public TMP_InputField typeInput;
    public TMP_InputField unitInput;
    public TMP_InputField locationInput;
    public TMP_InputField categoryInput;

    public Button saveButton;
    public Button closeButton;

    public E_SparePart GetMaterialData()
    {
        E_SparePart newMaterial = E_SparePart.NewEntity();
        newMaterial.f_name = nameInput.text;
        newMaterial.f_Purpose = purposeInput.text;
        newMaterial.f_Type = typeInput.text;
        newMaterial.f_Unit = unitInput.text;
        newMaterial.f_Stock = 0;
        newMaterial.f_Location = locationInput.text;
        newMaterial.f_Category = categoryInput.text;
        newMaterial.f_lastUpdate = DateTime.Now;

        return newMaterial;
    }

    public void ClearInputs()
    {
        nameInput.text = "";
        purposeInput.text = "";
        typeInput.text = "";
        unitInput.text = "";
        locationInput.text = "";
        categoryInput.text = "";
    }
}