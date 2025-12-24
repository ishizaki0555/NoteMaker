using UnityEngine;

namespace NoteMaker.GLDrawing
{
    public class Geometry
    {
        public Color color;
        public Vector3[] vertices;

        public Geometry(Vector3[] vertices, Color color)
        {
            this.vertices = vertices;
            this.color = color;
        }
    }
}