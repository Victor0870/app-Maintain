using Firebase.Firestore;

public static class FirebasePathUtils
{
    private const string COLLECTION_ARTIFACTS = "artifacts";
    private const string COLLECTION_PUBLIC = "public";
    private const string DOCUMENT_DATA = "data";
    private const string COLLECTION_TASKS = "tasks";
    private const string COLLECTION_MATERIALS = "materials";

    public static CollectionReference GetTasksCollection(string canvasAppId, FirebaseFirestore db)
    {
        return db.Collection(COLLECTION_ARTIFACTS)
                 .Document(canvasAppId)
                 .Collection(COLLECTION_PUBLIC)
                 .Document(DOCUMENT_DATA)
                 .Collection(COLLECTION_TASKS);
    }

    public static CollectionReference GetMaterialsCollection(string canvasAppId, FirebaseFirestore db)
    {
        return db.Collection(COLLECTION_ARTIFACTS)
                 .Document(canvasAppId)
                 .Collection(COLLECTION_PUBLIC)
                 .Document(DOCUMENT_DATA)
                 .Collection(COLLECTION_MATERIALS);
    }

    public static CollectionReference GetTaskMaterialsCollection(string canvasAppId, FirebaseFirestore db, string taskId)
    {
        return GetTasksCollection(canvasAppId, db).Document(taskId).Collection(COLLECTION_MATERIALS);
    }
}