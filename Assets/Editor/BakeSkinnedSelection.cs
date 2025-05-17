#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

public static class BakeSkinnedSelection
{
    [MenuItem("Tools/Bake Selected SkinnedMesh → Static Mesh")]
    private static void BakeSelected()
    {
        var selection = Selection.GetFiltered<SkinnedMeshRenderer>(SelectionMode.Deep);
        if (selection.Length == 0)
        {
            Debug.LogWarning("Нет выделенных SkinnedMeshRenderer!");
            return;
        }
        foreach (var sk in selection) BakeOne(sk);
    }

    [MenuItem("Tools/Bake Selected SkinnedMesh → Static Mesh", true)]
    private static bool ValidateBakeSelected()
        => Selection.GetFiltered<SkinnedMeshRenderer>(SelectionMode.Deep).Length > 0;

    private static void BakeOne(SkinnedMeshRenderer skinned)
    {
        // 0) Начинаем группу Undo, чтобы все действия откатывались сразу
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();

        // 1) Запекаем текущую деформированную геометрию
        Mesh baked = new Mesh();
        skinned.BakeMesh(baked);
        
        // Сохраняем меш как asset для корректной работы с префабами
        string meshPath = SaveMeshAsAsset(baked, skinned.name);
        
        // После сохранения загружаем меш заново, чтобы использовать сохраненный asset
        Mesh savedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);

        // 2) Создаём новый GameObject
        GameObject go = new GameObject(skinned.name + "_Static");
        Undo.RegisterCreatedObjectUndo(go, "Create baked mesh");
        
        // Устанавливаем родителя как родитель оригинального объекта
        go.transform.SetParent(skinned.transform.parent);

        // 3) Позиция/ротация: по кости rootBone или самому объекту
        Transform anchor = skinned.rootBone != null ? skinned.rootBone : skinned.transform;
        go.transform.position = anchor.position;
        go.transform.rotation = anchor.rotation;
        go.transform.localScale = Vector3.one; // масштаб уже в меше

        // 4) Добавляем MeshFilter и сразу назначаем запечённый меш
        var mf = Undo.AddComponent<MeshFilter>(go);
        mf.sharedMesh = savedMesh; // Используем сохраненный меш

        // 5) Добавляем MeshRenderer и копируем материалы
        var mr = Undo.AddComponent<MeshRenderer>(go);
        mr.sharedMaterials = skinned.sharedMaterials;
        
        // Копируем дополнительные настройки рендерера
        mr.shadowCastingMode = skinned.shadowCastingMode;
        mr.receiveShadows = skinned.receiveShadows;
        mr.lightProbeUsage = skinned.lightProbeUsage;
        mr.reflectionProbeUsage = skinned.reflectionProbeUsage;

        // 6) Добавляем MeshCollider и сразу подставляем тот же меш
        var mc = Undo.AddComponent<MeshCollider>(go);
        mc.sharedMesh = savedMesh; // Используем сохраненный меш
        mc.convex = true;

        // 7) Отключаем исходный SkinnedMeshRenderer (но не удаляем его)
        Undo.RecordObject(skinned, "Disable original SkinnedMeshRenderer");
        skinned.enabled = false;

        // 8) Завершаем группу Undo
        Undo.CollapseUndoOperations(group);
        
        // Выбираем созданный объект
        Selection.activeGameObject = go;
    }
    
    private static string SaveMeshAsAsset(Mesh mesh, string baseName)
    {
        // Создаем директорию если её нет
        string directory = "Assets/BakedMeshes";
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
            AssetDatabase.Refresh();
        }
        
        // Формируем уникальное имя для файла
        string assetPath = $"{directory}/{baseName}_Baked_{System.DateTime.Now.Ticks}.asset";
        
        // Сохраняем меш как asset
        AssetDatabase.CreateAsset(mesh, assetPath);
        AssetDatabase.SaveAssets();
        
        return assetPath;
    }
}
#endif