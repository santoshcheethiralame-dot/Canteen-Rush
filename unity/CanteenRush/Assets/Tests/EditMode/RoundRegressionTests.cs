using CanteenRush.Core;
using CanteenRush.Gameplay;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class RoundRegressionTests
{
    [Test] public void TutorialKeepsTheFullRoundAndAlwaysRequestsADonut()
    {
        var systems = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Systems/Systems.prefab"));
        try
        {
            var game = systems.GetComponent<GameManager>();
            typeof(GameManager).GetMethod("OnEnable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(game, null);
            game.Play();
            game.Tick(200);
            Assert.AreEqual(RoundState.Tutorial, game.State);
            Assert.AreEqual(180, game.Remaining);
            var order = systems.GetComponent<OrderService>().GetOrder(0);
            Assert.NotNull(order);
            CollectionAssert.AreEqual(new[] { ItemId.Donut }, order.Items);
            Assert.AreEqual(order.PatienceMax, order.PatienceRemaining);
            GameEvents.ItemGrabbed(ItemId.Donut, Hand.Left, Vector3.zero);
            GameEvents.ItemSocketed(ItemId.Donut, Vector3.zero);
            systems.GetComponent<OrderService>().Serve(0, new[] { ItemId.Donut });
            game.Tick(0);
            Assert.AreEqual(RoundState.Wave1, game.State);
            Assert.AreEqual(0, systems.GetComponent<ScoreService>().Total, "Practice score must not enter the timed round.");
            for (int round = 0; round < 3; round++)
            {
                game.Tick(180);
                Assert.AreEqual(RoundState.Results, game.State);
                Assert.AreEqual(0, game.Remaining);
                game.Play();
                Assert.AreEqual(RoundState.Wave1, game.State);
                Assert.AreEqual(180, game.Remaining);
            }
        }
        finally { Object.DestroyImmediate(systems); GameEvents.ResetAll(); }
    }

    [Test] public void UnmatchedItemsAndDuplicateShortagesAreCountedIndependently()
    {
        Assert.AreEqual((1, 0, 1), OrderMatcher.Evaluate(new[] { ItemId.Donut, ItemId.Donut }, new[] { ItemId.Donut }));
        Assert.AreEqual((2, 0, 0), OrderMatcher.Evaluate(new[] { ItemId.Cola, ItemId.Donut }, new[] { ItemId.Donut, ItemId.Cola }));
    }
}
