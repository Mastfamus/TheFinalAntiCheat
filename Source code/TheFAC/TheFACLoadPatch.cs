using System;
using System.IO;
using System.Collections;
using System.Linq;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BepInEx.Unity.IL2CPP.Utils;

namespace AmongUsPCMod;

[HarmonyPatch(typeof(SplashManager))]
public static class TheFACLoadPatch
{
    private static TextMeshPro _introTextComponent;
    private static Transform _cameraTransform;
    private static Vector3 _originalCameraPos;

    [HarmonyPatch(nameof(SplashManager.Start)), HarmonyPrefix]
    public static bool Start(SplashManager __instance)
    {
        __instance.startTime = Time.time;
        __instance.StartCoroutine(SovereignIntroSequence(__instance));
        return false; // Заменяме стария лоадер с нашия суверен механизъм
    }

    private static IEnumerator SovereignIntroSequence(SplashManager instance)
    {
        System.Console.WriteLine("[FAC INTRO]: Sovereign security matrix loading...");

        // 1. Изчистваме екрана
        var logoAnimator = GameObject.Find("LogoAnimator");
        if (logoAnimator != null) logoAnimator.SetActive(false);

        if (Camera.main != null)
        {
            _cameraTransform = Camera.main.transform;
            _originalCameraPos = _cameraTransform.localPosition;
        }

        if (instance.errorPopup != null && instance.errorPopup.InfoText != null)
        {
            _introTextComponent = UnityEngine.Object.Instantiate(instance.errorPopup.InfoText, instance.transform);
            _introTextComponent.transform.localPosition = new Vector3(0f, 0f, -10f);
            _introTextComponent.alignment = TextAlignmentOptions.Center;
            _introTextComponent.fontSize = 2.5f;
        }

        // Стартираме синхронизацията на вашите тапети, курсор и черния списък
        TheFACCloudEngine.SyncSovereignAssets();

        // ============================================================================
        // ЕТАП 1: Текст "Welcome!..."
        // ============================================================================
        if (_introTextComponent != null)
        {
            _introTextComponent.text = "Welcome!...";
            _introTextComponent.color = Color.white;
            yield return FadeTextInAndOut(_introTextComponent, 2.0f);
        }

        // ============================================================================
        // ЕТАП 2: Основното послание против хакери
        // ============================================================================
        if (_introTextComponent != null)
        {
            _introTextComponent.text = "To <color=#A020F0><b>The Final Anti-Cheat</b></color> mod.\n" +
                                       "A mod designed to stop <color=red>hackers&cheaters</color>\n" +
                                       "and keep lobbies fair!\n"+
                                       "made by <i><color=#6b2fbb>Mastfamus</i></color>";
            
            yield return SetTextAlpha(_introTextComponent, 0f, 1f, 1.5f);
            yield return new WaitForSeconds(6.0f); // Време за четене
            yield return SetTextAlpha(_introTextComponent, 1f, 0f, 1.0f);
        }

        // ============================================================================
        // ЕТАП 3: Извикване на OverruleAnimation & Епичен Screen Shake
        // ============================================================================
        System.Console.WriteLine("[FAC INTRO]: Activating native OverruleAnimation.");
        
        GameObject nativeGavelAnim = GameObject.Find("OverruleAnimation");
        if (nativeGavelAnim != null)
        {
            nativeGavelAnim.SetActive(true);
            var animator = nativeGavelAnim.GetComponent<Animator>();
            if (animator != null)
            {
                animator.Play("Play", 0, 0f); // Екранът се разбива с фабричния си звук!
            }
        }

        // Мълниеносна вибрация на камерата при удара
        if (_cameraTransform != null)
        {
            float shakeDuration = 0.3f;
            float shakeMagnitude = 0.15f;
            float elapsed = 0f;

            while (elapsed < shakeDuration)
            {
                float x = UnityEngine.Random.Range(-1f, 1f) * shakeMagnitude;
                float y = UnityEngine.Random.Range(-1f, 1f) * shakeMagnitude;
                _cameraTransform.localPosition = new Vector3(_originalCameraPos.x + x, _originalCameraPos.y + y, _originalCameraPos.z);
                elapsed += Time.deltaTime;
                yield return null;
            }
            _cameraTransform.localPosition = _originalCameraPos;
        }

        yield return new WaitForSeconds(2.0f);

        if (_introTextComponent != null) UnityEngine.Object.Destroy(_introTextComponent.gameObject);

        // ============================================================================
        // ЕТАП 4: Преход към Менюто + ХАРДУЕРНО МОДИФИЦИРАНА МУЗИКА (Deeper Pitch)
        // ============================================================================
        instance.sceneChanger.BeginLoadingScene();
        instance.doneLoadingRefdata = true;

        _ = new MainThreadTask(() =>
        {
            System.Console.WriteLine("[FAC MUSIC]: Modifying native Among Us menu theme to deep-frequency matrix.");
            
            // Взимаме фабричния аудио източник на играта
            var vanillaBackground = SoundManager.Instance.BackgroundMusic;
            if (vanillaBackground != null)
            {
                // Намаляваме тона (Pitch) от 1.0f на 0.78f. Това прави музиката по-бавна, 
                // с много по-дълбок, тежък космически бас и мистериозно звучене!
                vanillaBackground.pitch = 0.78f; 
                vanillaBackground.volume = 0.85f;
                vanillaBackground.Play();
            }
        }, "FAC Menu Music Ignition");
    }

    private static IEnumerator SetTextAlpha(TextMeshPro text, float start, float end, float duration)
    {
        float elapsed = 0f;
        Color originalColor = text.color;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(start, end, elapsed / duration);
            text.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            yield return null;
        }
        text.color = new Color(originalColor.r, originalColor.g, originalColor.b, end);
    }

    private static IEnumerator FadeTextInAndOut(TextMeshPro text, float duration)
    {
        yield return SetTextAlpha(text, 0f, 1f, duration / 2);
        yield return new WaitForSeconds(0.5f);
        yield return SetTextAlpha(text, 1f, 0f, duration / 2);
    }
}