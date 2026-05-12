using System.Collections.Generic;
using UnityEngine;
using UstAldanQuiz.Data;

namespace UstAldanQuiz.Managers
{
    /// <summary>
    /// Генерирует змейкообразный путь из вопросов (как в Quizzland)
    /// и сохраняет/загружает его через PlayerPrefs.
    /// </summary>
    public static class RoadmapManager
    {
        private const string SaveKey = "roadmap_layout";

        // Layout constants — должны совпадать с RoadmapUI.TileSize
        public const int   Cols      = 3;
        public const float TileSize  = 200f;
        public const float HGap      = 28f;
        public const float VGap      = 28f;
        public const float TopMargin = 80f;

        private static float StepX => TileSize + HGap;
        private static float StepY => TileSize + VGap;

        // ── Generate ────────────────────────────────────────────────────────

        /// <summary>
        /// Создаёт змейку из <paramref name="questions"/> в 3 колонки.
        /// Чётные строки идут слева направо, нечётные — справа налево.
        /// </summary>
        public static RoadmapSaveData Generate(List<QuestionData> questions)
        {
            int count = questions.Count;
            var colX  = ColumnCenters();

            var nodes = new List<RoadmapNodeData>(count);
            for (int i = 0; i < count; i++)
            {
                int  row          = i / Cols;
                int  col          = i % Cols;
                bool leftToRight  = (row % 2 == 0);

                float x = leftToRight ? colX[col] : colX[Cols - 1 - col];
                float y = -(TopMargin + TileSize / 2f + row * StepY);

                nodes.Add(new RoadmapNodeData { questionName = questions[i].name, x = x, y = y });
            }

            // Последовательные соединения — просто цепочка
            for (int i = 0; i < count - 1; i++)
            {
                nodes[i].edges.Add(i + 1);
                nodes[i + 1].edges.Add(i);
            }

            return new RoadmapSaveData { nodes = nodes };
        }

        /// <summary>Горизонтальные центры 3 колонок, выровненных по центру 1080px.</summary>
        private static float[] ColumnCenters()
        {
            float totalW  = Cols * TileSize + (Cols - 1) * HGap;
            float leftEdge = (1080f - totalW) / 2f;
            var   centers  = new float[Cols];
            for (int c = 0; c < Cols; c++)
                centers[c] = leftEdge + TileSize / 2f + c * StepX;
            return centers;
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
