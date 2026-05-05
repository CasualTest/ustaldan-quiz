using System;
using System.Collections.Generic;

namespace UstAldanQuiz.Data
{
    [Serializable]
    public class RoadmapNodeData
    {
        public string questionName;
        public float x;
        public float y;
        public List<int> edges = new List<int>();
    }

    [Serializable]
    public class RoadmapSaveData
    {
        public List<RoadmapNodeData> nodes = new List<RoadmapNodeData>();
    }
}
