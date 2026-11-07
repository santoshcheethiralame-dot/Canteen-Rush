using CanteenRush.Core;
using CanteenRush.Gameplay;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class PrefabTests
{
    [TestCase("Donut")] [TestCase("Cola")] [TestCase("Shake")] [TestCase("Cake")] [TestCase("Cup")]
    public void ItemPrefabHasPhysicsLogicAndVisual(string item)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Gameplay/Item_" + item + ".prefab");
        Assert.NotNull(prefab);
        Assert.NotNull(prefab.GetComponent<ItemBehaviour>());
        Assert.NotNull(prefab.GetComponent<Rigidbody>());
        Assert.NotNull(prefab.GetComponent<Collider>());
        Assert.NotNull(prefab.transform.Find("Visual"));
        Assert.NotNull(new SerializedObject(prefab.GetComponent<ItemBehaviour>()).FindProperty("_config").objectReferenceValue);
    }
    [Test] public void TrayContainsFourConfiguredSockets()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Gameplay/Prop_Tray.prefab");
        Assert.NotNull(prefab);
        var tray = prefab.GetComponent<Tray>(); Assert.NotNull(tray);
        var sockets = new SerializedObject(tray).FindProperty("_sockets");
        Assert.AreEqual(4, sockets.arraySize);
        for (int i = 0; i < sockets.arraySize; i++) Assert.NotNull(sockets.GetArrayElementAtIndex(i).objectReferenceValue);
    }
    [TestCase("Donut")] [TestCase("Cola")] [TestCase("Shake")] [TestCase("Cake")] [TestCase("Water")]
    public void ItemDefinitionReferencesPrefab(string item)
    {
        var def = AssetDatabase.LoadAssetAtPath<ItemDef>("Assets/Data/Items/Item_" + item + ".asset");
        Assert.NotNull(def); Assert.NotNull(def.Prefab);
    }
    [Test] public void SystemsPrefabHasConnectedRoundServices()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Systems/Systems.prefab");
        Assert.NotNull(prefab);
        var manager = prefab.GetComponent<GameManager>(); Assert.NotNull(manager);
        var fields = new SerializedObject(manager);
        foreach (var field in new[] { "_config", "_orders", "_score", "_customers" })
            Assert.NotNull(fields.FindProperty(field).objectReferenceValue, field);
    }
}
