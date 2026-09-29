using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace AmongUsPCMod
{
    // ============================================================================
    // ⚙️ THE FAC OBJECT HELPER: CORE LIGHTWEIGHT UI BUILDER (v1.6.0 - LOCKED)
    // ============================================================================
    public static class ObjectHelper
    {
        public static IEnumerable<T> Flatten<T>(this IEnumerable<IEnumerable<T>> collection)
        {
            return collection.SelectMany(x => x);
        }

        private static GameObject CreateObject(string objName, Transform parent, Vector3 localPosition, int? layer = null)
        {
            var obj = new GameObject(objName);
            obj.transform.SetParent(parent);
            obj.transform.localPosition = localPosition;
            obj.transform.localScale = new Vector3(1f, 1f, 1f);
            
            if (layer.HasValue) obj.layer = layer.Value;
            else if (parent != null) obj.layer = parent.gameObject.layer;
            
            return obj;
        }

        private static T CreateObject<T>(string objName, Transform parent, Vector3 localPosition, int? layer = null) where T : Component
        {
            return CreateObject(objName, parent, localPosition, layer).AddComponent<T>();
        }

        public static SpriteRenderer CreateSpriteRenderer(string name, string spriteName, float pixelsPerUnit, Vector3 position, Transform parent = null)
        {
            var renderer = CreateObject<SpriteRenderer>(name, null, position);
            if (parent != null)
            {
                renderer.gameObject.transform.SetParent(parent);
            }
            renderer.gameObject.transform.localPosition = position;
            
            renderer.sprite = null; 
            renderer.color = Color.clear;

            return renderer;
        }

        public static TextMeshPro InstantiateTextComponent(TextMeshPro template, Vector3 position, Transform parent = null)
        {
            if (template == null) return null;
            
            var text = Object.Instantiate(template, parent);
            text.transform.localPosition = position;
            text.fontStyle = FontStyles.Bold;
            text.text = string.Empty;
            return text;
        }

        public static GameObject CreateButton(
            string buttonName,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Vector2 colliderSize,
            Sprite defaultSprite,
            Color defaultColor,
            Color hoverColor,
            System.Action clickAction,
            bool aspectActive = false,
            Sprite hoverSprite = null)
        {
            var button = new GameObject(buttonName);
            button.transform.SetParent(parent);
            button.transform.localPosition = localPosition;
            button.transform.localScale = localScale;

            button.AddComponent<BoxCollider2D>().size = colliderSize;

            var spriteRenderer = button.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = defaultSprite;
            spriteRenderer.color = defaultColor;

            var passiveButton = button.AddComponent<PassiveButton>();
            passiveButton.OnClick = new Button.ButtonClickedEvent();
            
            if (clickAction != null)
            {
                passiveButton.OnClick.AddListener(clickAction);
            }

            passiveButton.OnMouseOut = new UnityEvent();
            passiveButton.OnMouseOut.AddListener((System.Action)(() =>
            {
                spriteRenderer.sprite = defaultSprite;
                spriteRenderer.color = defaultColor;
            }));

            passiveButton.OnMouseOver = new UnityEvent();
            passiveButton.OnMouseOver.AddListener((System.Action)(() =>
            {
                spriteRenderer.sprite = hoverSprite != null ? hoverSprite : defaultSprite;
                spriteRenderer.color = hoverColor;
            }));

            return button;
        }
    }
}
