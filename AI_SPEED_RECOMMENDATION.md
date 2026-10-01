# Making FIND ME's AI faster: options and recommendation

## Where the time goes today (measured)

| Step | Time |
|---|---|
| Hold the phone still on a spot (dwell) | 1.2 s (was 2 s) |
| Take + compress the photo | ~0.1 s |
| Upload + Gemini answer, `gemini-3.5-flash-lite` | **median 2.2 s**, worst seen ~15 s |
| Upload + Gemini answer, `gemini-3.8-flash` (better) | 3–6 s, and on our free key it's overloaded (HTTP 503) about half the time |

So a check takes about **3.5 s** from "I point at it" to an answer. A person glancing at an open drawer needs well under a second. To be faster than the human eye, the "is it here?" part has to run **on the phone, on every frame**. No cloud API can do a network round-trip in under ~0.5 s reliably.

The cloud is still the right place for the *reasoning* ("where would a wallet hide in this room?", "is this drawer open?"). It just shouldn't sit in the moment-to-moment loop.

## The options

### A. Quick wins with what we have (free, hours)
- Keep flash-lite for checks; send 384 px instead of 512 px for checks (fewer image tokens).
- **Start the check early**: take the photo at ~60% of the hold, so the answer arrives right as the ring fills.
- Already done: 1.2 s hold, retry on a second model when one is overloaded.

| + | − |
|---|---|
| No new tech, no cost | Still ~2–3 s per check; free-tier overload (503) and rate limits (429) remain |

### B. Paid Gemini tier (cheapest real fix for reliability)
The 503 "high demand" and 429 "too many requests" errors we hit constantly are mostly a free-tier problem. A paid key (Google AI Studio billing), or *Provisioned Throughput* on Vertex AI, gives higher limits and priority capacity. That makes `gemini-3.8-flash` (more accurate) usable for every call.

| + | − |
|---|---|
| Zero code change; more accurate model becomes reliable | Costs money per call (check current pricing); latency per call barely changes (~2–4 s) |

### C. Gemini Live API (streaming)
Instead of one photo per question, the app keeps a WebSocket open and streams camera frames (about 1 per second). Gemini answers as soon as it sees something ("that's the drawer, it's closed").

| + | − |
|---|---|
| Feels "live", no hold-still, sub-second replies once connected | New API to integrate (websocket, audio/video session); cost per minute of streaming; still cloud, so it needs Wi-Fi |

### D. Another paid model (OpenAI, Claude Haiku, Groq-hosted open models…)
Similar per-call latency to Gemini Flash (typically 1–3 s for an image). Groq-hosted open models can be faster, but their vision quality is lower. Box coordinates (`box_2d`) are a Gemini strength; other models are weaker at precise boxes.

| + | − |
|---|---|
| A fallback if Gemini is down | Not meaningfully faster; we'd lose Gemini's good boxes; another key + billing |

### E. On-device detector + Gemini for reasoning (**recommended long-term**)
Run a small object detector **on the phone every frame** (10–30 fps) using Unity Inference, which is *already installed* in this project (`com.unity.ai.inference`, runs ONNX models on the phone's GPU/NPU). It recognises the ~30 items in our knowledge base and highlights the item the instant it enters the camera view. Gemini keeps doing what it's best at: choosing where to look, open vs. closed, "look closer".

Model choices:
- **MediaPipe Object Detector (EfficientDet-Lite)**: Apache 2.0, designed for phones, custom training with MediaPipe Model Maker. For Unity there's the MIT-licensed MediaPipe Unity Plugin, or export to ONNX for Unity Inference.
- **Open-vocabulary detectors** (OWLv2 Apache 2.0; YOLO-World): find any item from a text prompt, so "pink wallet" works with no training. They're heavier on a phone. Check the licence: Ultralytics YOLO is AGPL-3.0, which forces you to open-source the app.

| + | − |
|---|---|
| Faster than the human eye; works offline; no per-call cost; huge demo "wow" | 2–5 days to integrate one pretrained model; fine-tuning needs ~200–500 photos per item; battery/heat on the A80 |

### F. Train our own model from scratch
| + | − |
|---|---|
| "Our own AI" story | **Not worth it.** Needs tens of thousands of labelled photos and weeks of GPU time, and would still be worse than fine-tuning an existing model (E). Fine-tuning a pretrained detector on our 30 items *is* "our own model" for the judges, at a fraction of the effort. |

## Ranking

1. **E: on-device detector + Gemini** (long-term). The only option that's faster than a person looking. Start with a pretrained open-vocabulary model, then fine-tune on the 30 knowledge-base items.
2. **B: paid Gemini tier** (now, if possible). Removes the overload errors and makes the better model usable. Biggest quality gain per minute of work.
3. **A: quick wins** (now, free). Early photo during the hold and smaller check photos; saves ~0.5–1 s per check.
4. **C: Live API** (later). Great feel, but more integration work than E for less speed.
5. **D: other APIs** (fallback only).
6. **F: from scratch** (no).

## What to tell the judges

> "Today FIND ME uses Gemini for reasoning: where would *your* item hide, is this drawer open, could it still be under that pile? Next, a small detector runs on the phone itself, so the moment your wallet enters the camera view it lights up, faster than you'd notice it yourself, and it works offline. Gemini stays the brain; the phone becomes the eyes."
