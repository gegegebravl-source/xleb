using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.TextCore;

namespace WarmBread
{
    public sealed class WarmBreadUITheme : MonoBehaviour
    {
        private static readonly Color Bread = new Color(0.95f, 0.67f, 0.34f, 1f);
        private static readonly Color Cream = new Color(0.96f, 0.90f, 0.78f, 1f);
        private static readonly Color Forest = new Color(0.075f, 0.17f, 0.13f, 0.98f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            // Procedural UI theme injection is disabled: style the UI manually in the editor.
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            Apply(SceneManager.GetActiveScene());
        }

        private void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) { Apply(scene); }

        private static void Apply(Scene scene)
        {
            var roots = scene.GetRootGameObjects();
            for (var r = 0; r < roots.Length; r++)
            {
                var texts = roots[r].GetComponentsInChildren<TMP_Text>(true);
                for (var i = 0; i < texts.Length; i++) StyleText(texts[i]);
                var buttons = roots[r].GetComponentsInChildren<Button>(true);
                for (var i = 0; i < buttons.Length; i++) StyleButton(buttons[i]);
                var sliders = roots[r].GetComponentsInChildren<Slider>(true);
                for (var i = 0; i < sliders.Length; i++) StyleSlider(sliders[i]);
                var toggles = roots[r].GetComponentsInChildren<Toggle>(true);
                for (var i = 0; i < toggles.Length; i++) StyleToggle(toggles[i]);
            }
        }

        private static void StyleText(TMP_Text text)
        {
            if (text == null) return;
            var interactiveLabel = text.GetComponentInParent<Selectable>() != null;
            if (!interactiveLabel && text.fontSize < 24f) return;
            text.fontFeatures.Add(OTL_FeatureTag.kern);
            text.extraPadding = true;
            text.color = new Color(Cream.r, Cream.g, Cream.b, text.color.a);
            text.outlineColor = new Color32(0, 0, 0, 150);
            text.outlineWidth = 0.08f;
        }

        private static void StyleButton(Button button)
        {
            var colors = button.colors;
            colors.normalColor = Forest;
            colors.highlightedColor = new Color(0.12f, 0.28f, 0.20f, 1f);
            colors.pressedColor = new Color(0.94f, 0.49f, 0.20f, 1f);
            colors.selectedColor = new Color(0.12f, 0.28f, 0.20f, 1f);
            colors.disabledColor = new Color(0.15f, 0.16f, 0.15f, 0.55f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.12f;
            button.colors = colors;
            if (button.GetComponent<WarmBreadButtonMotion>() == null) button.gameObject.AddComponent<WarmBreadButtonMotion>();
            var outline = button.GetComponent<Outline>() ?? button.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(Bread.r, Bread.g, Bread.b, 0.38f);
            outline.effectDistance = new Vector2(1f, -1f);
        }

        private static void StyleSlider(Slider slider)
        {
            if (slider.fillRect != null && slider.fillRect.TryGetComponent<Image>(out var fill)) fill.color = Bread;
            if (slider.handleRect != null && slider.handleRect.TryGetComponent<Image>(out var handle)) handle.color = Cream;
        }

        private static void StyleToggle(Toggle toggle)
        {
            if (toggle.graphic is Image checkmark) checkmark.color = Bread;
            var colors = toggle.colors;
            colors.highlightedColor = Cream;
            colors.pressedColor = Bread;
            toggle.colors = colors;
        }
    }

}
