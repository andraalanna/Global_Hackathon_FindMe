using System.Collections.Generic;
using UnityEngine;

namespace FindMe
{
    /// <summary>
    /// What ARCore measured at a grid of screen points at the exact moment a photo was taken:
    /// for each cell, the 3D point on the first surface (depth map, plane or feature point), or nothing.
    /// Later, Gemini's box tells us WHICH cells belong to the hiding spot; the grid tells us how far away they are.
    /// This works the same looking down, up, at walls or at tables, and needs no floor detection.
    /// </summary>
    public class SurfaceGrid
    {
        public int cols, rows;
        public Vector3[] points;              // world position of the hit per cell
        public PlacementQuality[] kinds;       // what was hit; Guess = nothing hit in that cell

        public SurfaceGrid(int cols, int rows)
        {
            this.cols = cols;
            this.rows = rows;
            points = new Vector3[cols * rows];
            kinds = new PlacementQuality[cols * rows];
        }

        /// <summary>Cell centre in viewport coordinates (0..1, origin bottom-left, the SCREEN image).</summary>
        public Vector2 CellViewport(int c, int r) => new((c + 0.5f) / cols, (r + 0.5f) / rows);

        public int MeasuredCount
        {
            get
            {
                int n = 0;
                foreach (var k in kinds) if (k != PlacementQuality.Guess) n++;
                return n;
            }
        }

        /// <summary>
        /// The densest group of distances (the surface most of the box is made of), nearest group on ties.
        /// <paramref name="sorted"/> must be sorted ascending. A group spans d .. d × (1 + relTol) + absTol,
        /// so it tolerates a curved bag front but not the wall 1 m behind it. Returns inclusive index range.
        /// </summary>
        public static (int start, int end) DensestCluster(IReadOnlyList<float> sorted, float relTol = 0.12f, float absTol = 0.05f)
        {
            int n = sorted.Count, bestS = 0, bestE = -1, e = 0;
            for (int s = 0; s < n; s++)
            {
                float limit = sorted[s] * (1f + relTol) + absTol;
                if (e < s) e = s;
                while (e + 1 < n && sorted[e + 1] <= limit) e++;
                if (e - s > bestE - bestS) { bestS = s; bestE = e; }   // strictly bigger: ties keep the nearer group
            }
            return (bestS, bestE);
        }
    }
}
