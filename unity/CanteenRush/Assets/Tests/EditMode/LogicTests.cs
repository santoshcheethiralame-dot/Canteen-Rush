using System;
using CanteenRush.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;

public class LogicTests
{
    GameConfig config;
    [SetUp] public void SetUp() { config = ScriptableObject.CreateInstance<GameConfig>(); }
    [TearDown] public void TearDown() { UnityEngine.Object.DestroyImmediate(config); GameEvents.ResetAll(); }

    [Test] public void MatchesMultisetsAndIgnoresNone()
    {
        Assert.AreEqual((2, 1, 1), OrderMatcher.Evaluate(
            new[] { ItemId.Donut, ItemId.Donut, ItemId.Cola, ItemId.None },
            new[] { ItemId.Cola, ItemId.Donut, ItemId.Cake, ItemId.None }));
        Assert.AreEqual((0, 0, 0), OrderMatcher.Evaluate(Array.Empty<ItemId>(), Array.Empty<ItemId>()));
        Assert.AreEqual((1, 0, 0), OrderMatcher.Evaluate(new[] { ItemId.Water }, new[] { ItemId.Water }));
        Assert.AreEqual((0, 0, 1), OrderMatcher.Evaluate(new[] { ItemId.Water }, Array.Empty<ItemId>()));
        Assert.AreEqual((0, 1, 0), OrderMatcher.Evaluate(Array.Empty<ItemId>(), new[] { ItemId.Water }));
    }

    [Test] public void ScoresPerfectPartialAndEmpty()
    {
        var order = new Order { Id = 1, Items = new[] { ItemId.Donut, ItemId.Cola, ItemId.Cake } };
        var perfect = ScoreCalculator.Score(order, order.Items, 1f, config);
        Assert.AreEqual(500, perfect.Points);
        Assert.AreEqual(ServeOutcome.Perfect, perfect.Outcome);
        var partial = ScoreCalculator.Score(order, new[] { ItemId.Donut, ItemId.Water }, .5f, config);
        Assert.AreEqual(100, partial.Points);
        Assert.AreEqual(ServeOutcome.Partial, partial.Outcome);
        var empty = ScoreCalculator.Score(order, Array.Empty<ItemId>(), 1f, config);
        Assert.AreEqual(0, empty.Points);
        Assert.AreEqual(ServeOutcome.Empty, empty.Outcome);
        Assert.AreEqual(0, empty.SpeedBonus);
        Assert.AreEqual(0, ScoreCalculator.Score(order, new[] { ItemId.Donut }, 0f, config).SpeedBonus);
    }

    [TestCase(299, 0)] [TestCase(300, 1)] [TestCase(700, 2)] [TestCase(1200, 3)]
    public void StarThresholds(int score, int expected) { Assert.AreEqual(expected, ScoreCalculator.Stars(score, config)); }

    [TestCase(1f, 150)] [TestCase(.5f, 125)] [TestCase(0f, 100)]
    public void SingleItemScoreIncludesOnlyTheCorrectSpeedBonus(float patience, int points)
    {
        var order = new Order { Items = new[] { ItemId.Donut } };
        Assert.AreEqual(points, ScoreCalculator.Score(order, order.Items, patience, config).Points);
    }

    [Test] public void ExtraDuplicatesArePenalizedWithoutACombo()
    {
        var order = new Order { Items = new[] { ItemId.Donut, ItemId.Cola } };
        var result = ScoreCalculator.Score(order, new[] { ItemId.Donut, ItemId.Cola, ItemId.Cola }, 0, config);
        Assert.AreEqual(175, result.Points);
        Assert.AreEqual(ServeOutcome.Partial, result.Outcome);
        Assert.AreEqual(1, result.Wrong);
        Assert.AreEqual(-10, ScoreCalculator.Spill(config));
        Assert.AreEqual(-50, ScoreCalculator.Timeout(config));
    }

