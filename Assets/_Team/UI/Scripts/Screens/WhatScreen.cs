using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FindMe.UI
{
    /// <summary>
    /// Prototype "what": pick one of your things, a one-time thing, or type/say something else.
    /// Continue stays disabled until something is chosen; it then starts the search (Phase U3: AI analyzing).
    /// </summary>
    public class WhatScreen : UIScreen
    {
        readonly List<Action<bool>> pickSetters = new();
        readonly List<Chip> oneTimeChips = new();
        TMP_InputField custom;
        ChunkyButton next;

        protected override void Build()
        {
            var page = Page();
            Widgets.TopBar(page.transform, "New search", App.Router.Back);
            var body = Body(page.transform).transform;
            Widgets.TitleLead(body, "What are you looking for?", null, 0f);

            Widgets.SectionLabel(body, "Your things", 0f);   // CSS margins collapse: the title's 10 px wins over 6
            var grid = UIKit.Row(body, "Grid4", 10f, null, TextAnchor.UpperLeft);
            grid.childForceExpandWidth = true;
            foreach (var t in App.State.mine)
            {
                var thing = t;
                Action<bool> set = null;
                set = Widgets2.Pick(grid.transform, thing, () => Choose(thing, set, null));
                pickSetters.Add(set);
            }
            foreach (Transform cell in grid.transform) UIKit.Size(cell.GetComponent<RectTransform>(), 0f, -1, 1);   // 4 equal columns

            // "Something else" + One-time tag
            UIKit.Space(body, 20f);
            var lab = UIKit.Row(body, "LabelRow", 4f);
            UIKit.Text(lab.transform, "Something else", TextStyle.Label);
            Widgets.Tag(lab.transform, "One-time", T.lemonLight, new Color32(0x8A, 0x6A, 0x00, 255));
            UIKit.Space(body, 10f);
            var chips = Widgets.Chips(body);
            foreach (var t in App.State.oneTime)
            {
                var thing = t;
                oneTimeChips.Add(Widgets.Chip(chips.transform, t.name, false, c => Choose(thing, null, c)));
            }

            // Photo box + "Type or say it" field with a mint mic button (.alt: 100 px + 1fr).
            UIKit.Space(body, 10f);
            var alt = UIKit.Row(body, "Alt", 10f, null, TextAnchor.UpperLeft);
            alt.childForceExpandHeight = true;
            var photo = UIKit.Column(alt.transform, "Photo", 4f, null, TextAnchor.MiddleCenter);
            UIKit.Size(photo, 100f, 100f);
            UIKit.Frame(photo.transform, T.white, T.cloud, 22f);
            UIKit.Icon(photo.transform, "camera", 26f, new Color(1f, 1f, 1f, 0.75f));
            UIKit.Text(photo.transform, "Photo", TextStyle.Small, null, TextAlignmentOptions.Center).font = T.bodyBlack;

            var say = UIKit.Rect(alt.transform, "Say");
            UIKit.Size(say, 0f, 60f, 1);
            custom = Widgets.Input(say, "Type or say it", "", false, 60f);
            UIKit.Stretch((RectTransform)custom.transform);
            custom.textViewport.offsetMax = new Vector2(-60f, 0f);       // leave room for the mic button
            custom.onValueChanged.AddListener(OnTyped);
            var mic = UIKit.Circle(say, "Mic", T.mint, 44f);
            var mrt = (RectTransform)mic.transform;
            mrt.anchorMin = mrt.anchorMax = new Vector2(1f, 0.5f);
            mrt.anchoredPosition = new Vector2(-30f, 0f);
            var mi = UIKit.Icon(mic.transform, "mic", 21f);
            UIKit.Place((RectTransform)mi.transform, new Vector2(0.5f, 0.5f), new Vector2(21f, 21f));
            Widgets2.Tap(mic.gameObject, mic, () => App.Toast("Voice input comes with the real build"));
            Widgets2.Tap(photo.gameObject, photo.GetComponentInChildren<Image>(), () => App.Toast("Photo search: point the camera at the item on the next step"));

            var foot = Foot(page.transform);
            next = UIKit.Button(foot.transform, "Continue", ButtonKind.Dark, App.BeginSearch);
        }

        public override void OnShow()
        {
            Choose(null, null, null);
            custom.SetTextWithoutNotify("");
        }

        /// <summary>One choice at a time across picks, chips and the text field.</summary>
        void Choose(Thing t, Action<bool> pick, Chip chip)
        {
            foreach (var s in pickSetters) s(s == pick);
            foreach (var c in oneTimeChips) c.Set(c == chip);
            if (t != null) custom.SetTextWithoutNotify("");
            App.State.selected = t;
            next.SetInteractable(t != null);
        }

        void OnTyped(string v)
        {
            v = v.Trim();
            foreach (var s in pickSetters) s(false);
            foreach (var c in oneTimeChips) c.Set(false);
            App.State.selected = v.Length == 0 ? null
                : new Thing { name = char.ToUpper(v[0]) + v.Substring(1), icon = "search", color = "lav", oneTime = true };
            next.SetInteractable(v.Length > 0);
        }
    }
}
