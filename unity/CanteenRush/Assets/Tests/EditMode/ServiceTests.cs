using System.Collections.Generic;
using CanteenRush.Core;
using CanteenRush.Gameplay;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class ServiceTests
{
    GameObject root;
    GameConfig config;
    OrderTemplate template;
    ScoreService score;
    OrderService orders;

    [SetUp] public void SetUp()
    {
        config = ScriptableObject.CreateInstance<GameConfig>();
        template = ScriptableObject.CreateInstance<OrderTemplate>(); template.Items = new[] { ItemId.Donut };
        root = new GameObject("service tests");
        score = root.AddComponent<ScoreService>(); orders = root.AddComponent<OrderService>();
        SetReference(score, "_config", config); SetReference(orders, "_config", config); SetReference(orders, "_score", score);
        // Non-ExecuteAlways MonoBehaviours do not receive OnEnable in EditMode.
        typeof(ScoreService).GetMethod("OnEnable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(score, null);
    }
    static void SetReference(Object target, string name, Object value)
    {
        var serialized = new SerializedObject(target); serialized.FindProperty(name).objectReferenceValue = value; serialized.ApplyModifiedPropertiesWithoutUndo();
    }
    [TearDown] public void TearDown()
    {
        typeof(ScoreService).GetMethod("OnDisable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(score, null);
        Object.DestroyImmediate(root); Object.DestroyImmediate(config); Object.DestroyImmediate(template); GameEvents.ResetAll();
    }
    [Test] public void ScoreIsFlooredAndSpillsAreApplied()
    {
        score.Apply(5); GameEvents.ItemDropped(ItemId.Donut, 2, Vector3.zero);
        Assert.AreEqual(0, score.Total);
        score.Apply(25); GameEvents.ItemDropped(ItemId.Cola, 2, Vector3.zero);
        Assert.AreEqual(15, score.Total);
    }
    [Test] public void ServeScoresOnlyTheActiveSeatAndRemovesItsOrder()
    {
        var order = orders.Place(1, Vector3.one, template);
        Assert.IsNull(orders.Place(1, Vector3.one, template));
        Assert.IsFalse(orders.Serve(0, new[] { ItemId.Donut }));
        Assert.IsTrue(orders.Serve(1, new[] { ItemId.Donut }));
        Assert.AreEqual(150, score.Total); Assert.IsFalse(orders.HasOrder(1));
        Assert.AreEqual(1, order.Id);
    }
    [Test] public void TimeoutPenalizesOnceAndReleasesTheSeat()
    {
        int expired = 0; GameEvents.OnOrderExpired += _ => expired++;
        score.Apply(100); orders.Place(1, Vector3.zero, template);
        orders.Tick(39); Assert.AreEqual(0, expired); Assert.IsTrue(orders.HasOrder(1));
        orders.Tick(1); orders.Tick(1);
        Assert.AreEqual(1, expired); Assert.AreEqual(50, score.Total); Assert.IsFalse(orders.HasOrder(1));
    }
    [Test] public void ExpirationListenerMayResetOrders()
    {
        orders.Place(0, Vector3.zero, template); orders.Place(1, Vector3.zero, template);
        GameEvents.OnOrderExpired += _ => orders.ResetOrders();
        Assert.DoesNotThrow(() => orders.Tick(40));
    }
    [Test] public void PatienceListenerMayResetOrders()
    {
        orders.Place(0, Vector3.zero, template); orders.Place(1, Vector3.zero, template);
        GameEvents.OnOrderPatience += (_, __) => orders.ResetOrders();
        Assert.DoesNotThrow(() => orders.Tick(.25f));
    }
}
