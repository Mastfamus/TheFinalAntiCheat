using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace AmongUsPCMod;

public static class MyMusicPanel
{
    private const int ItemsPerPage = 7;
    private static int CurrentPage { get; set; } = 1;
    private static List<GameObject> Items = new List<GameObject>();
    public static SpriteRenderer CustomBackground { get; set; }
    private static OptionsMenuBehaviour OptionsMenuBehaviourNow { get; set; }

    public static void RefreshTagList()
    {
        try
        {
            Items.ForEach(Object.Destroy);
            Items.Clear();
            var startIndex = (CurrentPage - 1) * ItemsPerPage;

            var count = 0;
            foreach (var audio in FinalMusic.Musics.Skip(startIndex))
            {
                if (count >= ItemsPerPage) break;
                
                var mouseMoveToggle = OptionsMenuBehaviourNow.DisableMouseMovement;
                var toggleButton = Object.Instantiate(mouseMoveToggle, CustomBackground.transform);
                toggleButton.Text.text = audio.Name;
                toggleButton.Background.color = audio.CurrentAudioStates == AudiosStates.Playing ? Color.green : Color.white;
                
                var passiveButton = toggleButton.GetComponent<PassiveButton>();
                passiveButton.OnClick = new Button.ButtonClickedEvent();
                passiveButton.OnClick.AddListener(new Action(() => { AudioPlayer.Play(audio); }));
                
                Items.Add(toggleButton.gameObject);
                count++;
            }
        }
        catch { }
    }
}