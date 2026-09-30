using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FindMe
{
    /// <summary>
    /// The three Gemini calls. Real (<see cref="GeminiClient"/>) and fake (<see cref="MockGeminiService"/>)
    /// both implement this, so the rest of the app never knows which one it's talking to.
    /// </summary>
    public interface IGeminiService
    {
        Task<ItemProfile> GetItemProfile(string userText, CancellationToken ct);
        Task<List<HidingSpot>> FindSpots(byte[] jpg, ItemProfile item, CancellationToken ct);
        Task<SpotCheckResult> CheckSpot(byte[] jpg, ItemProfile item, string spotLabel, CancellationToken ct);
    }
}