    [TestCase(60f, 11f, 3)] [TestCase(120f, 9f, 4)]
    public void LaterWaveSpawnCadenceAndCapacity(float start, float interval, int capacity)
    {
        var wave = new WaveDirector(config, new System.Random(1)); wave.Tick(start);
        wave.Tick(interval - .01f); Assert.IsFalse(wave.ShouldSpawnCustomer(0, 1));
        wave.Tick(.02f); Assert.IsFalse(wave.ShouldSpawnCustomer(capacity, 1));
        Assert.IsFalse(wave.ShouldSpawnCustomer(capacity - 1, 1));
        wave.Tick(interval); Assert.IsTrue(wave.ShouldSpawnCustomer(capacity - 1, 1));
    }

    [Test] public void WavesFollowBoundariesAndSpawnCadence()
    {
        var wave = new WaveDirector(config, new System.Random(7));
        Assert.AreEqual(RoundState.Wave1, wave.Current);
        wave.Tick(14); Assert.IsFalse(wave.ShouldSpawnCustomer(2, 1));
        wave.Tick(14); Assert.IsFalse(wave.ShouldSpawnCustomer(0, 0));
        wave.Tick(14); Assert.IsTrue(wave.ShouldSpawnCustomer(0, 1));
        Assert.IsFalse(wave.ShouldSpawnCustomer(0, 1));
        wave.Tick(18); Assert.AreEqual(RoundState.Wave2, wave.Current);
        wave.Tick(60); Assert.AreEqual(RoundState.Wave3, wave.Current);
        wave.Tick(60); Assert.AreEqual(RoundState.Results, wave.Current);
        Assert.IsFalse(wave.ShouldSpawnCustomer(0, 1));
    }

    [Test] public void TemplateSelectionIsSeededAndRespectsWaveLimits()
    {
        var one = ScriptableObject.CreateInstance<OrderTemplate>();
        var two = ScriptableObject.CreateInstance<OrderTemplate>();
        var three = ScriptableObject.CreateInstance<OrderTemplate>();
        try
        {
            one.Items = new[] { ItemId.Donut }; one.MinWave = 1;
            two.Items = new[] { ItemId.Donut, ItemId.Cola }; two.MinWave = 2;
            three.Items = new[] { ItemId.Donut, ItemId.Cola, ItemId.Cake }; three.MinWave = 3;
            var serialized = new SerializedObject(config);
            var templates = serialized.FindProperty("Templates"); templates.arraySize = 3;
            templates.GetArrayElementAtIndex(0).objectReferenceValue = one;
            templates.GetArrayElementAtIndex(1).objectReferenceValue = two;
            templates.GetArrayElementAtIndex(2).objectReferenceValue = three;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var a = new WaveDirector(config, new System.Random(123));
            var b = new WaveDirector(config, new System.Random(123));
            Assert.AreSame(one, a.NextTemplate());
            a.Tick(60); b.Tick(60);
            // Advance b's RNG once to match the first wave choice.
            var c = new WaveDirector(config, new System.Random(123)); c.NextTemplate(); c.Tick(60);
            for (int i = 0; i < 20; i++) Assert.AreSame(a.NextTemplate(), c.NextTemplate());
            b.Tick(60);
            for (int i = 0; i < 20; i++) Assert.GreaterOrEqual(b.NextTemplate().Items.Length, 2);
        }
        finally { UnityEngine.Object.DestroyImmediate(one); UnityEngine.Object.DestroyImmediate(two); UnityEngine.Object.DestroyImmediate(three); }
    }

    [Test] public void EventResetRemovesSubscribers()
    {
        int count = 0; GameEvents.OnUiClick += () => count++;
        GameEvents.UiClick(); GameEvents.ResetAll(); GameEvents.UiClick();
        Assert.AreEqual(1, count);
    }

    [Test] public void ConfiguredAssetsContainTheExpectedOrdersAndDefaults()
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/Data/GameConfig.asset");
        Assert.NotNull(asset);
        CollectionAssert.AreEqual(new[] { 2, 3, 4 }, asset.WaveMaxCustomers);
        var director = new WaveDirector(asset, new System.Random(1));
        Assert.AreEqual(1, director.NextTemplate().Items.Length);
        director.Tick(120);
        for (int i = 0; i < 20; i++) Assert.GreaterOrEqual(director.NextTemplate().Items.Length, 2);
    }
}
