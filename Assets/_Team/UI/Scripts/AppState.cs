using System.Collections.Generic;

namespace FindMe.UI
{
    /// <summary>A registered thing (prototype MINE): name, icon, tile color, where it lives and was last seen.</summary>
    public class Thing
    {
        public string id, name, icon, color, spot, last, lastTime, voice = "";
        public int finds;
        public string avg = "—";
        public bool oneTime;
        /// <summary>"Where it turns up": place → percent.</summary>
        public List<(string place, int pct)> top = new();
    }

    /// <summary>One line of History.</summary>
    public class HistoryEntry
    {
        public string name, icon, color, when, result;
        public bool oneTime, ok, lost;
    }

    /// <summary>
    /// The app's data for the prototype screens (sample data from the HTML prototype). Not saved anywhere yet:
    /// the search itself comes from SearchController; this is everything around it (account, things, history).
    /// </summary>
    public class AppState
    {
        public string userName = "Jisoo";
        public bool carePlan;                       // premium "Care plan" subscription
        public Thing selected;                      // the thing being searched for / viewed
        public string historyTab = "searches";

        public readonly List<Thing> mine = new()
        {
            new Thing { id = "keys", name = "Keys", icon = "key", color = "pink", spot = "Entryway tray", last = "Entryway tray",
                lastTime = "Today, 8:12", finds = 7, avg = "4 min", voice = "\"I drop them in the tray by the door.\"",
                top = { ("Coat pockets", 62), ("Sofa cushions", 24), ("Coffee table", 14) } },
            new Thing { id = "glasses", name = "Glasses", icon = "glasses", color = "sky", spot = "Desk", last = "Desk",
                lastTime = "Today, 07:50", finds = 5, avg = "5 min",
                top = { ("Desk", 45), ("Bathroom sink", 35), ("Jacket pocket", 20) } },
            new Thing { id = "wallet", name = "Wallet", icon = "wallet", color = "sun", spot = "Bag", last = "Car, passenger seat",
                lastTime = "Yesterday", finds = 4, avg = "6 min",
                top = { ("Bag", 51), ("Jacket", 30), ("Car seat", 19) } },
            new Thing { id = "pill", name = "Pill box", icon = "pill", color = "mint", spot = "Kitchen shelf", last = "Kitchen shelf",
                lastTime = "Today, 09:00", finds = 1, avg = "3 min",
                top = { ("Kitchen shelf", 55), ("Bag", 30), ("Bedside", 15) } },
        };

        /// <summary>"Something else" one-time picks on the search screen.</summary>
        public readonly List<Thing> oneTime = new()
        {
            new Thing { name = "Remote", icon = "phone", color = "lav", oneTime = true },
            new Thing { name = "Earbuds", icon = "buds", color = "pink", oneTime = true },
            new Thing { name = "Umbrella", icon = "sun", color = "sky", oneTime = true },
            new Thing { name = "Transit card", icon = "id", color = "mint", oneTime = true },
        };

        public readonly List<HistoryEntry> history = new()
        {
            new HistoryEntry { name = "Keys", icon = "key", color = "pink", when = "Today, 8:20", result = "Found · Coat pocket", ok = true },
            new HistoryEntry { name = "Umbrella", icon = "sun", color = "sky", oneTime = true, when = "Yesterday, 18:10", result = "Found · Car trunk", ok = true },
            new HistoryEntry { name = "Keys", icon = "key", color = "pink", when = "Mon, 8:05", result = "Found · Sofa cushions", ok = true },
            new HistoryEntry { name = "Wallet", icon = "wallet", color = "sun", when = "Sun, 22:30", result = "Truly lost", lost = true },
            new HistoryEntry { name = "Keys", icon = "key", color = "pink", when = "Fri, 7:48", result = "Found · Entryway tray", ok = true },
        };
    }
}
