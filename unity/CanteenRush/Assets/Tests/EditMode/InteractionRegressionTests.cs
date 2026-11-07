using System.Collections;
using CanteenRush.Core;
using CanteenRush.Gameplay;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public sealed class InteractionRegressionTests
{
    static IEnumerator WaitGame(float seconds) { float until = Time.time + seconds; while (Time.time < until) yield return null; }
    [UnityTearDown] public IEnumerator Cleanup()
    {
        Time.timeScale = 1;
        if (Application.isPlaying)
        {
            var game = Object.FindFirstObjectByType<GameManager>(); if (game != null) game.Menu();
            // XRI finalizes detached attach transforms at the end of the player frame.
            yield return null; yield return null;
            yield return new ExitPlayMode();
        }
        GameEvents.ResetAll();
    }
    [UnityTest] public IEnumerator PumpRequiresOverlapAndFridgeControlsFreshCola()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Sandbox_P2.unity");
        yield return new EnterPlayMode();
        yield return null; yield return null;
        var cup = Object.FindFirstObjectByType<CupFill>();
        var cooler = Object.FindFirstObjectByType<WaterCooler>();
        var pump = cooler.GetComponentInChildren<XRSimpleInteractable>();
        pump.activated.Invoke(new ActivateEventArgs());
        yield return WaitGame(.15f);
        Assert.AreEqual(ItemId.None, cup.GetComponent<ItemBehaviour>().Id);
        var cupBody = cup.GetComponent<Rigidbody>(); cupBody.useGravity = false;
        cupBody.linearVelocity = Vector3.zero; cupBody.angularVelocity = Vector3.zero;
        cupBody.position = cooler.transform.position;
        yield return new WaitForFixedUpdate(); yield return null;
        Time.timeScale = 5;
        yield return WaitGame(2.1f);
        Assert.AreEqual(ItemId.None, cup.GetComponent<ItemBehaviour>().Id, "Partial fill must remain an empty-cup item.");
        yield return WaitGame(2.1f);
        Time.timeScale = 1;
        Assert.AreEqual(ItemId.Water, cup.GetComponent<ItemBehaviour>().Id, "Pump plus overlap must fill the cup.");
        pump.deactivated.Invoke(new DeactivateEventArgs());
        FridgeDoor upper = null;
        foreach (var door in Object.FindObjectsByType<FridgeDoor>(FindObjectsSortMode.None)) if (door.DoorIndex == 0) upper = door;
        ItemBehaviour cola = null;
        foreach (var item in Object.FindObjectsByType<ItemBehaviour>(FindObjectsSortMode.None)) if (item.Id == ItemId.Cola) cola = item;
        Assert.IsFalse(cola.GetComponent<XRGrabInteractable>().enabled);
        upper.GetComponent<Rigidbody>().rotation *= Quaternion.Euler(0, 30, 0);
        yield return new WaitForFixedUpdate(); yield return null;
        Assert.IsTrue(upper.IsOpen);
        Assert.IsTrue(cola.GetComponent<XRGrabInteractable>().enabled);
        upper.ResetDoor();
        yield return new WaitForFixedUpdate(); yield return null;
        Assert.IsFalse(upper.IsOpen);
        Assert.IsFalse(cola.GetComponent<XRGrabInteractable>().enabled);
    }
    [UnityTest] public IEnumerator ReturningTrayCannotEjectAReplacementAndReplayCancelsItsTimer()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Sandbox_P2.unity");
        yield return new EnterPlayMode();
        yield return null; yield return null;
        var game = Object.FindFirstObjectByType<GameManager>();
        var orders = Object.FindFirstObjectByType<OrderService>();
        GameEvents.ItemGrabbed(ItemId.Donut, Hand.Left, Vector3.zero);
        GameEvents.ItemSocketed(ItemId.Donut, Vector3.zero);
        orders.Serve(0, new[] { ItemId.Donut }); game.Tick(0);
        yield return null;
        var trays = Object.FindObjectsByType<Tray>(FindObjectsSortMode.None);
        var original = trays[0]; var replacement = trays[1];
        var xr = Object.FindFirstObjectByType<XRInteractionManager>();
        var table = Object.FindFirstObjectByType<ServeSocket>().GetComponent<XRSocketInteractor>();
        xr.SelectEnter((IXRSelectInteractor)table, (IXRSelectInteractable)original.GetComponent<XRGrabInteractable>());
        original.BeginReturn();
        xr.SelectExit((IXRSelectInteractor)table, (IXRSelectInteractable)original.GetComponent<XRGrabInteractable>());
        xr.SelectEnter((IXRSelectInteractor)table, (IXRSelectInteractable)replacement.GetComponent<XRGrabInteractable>());
        Time.timeScale = 5;
        yield return WaitGame(3.2f);
        Time.timeScale = 1;
        Assert.IsFalse(original.Returning);
        Assert.AreEqual(replacement.transform, table.firstInteractableSelected.transform);
        original.BeginReturn(); game.Play();
        Assert.IsFalse(original.Returning);
        Assert.AreEqual(0, original.Contents.Count);
        Assert.AreEqual(0, table.interactablesSelected.Count);
    }
}
