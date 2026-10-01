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
        /// <summary>Same as GetItemProfile, from a photo of the item (or one like it) plus optional text.</summary>
        Task<ItemProfile> GetItemProfileFromPhoto(byte[] jpg, string userText, CancellationToken ct);
        Task<List<HidingSpot>> FindSpots(byte[] jpg, ItemProfile item, CancellationToken ct);
        /// <param name="note">Extra context for this spot, e.g. "the user said a previous match was not their wallet". May be null.</param>
        Task<SpotCheckResult> CheckSpot(byte[] jpg, ItemProfile item, string spotLabel, string note, CancellationToken ct);
    }
}
