using System.Collections;
using CanteenRush.Core;
using CanteenRush.Gameplay;
using CanteenRush.UI.Gameplay;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public sealed class SandboxSmokeTests
{
    static int _fillCount;
    static void CountFill(Vector3 position) { _fillCount++; }
    [UnityTearDown] public IEnumerator Cleanup()
    {
        Time.timeScale = 1;
        if (Application.isPlaying) yield return new ExitPlayMode();
        GameEvents.ResetAll();
    }
    [UnityTest] public IEnumerator SandboxPracticeServeAndThreeReplays()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Sandbox_P2.unity");
        yield return new EnterPlayMode();
        yield return null; yield return null;
        var game = Object.FindFirstObjectByType<GameManager>();
        var orders = Object.FindFirstObjectByType<OrderService>();
        Assert.AreEqual(RoundState.Tutorial, game.State);
        Assert.AreEqual(180, game.Remaining);
        Assert.AreEqual(5, Object.FindObjectsByType<ItemBehaviour>(FindObjectsSortMode.None).Length);
        Assert.AreEqual(2, Object.FindObjectsByType<Tray>(FindObjectsSortMode.None).Length);
        Assert.NotNull(orders.GetOrder(0));
        Assert.Greater(Object.FindObjectsByType<CustomerBubble>(FindObjectsSortMode.None).Length, 0);
        var tray = Object.FindFirstObjectByType<Tray>();
        Vector3 home = tray.transform.position;
        ItemBehaviour donut = null, cola = null;
        foreach (var item in Object.FindObjectsByType<ItemBehaviour>(FindObjectsSortMode.None))
        { if (item.Id == ItemId.Donut) donut = item; if (item.Id == ItemId.Cola) cola = item; }
        Assert.NotNull(donut); Assert.NotNull(cola);
        Assert.IsFalse(cola.GetComponent<XRGrabInteractable>().enabled, "Closed fridge must protect its cola.");
        var cup = Object.FindFirstObjectByType<CupFill>();
        Assert.NotNull(cup);
        cup.Fill(2); Assert.AreEqual(ItemId.None, cup.GetComponent<ItemBehaviour>().Id);
        cup.Fill(2); Assert.AreEqual(ItemId.Water, cup.GetComponent<ItemBehaviour>().Id);
        _fillCount = 0;
        GameEvents.OnCupFilled += CountFill;
        cup.Fill(8); Assert.AreEqual(0, _fillCount, "A full cup must not fire completion twice.");
        GameEvents.OnCupFilled -= CountFill;
        Debug.Log("P2 smoke: cup checks complete");
        GameEvents.ItemGrabbed(ItemId.Donut, Hand.Left, donut.transform.position);
        Debug.Log("P2 smoke: donut grab hook complete");
        var xr = Object.FindFirstObjectByType<XRInteractionManager>();
        Assert.NotNull(xr);
        var socket = tray.GetComponentInChildren<XRSocketInteractor>();
        Assert.NotNull(socket);
        xr.SelectEnter((IXRSelectInteractor)socket, (IXRSelectInteractable)donut.GetComponent<XRGrabInteractable>());
        Assert.AreEqual(1, tray.Contents.Count);
        ServeSocket target = null;
        foreach (var serve in Object.FindObjectsByType<ServeSocket>(FindObjectsSortMode.None)) if (serve.SeatIndex == 0) target = serve;
        Assert.NotNull(target);
        Debug.Log("P2 smoke: item socketed, serving next");
        xr.SelectEnter((IXRSelectInteractor)target.GetComponent<XRSocketInteractor>(), (IXRSelectInteractable)tray.GetComponent<XRGrabInteractable>());
        yield return null;
        Assert.AreEqual(RoundState.Wave1, game.State);
        Assert.AreEqual(0, Object.FindFirstObjectByType<ScoreService>().Total);
        Assert.IsFalse(tray.Returning);
        Assert.AreEqual(0, tray.Contents.Count);
        Assert.Less(Vector3.Distance(home, tray.transform.position), .1f);
        Time.timeScale = 5;
        float untilOrder = Time.time + 30;
        while (Time.time < untilOrder) yield return null;
        Time.timeScale = 1;
        Customer waiting = null;
        foreach (var customer in Object.FindObjectsByType<Customer>(FindObjectsSortMode.None))
            if (customer.State == CustomerState.Waiting) { waiting = customer; break; }
        Assert.NotNull(waiting, "A normal customer must navigate to a seat and order.");
        var visual = waiting.transform.Find("Visual");
        Assert.Greater(visual.localPosition.y, .7f, "Walking bob must preserve the body height.");
        Assert.GreaterOrEqual(ItemBehaviour.ActiveCount, 5);
        Assert.LessOrEqual(ItemBehaviour.ActiveCount, 34);
        for (int i = 0; i < 3; i++)
        {
            game.Tick(180);
            Assert.AreEqual(RoundState.Results, game.State);
            int frozen = Object.FindFirstObjectByType<ScoreService>().Total;
            GameEvents.ItemDropped(ItemId.Donut, 1, Vector3.zero);
            Assert.AreEqual(frozen, Object.FindFirstObjectByType<ScoreService>().Total);
            game.Play(); yield return null;
            Assert.AreEqual(RoundState.Wave1, game.State);
            Assert.AreEqual(0, tray.Contents.Count);
            Assert.IsFalse(tray.Returning);
        }
    }
    [UnityTest] public IEnumerator MainReceivesGameplayWithoutEditingTheWorldScene()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");
        yield return new EnterPlayMode();
        yield return null; yield return null;
        var game = Object.FindFirstObjectByType<GameManager>();
        Assert.NotNull(game);
        Assert.AreEqual(1, Object.FindObjectsByType<GameManager>(FindObjectsSortMode.None).Length);
        Assert.AreEqual(18, Object.FindObjectsByType<ItemSpawner>(FindObjectsSortMode.None).Length);
        Assert.AreEqual(8, Object.FindObjectsByType<ServeSocket>(FindObjectsSortMode.None).Length);
        Assert.AreEqual(2, Object.FindObjectsByType<Tray>(FindObjectsSortMode.None).Length);
        Assert.NotNull(Object.FindFirstObjectByType<ComfortSettings>());
        Assert.AreEqual(2, Object.FindObjectsByType<FridgeDoor>(FindObjectsSortMode.None).Length);
        float settle = Time.time + 1;
        while (Time.time < settle) yield return null;
        foreach (var spawner in Object.FindObjectsByType<ItemSpawner>(FindObjectsSortMode.None))
        {
            Assert.NotNull(spawner.Current, "A fixture must retain its displayed item: " + spawner.name);
            Assert.Less(Vector3.Distance(spawner.Current.transform.position, spawner.transform.position), .3f, "Item should remain at its fixture: " + spawner.name);
            if (spawner.Current.Id == ItemId.Cola) Assert.IsFalse(spawner.Current.GetComponent<XRGrabInteractable>().enabled);
        }
        game.Play(); yield return null;
        Assert.AreEqual(RoundState.Tutorial, game.State);
        Assert.NotNull(Object.FindFirstObjectByType<OrderService>().GetOrder(0));
        Assert.LessOrEqual(Object.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None).Length, 40);
        if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
        {
            var customer = Object.FindFirstObjectByType<Customer>();
            var preview = new GameObject("P2 preview").AddComponent<Camera>();
            preview.CopyFrom(Camera.main); preview.enabled = false;
            Vector3 towardPlayer = Camera.main.transform.position - customer.transform.position; towardPlayer.y = 0;
            preview.transform.position = customer.transform.position + towardPlayer.normalized * 3 + Vector3.up * 1.4f;
            preview.transform.LookAt(customer.transform.position + Vector3.up * 1.2f);
            var bubble = customer.GetComponentInChildren<CustomerBubble>();
            var data = new UnityEditor.SerializedObject(bubble); data.FindProperty("_viewer").objectReferenceValue = preview.transform; data.ApplyModifiedPropertiesWithoutUndo();
            yield return null;
            var target = new RenderTexture(1280, 900, 24); preview.targetTexture = target;
            preview.Render(); RenderTexture.active = target;
            var pixels = new Texture2D(1280, 900, TextureFormat.RGB24, false);
            pixels.ReadPixels(new Rect(0, 0, 1280, 900), 0, 0); pixels.Apply();
            System.IO.File.WriteAllBytes(System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../../../../p2-customer.png")), pixels.EncodeToPNG());
            RenderTexture.active = null; preview.targetTexture = null;
            Object.Destroy(pixels); Object.Destroy(target); Object.Destroy(preview.gameObject);
        }
        game.Menu(); yield return null;
        var stock = Object.FindObjectsByType<ItemSpawner>(FindObjectsSortMode.None);
        Time.timeScale = 10;
        for (int take = 0; take < 3; take++)
        {
            foreach (var spawner in stock)
            {
                if (spawner.Current == null) continue;
                var body = spawner.Current.GetComponent<Rigidbody>(); body.useGravity = false;
                body.position += Vector3.up * 3;
                if (!body.isKinematic) { body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; }
            }
            float refill = Time.time + 11;
            while (Time.time < refill) yield return null;
            Assert.LessOrEqual(ItemBehaviour.ActiveCount, 34, "Taking stock repeatedly must respect the item cap.");
            Assert.LessOrEqual(Object.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None).Length, 40);
        }
        Time.timeScale = 1;
        Assert.AreEqual(34, ItemBehaviour.ActiveCount, "Exercise the full item cap, not just the initial stock.");
    }
}
