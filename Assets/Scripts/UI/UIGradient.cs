using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UstAldanQuiz.UI
{
    [RequireComponent(typeof(Graphic))]
    public class UIGradient : BaseMeshEffect
    {
        [SerializeField] private Color topColor    = Color.white;
        [SerializeField] private Color bottomColor = Color.white;

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!isActiveAndEnabled) return;

            var verts = new List<UIVertex>();
            vh.GetUIVertexStream(verts);

            float minY = float.MaxValue, maxY = float.MinValue;
            foreach (var v in verts)
            {
                if (v.position.y < minY) minY = v.position.y;
                if (v.position.y > maxY) maxY = v.position.y;
            }

            float h = maxY - minY;
            if (h <= 0f) return;

            for (int i = 0; i < verts.Count; i++)
            {
                var v = verts[i];
                float t = (v.position.y - minY) / h;
                v.color = Color32.Lerp(bottomColor, topColor, t);
                verts[i] = v;
            }

            vh.Clear();
            vh.AddUIVertexTriangleStream(verts);
        }
    }
}
