using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using MySpace;
using BansheeGz.BGDatabase;
using System.Threading.Tasks;
using System.Linq;
using System;

public class MaterialManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject sparePartManagerPanel;
    public GameObject sparePartListPanel;
    public GameObject materialUsagePanel;
    public GameObject materialSelectPanel;
    public GameObject confirmPanel;
    public GameObject purchasePanel;
    // --- Thêm panel chi tiết vật tư mới ---
    public GameObject materialDetailsPanel;

    [Header("UI Buttons")]
    public Button showListButton;
    public Button showPurchaseButton;
    public Button closeListButton;
    public Button confirmYesButton;
    public Button confirmNoButton;
    public Button closePurchaseButton;
    public Button confirmPurchaseButton;
    public Button addNewPurchaseButton;
    // --- Thêm nút đóng panel chi tiết mới ---
    public Button closeDetailsButton;

    [Header("UI Elements - List Panel")]
    public Transform materialsListParent;
    public GameObject materialItemPrefab;

    [Header("UI Elements - Select Panel")]
    public Transform materialSelectParent;
    public GameObject materialSelectPanelItemPrefab;

    public TMP_InputField searchInputField;

    [Header("UI Elements - Filters")]
    public TMP_Dropdown typeFilterDropdown;
    public TMP_Dropdown locationFilterDropdown;
    public TMP_Dropdown categoryFilterDropdown;

    [Header("UI Elements - Select Panel Filters")]
    public TMP_InputField selectSearchInputField;
    public TMP_Dropdown selectTypeFilterDropdown;
    public TMP_Dropdown selectLocationFilterDropdown;
    public TMP_Dropdown selectCategoryFilterDropdown;

    [Header("UI Elements - Usage Panel")]
    public Transform usageListParent;
    public GameObject usageItemPrefab;
    public Button closeUsagePanelButton;
    public Button addNewUsageButton;
    public Button confirmUsageButton;

    [Header("UI Elements - Purchase Panel")]
    public Transform purchaseItemsParent;
    public GameObject purchaseItemPrefab;
    // --- Đã xóa các khai báo thừa: poNumberInput, purchaseQuantityInput, supplierInput, selectedPurchaseMaterialText ---
    public TMP_InputField poNumberInput;
    public TMP_InputField purchaseQuantityInput;
    public TMP_InputField supplierInput;
    public TMP_Text selectedPurchaseMaterialText;

    [Header("UI Elements - Confirmation Popup")]
    public TextMeshProUGUI confirmPopupText;
    
    // --- Các biến mới cho panel chi tiết vật tư ---
    [Header("UI Elements - Details Panel")]
    public Transform usageHistoryParent;
    public Transform purchaseHistoryParent;
    public GameObject usageHistoryItemPrefab;
    public GameObject purchaseHistoryItemPrefab;
    

    public BGDatabaseToFirebaseSynchronizer synchronizerToFirebase;
    public FirebaseToBGDatabaseMaterialUsageSynchronizer materialUsageSynchronizer;
    public FirebaseToBGDatabasePurchaseSynchronizer purchaseSynchronizer;

    private List<E_SparePart> _allMaterials = new List<E_SparePart>();
    private string _currentTaskId;
    private string _selectedMaterialForPurchase;

    private MaterialUIManager _materialUIManager;
    private FirebaseToBGDatabaseSynchronizer _synchronizer;
    private MaterialDataHandler _materialDataHandler;

    private Dictionary<string, object> _temporaryTaskMaterials = new Dictionary<string, object>();
    private List<PurchaseItemUI> _currentPurchaseItems = new List<PurchaseItemUI>();

    async void Start()
    {
        SaveData.Load();
        if (FirebaseManager.Instance == null || FirebaseManager.Instance.db == null)
        {
            Debug.LogError("FirebaseManager chưa được khởi tạo hoặc Firebase chưa sẵn sàng. Thử lại sau 1 giây.");
            Invoke("Start", 1f);
            return;
        }

        _synchronizer = GetComponent<FirebaseToBGDatabaseSynchronizer>();
        if (_synchronizer == null)
        {
            Debug.LogError("Thiếu script FirebaseToBGDatabaseSynchronizer trên cùng GameObject. Vui lòng thêm nó.");
            return;
        }

        synchronizerToFirebase = FindObjectOfType<BGDatabaseToFirebaseSynchronizer>();
        if (synchronizerToFirebase == null)
        {
            Debug.LogError("Thiếu script BGDatabaseToFirebaseSynchronizer trong scene.");
            return;
        }

        materialUsageSynchronizer = FindObjectOfType<FirebaseToBGDatabaseMaterialUsageSynchronizer>();
        if (materialUsageSynchronizer == null)
        {
            Debug.LogError("Thiếu script FirebaseToBGDatabaseMaterialUsageSynchronizer trong scene.");
            return;
        }

        purchaseSynchronizer = FindObjectOfType<FirebaseToBGDatabasePurchaseSynchronizer>();
        if (purchaseSynchronizer == null)
        {
            Debug.LogError("Thiếu script FirebaseToBGDatabasePurchaseSynchronizer trong scene.");
            return;
        }

        _materialDataHandler = new MaterialDataHandler(FirebaseManager.Instance.db, FirebaseManager.Instance.GetCanvasAppId());

        _materialUIManager = new MaterialUIManager(
            sparePartListPanel, materialSelectPanel, materialsListParent, materialSelectParent, materialItemPrefab, materialSelectPanelItemPrefab,
            closeListButton, searchInputField, typeFilterDropdown, locationFilterDropdown, categoryFilterDropdown,
            materialUsagePanel, usageListParent, usageItemPrefab, closeUsagePanelButton, addNewUsageButton, confirmUsageButton,
            selectSearchInputField, selectTypeFilterDropdown, selectLocationFilterDropdown, selectCategoryFilterDropdown,
            confirmPanel, confirmPopupText, confirmYesButton, confirmNoButton, purchasePanel,
            // --- Thêm các tham số mới ---
            materialDetailsPanel, usageHistoryParent, purchaseHistoryParent, usageHistoryItemPrefab, purchaseHistoryItemPrefab, closeDetailsButton
        );

        if (showListButton != null) showListButton.onClick.AddListener(HandleShowListClicked);
        if (showPurchaseButton != null) showPurchaseButton.onClick.AddListener(HandleShowPurchaseClicked);
        if (closePurchaseButton != null) closePurchaseButton.onClick.AddListener(HandleClosePurchaseClicked);
        if (confirmPurchaseButton != null) confirmPurchaseButton.onClick.AddListener(HandleConfirmPurchaseClicked);
        if (addNewPurchaseButton != null) addNewPurchaseButton.onClick.AddListener(HandleAddNewPurchaseClicked);

        if (_materialUIManager != null)
        {
            _materialUIManager.OnCloseListPanelClicked += HandleCloseListPanelClicked;
            _materialUIManager.OnSearchOrFilterChanged += HandleSearchOrFilterChanged;
            _materialUIManager.OnCloseUsagePanelClicked += HandleCloseUsagePanelClicked;
            _materialUIManager.OnAddNewUsageClicked += HandleAddNewUsageClicked;
            _materialUIManager.OnConfirmUsageClicked += HandleConfirmUsageClicked;
            _materialUIManager.OnAddMaterialToTaskClicked += HandleAddMaterialToTaskClicked;
            _materialUIManager.OnQuantityChanged += HandleQuantityChanged;
            _materialUIManager.OnRemoveMaterialConfirmed += HandleRemoveMaterialConfirmed;
            _materialUIManager.OnAddMaterialToPurchaseClicked += HandleAddMaterialToPurchaseClicked;
            // --- Đăng ký sự kiện mới ---
            _materialUIManager.OnMaterialItemSelected += HandleMaterialItemSelected;
        }

        if (_materialDataHandler != null)
        {
            _materialDataHandler.OnTaskMaterialsChanged += _materialUIManager.UpdateMaterialUsageUI;
        }

        if (sparePartListPanel != null) sparePartListPanel.SetActive(false);
        if (materialUsagePanel != null) materialUsagePanel.SetActive(false);
        if (materialSelectPanel != null) materialSelectPanel.SetActive(false);
        if (confirmPanel != null) confirmPanel.SetActive(false);
        if (purchasePanel != null) purchasePanel.SetActive(false);
        // --- Ẩn panel chi tiết khi khởi động ---
        if (materialDetailsPanel != null) materialDetailsPanel.SetActive(false);

        DateTime latestLocalTimestamp = DateTime.MinValue;
        if (E_SparePart.CountEntities > 0)
        {
            latestLocalTimestamp = E_SparePart.FindEntities(entity => entity.f_lastUpdate > DateTime.MinValue)
                .OrderByDescending(entity => entity.f_lastUpdate)
                .FirstOrDefault()?.f_lastUpdate ?? DateTime.MinValue;
        }
        bool syncSuccess = await _synchronizer.SynchronizeSparePartsFromFirebase(latestLocalTimestamp);

        DateTime latestUsageSyncTimestamp = DateTime.MinValue;
        if (E_UsageHistory.CountEntities > 0)
        {
            latestUsageSyncTimestamp = E_UsageHistory.FindEntities(entity => entity.f_timestamp > DateTime.MinValue)
                                                     .OrderByDescending(entity => entity.f_timestamp)
                                                     .FirstOrDefault()?.f_timestamp ?? DateTime.MinValue;
        }
        await materialUsageSynchronizer.SynchronizeMaterialUsagesFromFirebase(latestUsageSyncTimestamp);

        DateTime latestPurchaseSyncTimestamp = DateTime.MinValue;
        if (E_PurchaseHistory.CountEntities > 0)
        {
            latestPurchaseSyncTimestamp = E_PurchaseHistory.FindEntities(entity => entity.f_timestamp > DateTime.MinValue)
                                                            .OrderByDescending(entity => entity.f_timestamp)
                                                            .FirstOrDefault()?.f_timestamp ?? DateTime.MinValue;
        }
        await purchaseSynchronizer.SynchronizePurchasesFromFirebase(latestPurchaseSyncTimestamp);


        if (syncSuccess)
        {
            LoadAllMaterialsFromBGDatabase();
            PopulateFilterDropdowns();
            SaveData.Save();
            Debug.Log("Đã lưu dữ liệu vào bộ nhớ cục bộ.");
        }
    }

    private void PopulateFilterDropdowns()
    {
        if (_allMaterials == null || _allMaterials.Count == 0)
        {
            Debug.LogWarning("Danh sách vật tư trống, không thể tạo bộ lọc.");
            return;
        }

        var types = new HashSet<string>();
        var locations = new HashSet<string>();
        var categories = new HashSet<string>();

        foreach (var material in _allMaterials)
        {
            types.Add(material.f_Type);
            locations.Add(material.f_Location);
            categories.Add(material.f_Category);
        }

        if (_materialUIManager != null)
        {
            _materialUIManager.UpdateFilterDropdown(typeFilterDropdown, types);
            _materialUIManager.UpdateFilterDropdown(locationFilterDropdown, locations);
            _materialUIManager.UpdateFilterDropdown(categoryFilterDropdown, categories);

            _materialUIManager.UpdateFilterDropdown(selectTypeFilterDropdown, types);
            _materialUIManager.UpdateFilterDropdown(selectLocationFilterDropdown, locations);
            _materialUIManager.UpdateFilterDropdown(selectCategoryFilterDropdown, categories);
        }
    }

    private void LoadAllMaterialsFromBGDatabase()
    {
        _allMaterials.Clear();
        E_SparePart.ForEachEntity(entity => _allMaterials.Add(entity));
        Debug.Log($"Đã tải {E_SparePart.CountEntities} vật tư từ BGDatabase cục bộ.");
    }

    void OnDestroy()
    {
        if (showListButton != null) showListButton.onClick.RemoveListener(HandleShowListClicked);
        if (showPurchaseButton != null) showPurchaseButton.onClick.RemoveListener(HandleShowPurchaseClicked);
        if (closePurchaseButton != null) closePurchaseButton.onClick.RemoveListener(HandleClosePurchaseClicked);
        if (confirmPurchaseButton != null) confirmPurchaseButton.onClick.RemoveListener(HandleConfirmPurchaseClicked);
        if (addNewPurchaseButton != null) addNewPurchaseButton.onClick.RemoveListener(HandleAddNewPurchaseClicked);

        if (_materialUIManager != null)
        {
            _materialUIManager.OnCloseListPanelClicked -= HandleCloseListPanelClicked;
            _materialUIManager.OnSearchOrFilterChanged -= HandleSearchOrFilterChanged;
            _materialUIManager.OnCloseUsagePanelClicked -= HandleCloseUsagePanelClicked;
            _materialUIManager.OnAddNewUsageClicked -= HandleAddNewUsageClicked;
            _materialUIManager.OnConfirmUsageClicked -= HandleConfirmUsageClicked;
            _materialUIManager.OnAddMaterialToTaskClicked -= HandleAddMaterialToTaskClicked;
            _materialUIManager.OnQuantityChanged -= HandleQuantityChanged;
            _materialUIManager.OnRemoveMaterialConfirmed -= HandleRemoveMaterialConfirmed;
            _materialUIManager.OnAddMaterialToPurchaseClicked -= HandleAddMaterialToPurchaseClicked;
            // --- Bỏ đăng ký sự kiện mới ---
            _materialUIManager.OnMaterialItemSelected -= HandleMaterialItemSelected;
        }

        if (_materialDataHandler != null)
        {
            _materialDataHandler.OnTaskMaterialsChanged -= _materialUIManager.UpdateMaterialUsageUI;
        }

        _materialDataHandler.StopListeningForTaskMaterials();
    }
    private async void HandleRemoveMaterialConfirmed(string materialId)
    {
        var localUsageRecord = E_UsageHistory.FindEntity(e => e.f_materialId == materialId && e.f_taskId == _currentTaskId);

        if (localUsageRecord != null && !string.IsNullOrEmpty(localUsageRecord.f_firestoreDocId))
        {
            await _materialDataHandler.DeleteMaterialUsage(_currentTaskId, localUsageRecord.f_firestoreDocId);

            localUsageRecord.Delete();
            SaveData.Save();
            Debug.Log("Đã xóa bản ghi sử dụng vật tư cục bộ.");
        }
        else
        {
            Debug.LogWarning("Không tìm thấy bản ghi cục bộ hoặc thiếu firestoreDocId. Không thể xóa trên Firebase.");
        }

        var localMaterial = E_SparePart.FindEntity(entity => entity.f_No.ToString() == materialId);
        if (localMaterial != null)
        {
            localMaterial.f_Stock += 1;
            await synchronizerToFirebase.SynchronizeSingleSparePart(localMaterial);
            SaveData.Save();
            Debug.Log("Đã lưu dữ liệu vào bộ nhớ cục bộ sau khi xóa vật tư và tăng stock.");
        }

        _materialUIManager.RemoveTemporaryMaterial(materialId);
    }

    public void ShowMaterialUsagePanel(string taskId)
    {
        _currentTaskId = taskId;
        _materialUIManager.ShowMaterialUsagePanel();
        _materialDataHandler.StartListeningForTaskMaterials(taskId);
    }

    private void HandleShowPurchaseClicked()
    {
        if (purchasePanel != null) purchasePanel.SetActive(true);
        if (sparePartListPanel != null) sparePartListPanel.SetActive(true);
        if (materialUsagePanel != null) materialUsagePanel.SetActive(false);
        if (materialSelectPanel != null) materialSelectPanel.SetActive(false);
        // --- Ẩn panel chi tiết khi hiển thị panel mua hàng ---
        if (materialDetailsPanel != null) materialDetailsPanel.SetActive(false);

        _materialUIManager.UpdateMaterialsListUI(_allMaterials, false);
    }

    private void HandleClosePurchaseClicked()
    {
        if (purchasePanel != null) purchasePanel.SetActive(false);
        if (sparePartListPanel != null) sparePartListPanel.SetActive(false);
        ClearPurchaseList();
    }

    private void HandleAddNewPurchaseClicked()
    {
        _materialUIManager.ShowMaterialSelectPanel();
        _materialUIManager.UpdateMaterialsListUI(_allMaterials, true);
    }

    private async void HandleAddMaterialToPurchaseClicked(string materialId, int initialQuantity)
    {
        if (_currentPurchaseItems.Any(item => item.materialId == materialId))
        {
            Debug.LogWarning("Vật tư đã có trong danh sách mua hàng.");
            _materialUIManager.HideMaterialSelectPanel();
            return;
        }

        E_SparePart material = E_SparePart.FindEntity(entity => entity.f_No.ToString() == materialId);
        if (material == null) return;

        GameObject purchaseUI = GameObject.Instantiate(purchaseItemPrefab, purchaseItemsParent);
        PurchaseItemUI itemScript = purchaseUI.GetComponent<PurchaseItemUI>();

        if (itemScript != null)
        {
            itemScript.SetPurchaseData(material.f_name, materialId);
            _currentPurchaseItems.Add(itemScript);
        }

        _materialUIManager.HideMaterialSelectPanel();
    }


    private async void HandleConfirmPurchaseClicked()
    {
        foreach (var item in _currentPurchaseItems)
        {
            string poNumber = item.poNumberInput.text;
            string supplier = item.supplierInput.text;

            if (!int.TryParse(item.quantityInput.text, out int quantity) || quantity <= 0)
            {
                Debug.LogError($"Số lượng không hợp lệ cho vật tư {item.materialNameText.text}. Bỏ qua.");
                continue;
            }

            string firestoreDocId = await _materialDataHandler.AddPurchaseRecordToFirebase(item.materialId, quantity, supplier, poNumber);

            var localMaterial = E_SparePart.FindEntity(entity => entity.f_No.ToString() == item.materialId);
            if (localMaterial != null)
            {
                localMaterial.f_Stock += quantity;
                SaveData.Save();

                await synchronizerToFirebase.SynchronizeSingleSparePart(localMaterial);
            }

            if (!string.IsNullOrEmpty(firestoreDocId))
            {
                var newPurchaseEntity = E_PurchaseHistory.NewEntity();
                newPurchaseEntity.f_firestoreDocId = firestoreDocId;
                newPurchaseEntity.f_materialId = item.materialId;
                newPurchaseEntity.f_quantity = quantity;
                newPurchaseEntity.f_supplier = supplier;
                newPurchaseEntity.f_name = poNumber;
                newPurchaseEntity.f_timestamp = DateTime.Now;
                SaveData.Save();
            }
        }

        Debug.Log("Đã xác nhận mua hàng thành công.");
        HandleClosePurchaseClicked();
    }

    private void ClearPurchaseList()
    {
        foreach (var item in _currentPurchaseItems)
        {
            GameObject.Destroy(item.gameObject);
        }
        _currentPurchaseItems.Clear();
    }

    private void HandleShowListClicked()
    {
        _materialUIManager.ShowMaterialsListPanel();
        _materialUIManager.UpdateMaterialsListUI(_allMaterials, false);
    }

    private void HandleCloseListPanelClicked()
    {
        _materialUIManager.HideMaterialsListPanel();
    }
    
    // --- Phương thức mới để xử lý sự kiện click vào item vật tư ---
    private void HandleMaterialItemSelected(string materialId)
    {
        E_SparePart material = E_SparePart.FindEntity(entity => entity.f_No.ToString() == materialId);
        if (material != null)
        {
            _materialUIManager.ShowMaterialDetailsPanel(
                material.f_name, 
                material.f_Stock.ToString(), 
                material.f_Location, 
                material.f_Purpose, 
                material.f_Category, 
                material.f_Type
            );

            var usageHistory = E_UsageHistory.FindEntities(e => e.f_materialId == materialId).ToList();
            var purchaseHistory = E_PurchaseHistory.FindEntities(e => e.f_materialId == materialId).ToList();

            _materialUIManager.UpdateUsageHistoryUI(usageHistory);
            _materialUIManager.UpdatePurchaseHistoryUI(purchaseHistory);
        }
    }

    private void HandleSearchOrFilterChanged(string searchTerm, string type, string location, string category)
    {
        List<E_SparePart> searchResults = new List<E_SparePart>();
        string lowerSearchTerm = searchTerm.ToLower();

        foreach (var material in _allMaterials)
        {
            bool matchesSearch = string.IsNullOrEmpty(lowerSearchTerm) || material.f_name.ToLower().Contains(lowerSearchTerm);
            bool matchesType = string.IsNullOrEmpty(type) || material.f_Type == type;
            bool matchesLocation = string.IsNullOrEmpty(location) || material.f_Location == location;
            bool matchesCategory = string.IsNullOrEmpty(category) || material.f_Category == category;

            if (matchesSearch && matchesType && matchesLocation && matchesCategory)
            {
                searchResults.Add(material);
            }
        }

        _materialUIManager.UpdateMaterialsListUI(searchResults, _materialUIManager.IsMaterialSelectPanelActive());
    }

    private void HandleCloseUsagePanelClicked()
    {
        _materialUIManager.HideMaterialUsagePanel();
        _materialDataHandler.StopListeningForTaskMaterials();
        _temporaryTaskMaterials.Clear();
    }

    private void HandleAddNewUsageClicked()
    {
        _materialUIManager.ShowMaterialSelectPanel();
        _materialUIManager.UpdateMaterialsListUI(_allMaterials, true);
    }

    private async void HandleConfirmUsageClicked()
    {
        var allUsageItems = _materialUIManager.GetAllMaterialUsageItems();

        foreach (var item in allUsageItems)
        {
            var localUsageRecord = E_UsageHistory.FindEntity(e => e.f_materialId == item.materialId && e.f_taskId == _currentTaskId);

            if (localUsageRecord != null && !string.IsNullOrEmpty(localUsageRecord.f_firestoreDocId))
            {
                if (item.changeType != MaterialUIManager.ChangeType.NoChange)
                {
                    int quantityChange = item.quantity - localUsageRecord.f_quantity;
                    if (quantityChange != 0)
                    {
                        var localMaterial = E_SparePart.FindEntity(entity => entity.f_No.ToString() == item.materialId);
                        if (localMaterial != null)
                        {
                            localMaterial.f_Stock -= quantityChange;
                            await synchronizerToFirebase.SynchronizeSingleSparePart(localMaterial);
                            SaveData.Save();
                            Debug.Log("Đã lưu dữ liệu vào bộ nhớ cục bộ sau khi cập nhật stock.");
                        }
                    }
                    await _materialDataHandler.UpdateMaterialUsage(_currentTaskId, localUsageRecord.f_firestoreDocId, item.quantity);

                    localUsageRecord.f_quantity = item.quantity;
                    localUsageRecord.f_timestamp = DateTime.Now;
                    SaveData.Save();
                }
            }
            else
            {
                string firestoreDocId = await _materialDataHandler.AddMaterialToTask(_currentTaskId, item.materialId, item.quantity);

                if (!string.IsNullOrEmpty(firestoreDocId))
                {
                    var newUsageRecord = E_UsageHistory.NewEntity();
                    newUsageRecord.f_taskId = _currentTaskId;
                    newUsageRecord.f_materialId = item.materialId;
                    newUsageRecord.f_quantity = item.quantity;
                    newUsageRecord.f_timestamp = DateTime.Now;
                    newUsageRecord.f_firestoreDocId = firestoreDocId;
                    SaveData.Save();
                }

                var localMaterial = E_SparePart.FindEntity(entity => entity.f_No.ToString() == item.materialId);
                if (localMaterial != null)
                {
                    localMaterial.f_Stock -= item.quantity;
                    await synchronizerToFirebase.SynchronizeSingleSparePart(localMaterial);
                    SaveData.Save();
                    Debug.Log("Đã lưu dữ liệu vào bộ nhớ cục bộ sau khi thêm mới vật tư.");
                }
            }
        }

        _temporaryTaskMaterials.Clear();
    }

    private void HandleAddMaterialToTaskClicked(string materialId, int initialQuantity)
    {
        _materialUIManager.HideMaterialSelectPanel();

        if (!_temporaryTaskMaterials.ContainsKey(materialId))
        {
            var localUsageRecord = E_UsageHistory.FindEntity(e => e.f_materialId == materialId && e.f_taskId == _currentTaskId);

            if (localUsageRecord == null)
            {
                _temporaryTaskMaterials[materialId] = new Dictionary<string, object>
                {
                    { "materialId", materialId },
                    { "quantity", initialQuantity },
                    { "status", "Đang chờ xác nhận" }
                };
            }
        }

        var combinedList = new List<Dictionary<string, object>>();
        var localUsageRecords = E_UsageHistory.FindEntities(e => e.f_taskId == _currentTaskId);

        foreach(var record in localUsageRecords)
        {
            combinedList.Add(new Dictionary<string, object>
            {
                {"id", record.f_firestoreDocId},
                {"materialId", record.f_materialId},
                {"quantity", record.f_quantity},
                {"timestamp", record.f_timestamp}
            });
        }

        foreach(var item in _temporaryTaskMaterials.Values)
        {
            if (item is Dictionary<string, object> dictItem)
            {
                if (!combinedList.Any(c => c.ContainsKey("materialId") && c["materialId"].ToString() == dictItem["materialId"].ToString()))
                {
                    combinedList.Add(dictItem);
                }
            }
        }

        _materialUIManager.UpdateMaterialUsageUI(combinedList);
    }

    private void HandleQuantityChanged(string materialId, int newQuantity, int oldQuantity)
    {
        E_SparePart materialToUpdate = E_SparePart.FindEntity(entity => entity.f_No.ToString() == materialId);
        if (materialToUpdate != null)
        {
            _materialUIManager.UpdateQuantityButtons(materialId, newQuantity, materialToUpdate.f_Stock);
        }
    }
}
