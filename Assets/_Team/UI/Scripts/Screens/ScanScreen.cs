using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FindMe.UI
{
    /// <summary>
    /// Prototype "scan": the search HUD over the live camera. Top: exit · "Quick search: keys" · timer, and the
    /// AI's status in a speech bubble. The middle stays clear for the AR cues. The bottom sheet has 3 modes:
    ///   normal — next-step hint, "2 of 4 spots checked", Scan / Check here / Found
    ///   spot   — the cue being looked at: its name, Gemini's tip, a dwell bar (+ ✓ Checked / Found it when asked)
    ///   match  — "Is this your keys?" Not it / Yes, that's it!
    /// </summary>
    public class ScanScreen : UIScreen
    {
        TextMeshProUGUI pill, timer, bubble;
        Image tileBg, tileIcon;
        TextMeshProUGUI hint, title, progLeft, progRight;
        RectTransform progFill;
        GameObject normalButtons, manualButtons, infoGroup, matchGroup;
        TextMeshProUGUI matchTitle, matchText;
        ChunkyButton scanBtn, checkBtn;
        RectTransform toast;
        TextMeshProUGUI toastText;
        CanvasGroup toastGroup;
        bool matching;
        string shownTime;

        protected override void Build()
        {
            Background.color = Color.clear;
            var page = Page();

            var top = CamWidgets.Top(page.transform, App.ExitSearch);
            pill = CamWidgets.Pill(top.transform, new Thing { icon = "key", color = "pink" }, "", out _);
            UIKit.Space(top.transform).flexibleWidth = 1;
            timer = CamWidgets.Timer(top.transform);
            bubble = CamWidgets.Bubble(page.transform);
            UIKit.Space(page.transform, 0f, 1f);                       // camera area for the AR cues

            var sheet = CamWidgets.Sheet(page.transform).transform;
            BuildInfo(sheet);
            BuildMatch(sheet);
            BuildToast();
        }

        void BuildInfo(Transform sheet)
        {
            var group = UIKit.Column(sheet, "Info", 0f);
            group.childForceExpandWidth = true;
            infoGroup = group.gameObject;

            // Row: icon tile · small hint + bold title (.row with .badge-like tile).
            var row = UIKit.Row(group.transform, "Next", 12f, UIKit.Pad(0, 0, 0, 12));
            tileBg = UIKit.Box(row.transform, "Tile", T.lemon, 46f * 0.3f);
            UIKit.Size(tileBg, 46f, 46f);
            tileIcon = UIKit.Icon(tileBg.transform, "compass", 27f);
            UIKit.Place((RectTransform)tileIcon.transform, new Vector2(0.5f, 0.5f), new Vector2(27f, 27f));
            var col = UIKit.Column(row.transform, "Text", 0f);
            UIKit.Size(col, 0f, -1, 1);
            hint = UIKit.Text(col.transform, "", TextStyle.Small);
            title = UIKit.Text(col.transform, "", TextStyle.Body);
            title.font = T.bodyBlack;
            title.fontSize = 18f;

            (progLeft, progRight, progFill) = CamWidgets.Progress(group.transform, T.mint);
            UIKit.Space(group.transform, 14f);

            // Normal buttons: Scan this area · Check here · Found
            var nb = UIKit.Row(group.transform, "Buttons", 8f);
            nb.childForceExpandWidth = true;
            normalButtons = nb.gameObject;
            scanBtn = Small(UIKit.Button(nb.transform, "Scan", ButtonKind.Primary, App.Actions.ScanArea, "scan"));
            checkBtn = Small(UIKit.Button(nb.transform, "Check here", ButtonKind.Outline, App.Actions.CheckHere));
            Small(UIKit.Button(nb.transform, "Found", ButtonKind.Success, FoundTapped));

            // Manual buttons for a spot the AI couldn't decide on: ✓ Checked · Found it
            var mb = UIKit.Row(group.transform, "Manual", 10f);
            mb.childForceExpandWidth = true;
            manualButtons = mb.gameObject;
            Small(UIKit.Button(mb.transform, "Checked", ButtonKind.Calm, App.Actions.MarkChecked, "check"));
            Small(UIKit.Button(mb.transform, "Found it", ButtonKind.Success, FoundTapped));
        }

        // Equal-width buttons with a slightly smaller label so three fit in one row.
        static ChunkyButton Small(ChunkyButton b)
        {
            b.Label.fontSize = 16f;
            UIKit.Size(b, 0f, -1, 1);
            return b;
        }

        void BuildMatch(Transform sheet)
        {
            var group = UIKit.Column(sheet, "Match", 0f);
            group.childForceExpandWidth = true;
            matchGroup = group.gameObject;
            (matchTitle, matchText) = CamWidgets.SheetText(group.transform, "", "");
            var row = UIKit.Row(group.transform, "Buttons", 10f);
            row.childForceExpandWidth = true;
            Small(UIKit.Button(row.transform, "Not it", ButtonKind.Outline, () => Answer(false)));
            Small(UIKit.Button(row.transform, "Yes, that's it!", ButtonKind.Success, () => Answer(true)));
            matchGroup.SetActive(false);
        }

        // Soda toast under the top row: check tile · "Desk drawer checked" · Undo. Slides down, stays 4 s.
        void BuildToast()
        {
            var row = UIKit.Row(Safe, "CheckedToast", 12f, UIKit.Pad(10, 10, 14, 10));
            toast = (RectTransform)row.transform;
            toast.anchorMin = new Vector2(0f, 1f);
            toast.anchorMax = new Vector2(1f, 1f);
            toast.pivot = new Vector2(0.5f, 1f);
            toast.offsetMin = new Vector2(16f, -80f);
            toast.offsetMax = new Vector2(-16f, -72f);
            row.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            UIKit.Backdrop(row.transform, T.soda, 20f);
            var tile = UIKit.Box(row.transform, "Tile", T.white, 12f);
            UIKit.Size(tile, 40f, 40f);
            var ic = UIKit.Icon(tile.transform, "check", 22f);
            UIKit.Place((RectTransform)ic.transform, new Vector2(0.5f, 0.5f), new Vector2(22f, 22f));
            toastText = Widgets.Grow(UIKit.Text(row.transform, "", TextStyle.Label));
            toastText.fontSize = 15f;
            var undo = UIKit.Link(row.transform, "Undo", () => { App.Actions.UndoChecked(); HideToast(); }, T.grape);
            undo.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Underline;
            toastGroup = row.gameObject.AddComponent<CanvasGroup>();
            row.gameObject.SetActive(false);
        }

        public override void OnShow()
        {
            var t = App.Search.thing ?? App.State.mine[0];
            pill.text = "Quick search: " + t.name.ToLower();
            var tile = pill.transform.parent.Find("Tile");
            if (tile != null)
            {
                tile.GetComponent<Image>().color = UIKit.Swatch(t.color);
                tile.GetComponentsInChildren<Image>()[1].sprite = T.Icon(t.icon);
            }
            matching = false;
            matchGroup.SetActive(false);
            infoGroup.SetActive(true);
            toast.gameObject.SetActive(false);
            shownTime = null;
            Refresh();
        }

        void Update()
        {
            // The timer text only changes once a second; skip the rebuild otherwise.
            string s = App.Search.ElapsedText;
            if (s != shownTime) timer.text = CamWidgets.TimerText(shownTime = s);
        }

        /// <summary>Re-reads the SearchView (called on every ISearchUI update).</summary>
        public void Refresh()
        {
            if (title == null) return;
            var v = App.Search;
            CamWidgets.SetBubble(bubble, matching ? null : v.status);   // the match sheet asks the question itself
            if (matching) return;

            if (v.panelOn)
            {
                // The cue being looked at. "Open it up"-style tips get the tangerine hand; checking gets the eye.
                bool nudge = v.panelManual || (v.panelTip != null && !v.panelTip.EndsWith("…"));
                tileBg.color = nudge ? T.tangerine : T.lavender;
                tileIcon.sprite = T.Icon(nudge ? "hand" : "eye");
                hint.text = v.panelTip == "Looking…" ? "Looking…" : v.panelManual ? "Did you look inside?" : "Hold your phone still";
                title.text = UIManager.Cap(v.panelLabel);
                progLeft.text = v.panelTip;
                progRight.text = v.panelTip == "Looking…" ? "Checking" : Mathf.RoundToInt(v.panelDwell * 100f) + "%";
                SetFill(v.panelDwell, T.bubblegum);
            }
            else
            {
                bool searching = v.total > 0;
                tileBg.color = T.lemon;
                tileIcon.sprite = T.Icon("compass");
                hint.text = searching ? "Next spot · hold your phone on a marker" : "Point at part of the room";
                title.text = searching ? (v.checkedCount >= v.total ? "All spots checked. Scan another area?" : "Check the marked spots")
                                       : "Then tap Scan";
                progLeft.text = searching ? $"{v.checkedCount} of {v.total} spots checked" : "No spots yet";
                progRight.text = "This room";
                SetFill(v.total > 0 ? (float)v.checkedCount / v.total : 0f, T.mint);
            }
            normalButtons.SetActive(!(v.panelOn && v.panelManual));
            manualButtons.SetActive(v.panelOn && v.panelManual);
            scanBtn.SetInteractable(v.scanEnabled);
            checkBtn.SetInteractable(v.total > 0);
        }

        void SetFill(float value, Color c)
        {
            CamWidgets.SetBar(progFill, value);
            progFill.GetComponent<Image>().color = c;
        }

        // ---------- found / match ----------

        /// <summary>Found on the targeted spot if there is one; otherwise ask where it was.</summary>
        void FoundTapped()
        {
            if (!App.Actions.MarkFound()) App.Router.Go("foundWhere");
        }

        public void ShowMatch(string label)
        {
            matching = true;
            var name = (App.Search.profile?.name ?? App.Search.thing?.name ?? "item").ToLower();
            matchTitle.text = $"Is this your {name}?";
            matchText.text = $"I think I see it at the {label}. Take a look before you answer.";
            infoGroup.SetActive(false);
            matchGroup.SetActive(true);
            CamWidgets.SetBubble(bubble, null);
            if (Application.isPlaying) StartCoroutine(Tweens.SlideIn((RectTransform)matchGroup.transform.parent, new Vector2(0f, -40f), 0.35f, Tweens.OutBack));
        }

        void Answer(bool yes)
        {
            matching = false;
            matchGroup.SetActive(false);
            infoGroup.SetActive(true);
            App.Actions.ConfirmMatch(yes);
            Refresh();
        }

        // ---------- toast ----------

        public void ShowCheckedToast(string label)
        {
            if (!isActiveAndEnabled) return;
            toastText.text = $"{UIManager.Cap(label)} checked";
            StopCoroutine(nameof(ToastLife));
            StartCoroutine(nameof(ToastLife));
        }

        IEnumerator ToastLife()
        {
            toast.gameObject.SetActive(true);
            toastGroup.alpha = 0f;
            StartCoroutine(Tweens.SlideIn(toast, new Vector2(0f, 30f), 0.2f));      // slides down from above
            yield return Tweens.Fade(toastGroup, 1f, 0.2f);
            yield return new WaitForSecondsRealtime(4f);                             // the Undo window
            yield return Tweens.Fade(toastGroup, 0f, 0.3f);
            toast.gameObject.SetActive(false);
        }

        void HideToast()
        {
            StopCoroutine(nameof(ToastLife));
            toast.gameObject.SetActive(false);
        }
    }
}
