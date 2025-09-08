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

    [Header("UI Buttons")]
    public Button showListButton;
    public Button showPurchaseButton;
    public Button closeListButton;
    public Button confirmYesButton;
    public Button confirmNoButton;

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

    public BGDatabaseToFirebaseSynchronizer synchronizerToFirebase;
    public FirebaseToBGDatabaseMaterialUsageSynchronizer materialUsageSynchronizer; // BIẾN MỚI

    private List<E_SparePart> _allMaterials = new List<E_SparePart>();
    private string _currentTaskId;

    private MaterialUIManager _materialUIManager;
    private FirebaseToBGDatabaseSynchronizer _synchronizer;
    private MaterialDataHandler _materialDataHandler;

    private Dictionary<string, object> _temporaryTaskMaterials = new Dictionary<string, object>();

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

        materialUsageSynchronizer = FindObjectOfType<FirebaseToBGDatabaseMaterialUsageSynchronizer>(); // KHỞI TẠO BIẾN MỚI
        if (materialUsageSynchronizer == null)
        {
            Debug.LogError("Thiếu script FirebaseToBGDatabaseMaterialUsageSynchronizer trong scene.");
            return;
        }

        _materialDataHandler = new MaterialDataHandler(FirebaseManager.Instance.db, FirebaseManager.Instance.GetCanvasAppId());

        _materialUIManager = new MaterialUIManager(
            sparePartListPanel, materialSelectPanel, materialsListParent, materialSelectParent, materialItemPrefab, materialSelectPanelItemPrefab,
            closeListButton, searchInputField, typeFilterDropdown, locationFilterDropdown, categoryFilterDropdown,
            materialUsagePanel, usageListParent, usageItemPrefab, closeUsagePanelButton, addNewUsageButton, confirmUsageButton,
            selectSearchInputField, selectTypeFilterDropdown, selectLocationFilterDropdown, selectCategoryFilterDropdown,
            confirmPanel, confirmYesButton, confirmNoButton
        );

        if (showListButton != null) showListButton.onClick.AddListener(HandleShowListClicked);
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
        }

        if (_materialDataHandler != null)
        {
            _materialDataHandler.OnTaskMaterialsChanged += _materialUIManager.UpdateMaterialUsageUI;
        }

        if (sparePartListPanel != null) sparePartListPanel.SetActive(false);
        if (materialUsagePanel != null) materialUsagePanel.SetActive(false);
        if (materialSelectPanel != null) materialSelectPanel.SetActive(false);
        if (confirmPanel != null) confirmPanel.SetActive(false);

        // ĐỒNG BỘ HÓA TỒN KHO VẬT TƯ
        DateTime latestLocalTimestamp = DateTime.MinValue;
        if (E_SparePart.CountEntities > 0)
        {
            latestLocalTimestamp = E_SparePart.FindEntities(entity => entity.f_lastUpdate > DateTime.MinValue)
                .OrderByDescending(entity => entity.f_lastUpdate)
                .FirstOrDefault()?.f_lastUpdate ?? DateTime.MinValue;
        }
        bool syncSuccess = await _synchronizer.SynchronizeSparePartsFromFirebase(latestLocalTimestamp);

        // ĐỒNG BỘ HÓA LỊCH SỬ SỬ DỤNG VẬT TƯ
        DateTime latestUsageSyncTimestamp = DateTime.MinValue;
        if (E_UsageHistory.CountEntities > 0)
        {
            latestUsageSyncTimestamp = E_UsageHistory.FindEntities(entity => entity.f_timestamp > DateTime.MinValue)
                                                     .OrderByDescending(entity => entity.f_timestamp)
                                                     .FirstOrDefault()?.f_timestamp ?? DateTime.MinValue;
        }
        await materialUsageSynchronizer.SynchronizeMaterialUsagesFromFirebase(latestUsageSyncTimestamp);

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

        // Cập nhật stock cục bộ và đồng bộ lên Firebase
        var localMaterial = E_SparePart.FindEntity(entity => entity.f_No.ToString() == materialId);
        if (localMaterial != null)
        {
            localMaterial.f_Stock += 1;
            await synchronizerToFirebase.SynchronizeSingleSparePart(localMaterial);
            SaveData.Save();
            Debug.Log("Đã lưu dữ liệu vào bộ nhớ cục bộ sau khi xóa vật tư và tăng stock.");
        }

        // Xóa khỏi danh sách tạm thời và cập nhật UI cuối cùng
        _temporaryTaskMaterials.Remove(materialId);
        _materialUIManager.RemoveTemporaryMaterial(materialId);
    }

    public void ShowMaterialUsagePanel(string taskId)
    {
        _currentTaskId = taskId;
        _materialUIManager.ShowMaterialUsagePanel();
        _materialDataHandler.StartListeningForTaskMaterials(taskId);
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
            // Tìm bản ghi cục bộ để lấy firestoreDocId
            var localUsageRecord = E_UsageHistory.FindEntity(e => e.f_materialId == item.materialId && e.f_taskId == _currentTaskId);

            // Kiểm tra xem có phải là vật tư đã tồn tại trên Firebase và đang được cập nhật không
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
                    // GỌI PHƯƠNG THỨC MỚI VỚI firestoreDocId
                    await _materialDataHandler.UpdateMaterialUsage(_currentTaskId, localUsageRecord.f_firestoreDocId, item.quantity);

                    // Cập nhật lại bản ghi cục bộ
                    localUsageRecord.f_quantity = item.quantity;
                    localUsageRecord.f_timestamp = DateTime.Now;
                    SaveData.Save();
                }
            }
            else
            {
                // Trường hợp thêm mới vật tư
                // CHÚ Ý: CẦN SỬA MaterialDataHandler.AddMaterialToTask để trả về firestoreDocId
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

    private async void HandleAddMaterialToTaskClicked(string materialId, int initialQuantity)
    {
        _materialUIManager.HideMaterialSelectPanel();

        // Kiểm tra xem vật tư đã có trong danh sách tạm thời chưa
        if (!_temporaryTaskMaterials.ContainsKey(materialId))
        {
            // Kiểm tra xem vật tư có tồn tại trong bảng lịch sử cục bộ không
            var localUsageRecord = E_UsageHistory.FindEntity(e => e.f_materialId == materialId && e.f_taskId == _currentTaskId);

            if (localUsageRecord == null)
            {
                // Thêm mới
                string firestoreDocId = await _materialDataHandler.AddMaterialToTask(_currentTaskId, materialId, initialQuantity);
                if (!string.IsNullOrEmpty(firestoreDocId))
                {
                    var newUsageRecord = E_UsageHistory.NewEntity();
                    newUsageRecord.f_taskId = _currentTaskId;
                    newUsageRecord.f_materialId = materialId;
                    newUsageRecord.f_quantity = initialQuantity;
                    newUsageRecord.f_timestamp = DateTime.Now;
                    newUsageRecord.f_firestoreDocId = firestoreDocId;
                    SaveData.Save();
                }

                var localMaterial = E_SparePart.FindEntity(entity => entity.f_No.ToString() == materialId);
                if (localMaterial != null)
                {
                    localMaterial.f_Stock -= initialQuantity;
                    await synchronizerToFirebase.SynchronizeSingleSparePart(localMaterial);
                    SaveData.Save();
                }
            }
            // Nếu đã tồn tại thì không làm gì cả
        }

        // Luôn cập nhật UI từ dữ liệu cục bộ đã được đồng bộ
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