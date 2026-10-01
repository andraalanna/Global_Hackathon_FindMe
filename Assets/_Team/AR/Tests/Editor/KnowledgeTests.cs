using NUnit.Framework;

namespace FindMe.Tests
{
    /// <summary>On-device safety net for Gemini's answers (knowledge base + "checked needs proof").</summary>
    public class KnowledgeTests
    {
        static LostItemKnowledge KB => LostItemKnowledge.Load();

        [TestCase("chapstick", "glasses")]       // the user's bug: a small object is not a hiding place
        [TestCase("snack bag", "glasses")]       // the user's bug: food packaging
        [TestCase("bag of chips", "keys")]
        [TestCase("trash bin", "wallet")]
        [TestCase("glasses", "keys")]            // glasses can't hide keys
        [TestCase("phone", "wallet")]
        [TestCase("television", "remote")]
        [TestCase("radio", "keys")]              // found by the eval: gadgets on a desk are not hiding places
        [TestCase("tape dispenser", "keys")]
        public void ImpossiblePlaces_AreDropped(string label, string item)
        {
            var info = KB.Match(item);
            Assert.IsTrue(KB.IsImpossiblePlace(label, info, info.MaxSizeCm), $"\"{label}\" should not be a place for {item}");
        }

        [TestCase("glasses case", "glasses")]    // its own case IS a place (must be opened)
        [TestCase("backpack", "wallet")]
        [TestCase("sofa cushions", "phone")]
        [TestCase("pile of clothes", "keys")]
        [TestCase("wall socket near bed", "charger")]   // "socket" is never a place... except for a charger
        [TestCase("laptop", "ring")]             // a much bigger object can hide something small
        [TestCase("jacket pocket", "car keys")]
        public void RealPlaces_AreKept(string label, string item)
        {
            var info = KB.Match(item);
            Assert.IsFalse(KB.IsImpossiblePlace(label, info, info.MaxSizeCm), $"\"{label}\" should be a place for {item}");
        }

        [TestCase("my pink wallet", "wallet")]
        [TestCase("car keys", "car_key")]       // longest name wins over "keys"
        [TestCase("the TV remote", "remote")]
        [TestCase("AirPods", "earbuds")]
        [TestCase("reading glasses", "glasses")]
        public void UserText_MatchesKnownItem(string text, string id) => Assert.AreEqual(id, KB.Match(text)?.id);

        [Test]
        public void TheItemItself_IsNotAPlace_ButItsCaseIs()
        {
            Assert.IsTrue(LostItemKnowledge.IsTheItem("black shoe", "shoe"));
            Assert.IsFalse(LostItemKnowledge.IsTheItem("glasses case", "glasses"));
        }

        [Test]
        public void ClosedCase_IsNeverChecked_EvenIfModelSaysInsideVisible()
        {
            var r = GeminiClient.ParseCheck("{\"closed\":true,\"insideVisible\":true,\"itemVisible\":false,\"mightBeHidden\":false,\"tip\":\"\"}");
            Assert.IsFalse(r.spotFullyVisible);
        }

        [Test]
        public void ClutteredOrDark_AsksToLookCloser()
        {
            var r = GeminiClient.ParseCheck("{\"closed\":false,\"insideVisible\":true,\"itemVisible\":false,\"mightBeHidden\":true,\"tip\":\"Move the clothes\"}");
            Assert.IsFalse(r.spotFullyVisible);
        }

        [Test]
        public void OpenClearAndEmpty_IsChecked()
        {
            var r = GeminiClient.ParseCheck("{\"closed\":false,\"insideVisible\":true,\"itemVisible\":false,\"mightBeHidden\":false,\"tip\":\"Nothing here\"}");
            Assert.IsTrue(r.spotFullyVisible);
        }

        [Test]
        public void ParseSpots_PutsTheVisibleItemFirst_AndDropsNonsense()
        {
            var item = new ItemProfile { name = "glasses", category = "glasses", sizeCm = new[] { 14f, 5f }, shape = "folded glasses frame" };
            string json = "{\"itemEvidence\":\"black frames on desk\",\"itemVisible\":true,\"itemBox\":[100,200,300,400]," +
                          "\"spots\":[{\"label\":\"chapstick\",\"box_2d\":[1,1,50,50],\"likelihood\":2}," +
                          "{\"label\":\"glasses case\",\"box_2d\":[500,500,600,700],\"likelihood\":3}]}";
            var spots = GeminiClient.ParseSpots(json, item, KB);

            Assert.AreEqual(2, spots.Count);
            Assert.IsTrue(spots[0].isItem);
            Assert.AreEqual("glasses case", spots[1].label);
        }
    }
}
