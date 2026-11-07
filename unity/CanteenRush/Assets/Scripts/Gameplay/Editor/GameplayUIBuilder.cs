using System.IO;
using CanteenRush.Core;
using CanteenRush.UI.Gameplay;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
namespace CanteenRush.Gameplay.Editor
{
    public static class GameplayUIBuilder
    {
        const string Folder = "Assets/Prefabs/UIGameplay/";
        static ItemDef[] _items;
        static Sprite _ring, _square;
        static void Ref(Object target, string name, Object value)
        { var data = new SerializedObject(target); data.FindProperty(name).objectReferenceValue = value; data.ApplyModifiedPropertiesWithoutUndo(); }
        static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 position)
        {
            var obj = new GameObject(name, typeof(RectTransform)); obj.layer = LayerMask.NameToLayer("WorldUI");
            obj.transform.SetParent(parent, false); var rect = obj.GetComponent<RectTransform>(); rect.sizeDelta = size; rect.anchoredPosition = position; return rect;
        }
        static Text Label(string name, Transform parent, Vector2 size, Vector2 position, int font = 28)
        {
            var text = Rect(name, parent, size, position).gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.fontSize = font;
            text.alignment = TextAnchor.MiddleCenter; text.color = Color.white; text.raycastTarget = false; return text;
        }
        static Sprite SpriteAsset(string name, Color colour, bool ring = false, bool square = false)
        {
            string path = "Assets/Data/Icons/" + name + ".png";
            var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
            {
                float r = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(32, 32));
                texture.SetPixel(x, y, square || r < 30 && (!ring || r > 23) ? colour : Color.clear);
            }
            texture.Apply(); File.WriteAllBytes(path, texture.EncodeToPNG()); Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path); importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single; importer.mipmapEnabled = false; importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        static GameObject CanvasRoot(string name, Vector2 size)
        {
            var root = Rect(name, null, size, Vector2.zero).gameObject;
            root.transform.localScale = Vector3.one * .002f;
            root.AddComponent<Canvas>().renderMode = RenderMode.WorldSpace; return root;
        }
        static OrderTicket Ticket(Transform parent, bool bubble)
        {
            var rect = Rect("Ticket", parent, new Vector2(260, 240), Vector2.zero);
            var background = rect.gameObject.AddComponent<Image>(); background.color = new Color(.06f, .09f, .15f, .96f); background.raycastTarget = false;
            var ticket = rect.gameObject.AddComponent<OrderTicket>();
            Ref(ticket, "_seat", Label("Seat", rect, new Vector2(240, 40), new Vector2(0, 90), 30));
            var meter = Rect("Patience", rect, bubble ? new Vector2(300, 300) : new Vector2(230, 16), bubble ? Vector2.zero : new Vector2(0, -98)).gameObject.AddComponent<Image>();
            meter.sprite = bubble ? _ring : _square; meter.type = Image.Type.Filled;
            meter.fillMethod = bubble ? Image.FillMethod.Radial360 : Image.FillMethod.Horizontal; meter.raycastTarget = false; Ref(ticket, "_patience", meter);
            var data = new SerializedObject(ticket); var icons = data.FindProperty("_icons"); var labels = data.FindProperty("_itemLabels"); var defs = data.FindProperty("_items");
            icons.arraySize = labels.arraySize = 3; defs.arraySize = _items.Length;
            for (int i = 0; i < _items.Length; i++) defs.GetArrayElementAtIndex(i).objectReferenceValue = _items[i];
            for (int i = 0; i < 3; i++)
            {
                var icon = Rect("Icon_" + i, rect, new Vector2(38, 38), new Vector2(-90, 40 - i * 52)).gameObject.AddComponent<Image>(); icon.raycastTarget = false;
                icons.GetArrayElementAtIndex(i).objectReferenceValue = icon;
                labels.GetArrayElementAtIndex(i).objectReferenceValue = Label("Item_" + i, rect, new Vector2(170, 44), new Vector2(28, 40 - i * 52), 27);
            }
            data.ApplyModifiedPropertiesWithoutUndo(); return ticket;
        }
        public static void Build()
        {
            Directory.CreateDirectory(Folder); Directory.CreateDirectory("Assets/Data/Icons");
            string[] names = { "Donut", "Cola", "Shake", "Cake", "Water" };
            Color[] colours = { new Color(.8f, .25f, .8f), new Color(.2f, .4f, 1), new Color(.65f, .35f, .15f), new Color(1, .55f, .25f), new Color(.15f, .8f, 1) };
            _items = new ItemDef[5];
            for (int i = 0; i < 5; i++)
            {
                _items[i] = AssetDatabase.LoadAssetAtPath<ItemDef>("Assets/Data/Items/Item_" + names[i] + ".asset");
                _items[i].Icon = SpriteAsset(names[i], colours[i]); EditorUtility.SetDirty(_items[i]);
            }
            _ring = SpriteAsset("PatienceRing", Color.white, true); _square = SpriteAsset("Bar", Color.white, false, true);
            var root = CanvasRoot("UI_CustomerBubble", new Vector2(300, 300));
            var bubble = root.AddComponent<CustomerBubble>(); var ticket = Ticket(root.transform, true);
            Ref(bubble, "_ticket", ticket); ticket.gameObject.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(root, Folder + "UI_CustomerBubble.prefab"); Object.DestroyImmediate(root);
            root = CanvasRoot("UI_OrderRail", new Vector2(1150, 360));
            var panel = root.AddComponent<Image>(); panel.color = new Color(.04f, .06f, .1f, .95f); panel.raycastTarget = false;
            var status = root.AddComponent<GameplayStatus>(); Ref(status, "_text", Label("Status", root.transform, new Vector2(1100, 70), new Vector2(0, 140), 30));
            Ref(status, "_config", AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/Data/GameConfig.asset"));
            var rail = root.AddComponent<OrderRail>(); var railData = new SerializedObject(rail); var tickets = railData.FindProperty("_tickets"); tickets.arraySize = 4;
            for (int i = 0; i < 4; i++)
            {
                ticket = Ticket(root.transform, false); ticket.GetComponent<RectTransform>().anchoredPosition = new Vector2(-420 + 280 * i, -35);
                tickets.GetArrayElementAtIndex(i).objectReferenceValue = ticket; ticket.gameObject.SetActive(false);
                if (i == 0) PrefabUtility.SaveAsPrefabAsset(ticket.gameObject, Folder + "UI_OrderTicket.prefab");
            }
            railData.ApplyModifiedPropertiesWithoutUndo(); PrefabUtility.SaveAsPrefabAsset(root, Folder + "UI_OrderRail.prefab"); Object.DestroyImmediate(root);
        }
    }
}
