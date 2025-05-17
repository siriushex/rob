using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class DisasterRigidbodySetup : EditorWindow
{
    [MenuItem("Tools/Disaster Tools/Setup Rigidbodies %#r")]
    private static void SetupDisasterObjects()
    {
        // 1. Найти все объекты и создать их копию
        var allDestructibles = Object.FindObjectsOfType<DisasterDestructible>(true);
        var objectsToProcess = new List<DisasterDestructible>(allDestructibles);

        int rigidbodiesAdded = 0;
        int componentsRemoved = 0;

        foreach (var obj in objectsToProcess)
        {
            // 2. Пропустить уничтоженные объекты
            if (obj == null || obj.gameObject == null) continue;

            // 3. Удаление дубликатов компонента
            var components = new List<DisasterDestructible>(obj.GetComponents<DisasterDestructible>());
            if (components.Count > 1)
            {
                // Удаляем все кроме первого компонента (с конца)
                for (int i = components.Count - 1; i >= 1; i--)
                {
                    var component = components[i];
                    if (component != null)
                    {
                        Undo.DestroyObjectImmediate(component);
                        componentsRemoved++;
                    }
                }
            }

            // 4. Двойная проверка после удаления компонентов
            if (obj == null) continue;

            // 5. Работа с Rigidbody
            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Undo.RecordObject(rb, "Update Rigidbody");
                rb.isKinematic = true;
                rb.mass = 1f;
            }
            else
            {
                rb = Undo.AddComponent<Rigidbody>(obj.gameObject);
                rb.isKinematic = true;
                rb.mass = 1f;
                rigidbodiesAdded++;
            }
        }

        Debug.Log($"Успешно обработано объектов: {objectsToProcess.Count}\n" +
                 $"Добавлено Rigidbody: {rigidbodiesAdded}\n" +
                 $"Удалено дубликатов: {componentsRemoved}");
    }
}