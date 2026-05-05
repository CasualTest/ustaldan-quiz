using System.Collections.Generic;
using UnityEngine;
using UstAldanQuiz.Data;

namespace UstAldanQuiz.Managers
{
    public static class RoadmapManager
    {
        private const string SaveKey     = "roadmap_layout";
        private const float  CellSize    = 260f;
        private const float  Jitter      = 55f;
        private const int    ExtraEdges  = 2;

        // ── Generate ────────────────────────────────────────────────────────

        public static RoadmapSaveData Generate(List<QuestionData> questions)
        {
            int count = questions.Count;
            int cols  = Mathf.Max(2, Mathf.CeilToInt(Mathf.Sqrt(count)));

            float ox = CellSize;
            float oy = CellSize;

            var nodes = new List<RoadmapNodeData>(count);
            for (int i = 0; i < count; i++)
            {
                int row = i / cols;
                int col = i % cols;
                float x =  ox + col * CellSize + Random.Range(-Jitter, Jitter);
                float y = -(oy + row * CellSize + Random.Range(-Jitter, Jitter));
                nodes.Add(new RoadmapNodeData { questionName = questions[i].name, x = x, y = y });
            }

            BuildMST(nodes);
            AddExtraEdges(nodes);

            return new RoadmapSaveData { nodes = nodes };
        }

        private static void BuildMST(List<RoadmapNodeData> nodes)
        {
            int n = nodes.Count;
            if (n <= 1) return;

            var inMST  = new bool[n];
            var minCost = new float[n];
            var parent  = new int[n];
            for (int i = 0; i < n; i++) { minCost[i] = float.MaxValue; parent[i] = -1; }
            minCost[0] = 0f;

            for (int iter = 0; iter < n; iter++)
            {
                int u = -1;
                for (int i = 0; i < n; i++)
                    if (!inMST[i] && (u < 0 || minCost[i] < minCost[u])) u = i;

                inMST[u] = true;
                if (parent[u] >= 0) AddEdge(nodes, parent[u], u);

                for (int v = 0; v < n; v++)
                {
                    if (inMST[v]) continue;
                    float d = Dist(nodes[u], nodes[v]);
                    if (d < minCost[v]) { minCost[v] = d; parent[v] = u; }
                }
            }
        }

        private static void AddExtraEdges(List<RoadmapNodeData> nodes)
        {
            int n = nodes.Count;
            for (int i = 0; i < n; i++)
            {
                var sorted = new List<(float d, int idx)>(n - 1);
                for (int j = 0; j < n; j++)
                {
                    if (i == j) continue;
                    sorted.Add((Dist(nodes[i], nodes[j]), j));
                }
                sorted.Sort((a, b) => a.d.CompareTo(b.d));

                int added = 0;
                foreach (var (_, j) in sorted)
                {
                    if (added >= ExtraEdges) break;
                    if (!nodes[i].edges.Contains(j)) { AddEdge(nodes, i, j); added++; }
                }
            }
        }

        private static void AddEdge(List<RoadmapNodeData> nodes, int a, int b)
        {
            if (!nodes[a].edges.Contains(b)) nodes[a].edges.Add(b);
            if (!nodes[b].edges.Contains(a)) nodes[b].edges.Add(a);
        }

        private static float Dist(RoadmapNodeData a, RoadmapNodeData b)
        {
            float dx = a.x - b.x, dy = a.y - b.y;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        // ── Save / Load / Clear ─────────────────────────────────────────────

        public static void Save(RoadmapSaveData data)
        {
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        public static RoadmapSaveData Load()
        {
            string json = PlayerPrefs.GetString(SaveKey, null);
            if (string.IsNullOrEmpty(json)) return null;
            try { return JsonUtility.FromJson<RoadmapSaveData>(json); }
            catch { return null; }
        }

        public static void Clear()
        {
            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.Save();
        }
    }
}
