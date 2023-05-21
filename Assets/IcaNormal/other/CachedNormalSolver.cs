﻿using System;
using System.Collections.Generic;
using TB;
using UnityEngine;
using UnityEngine.Serialization;


public class CachedNormalSolver : MonoBehaviour
{
    public float angle = 180f;
    public Mesh mesh;

    Dictionary<VertexKey, List<VertexEntry>> dictionary;
    float cosineThreshold;

    
    public Vector3[] newVertices;
    public int[] triangles;
    
    Vector3[] vertices;
    [FormerlySerializedAs("_normals")] public Vector3[] CalculatedNormals;
    // Holds the normal of each triangle in each sub mesh.
    Vector3[][] triNormals;

    
    private int vertexCount;
    private int triangleCount;
    Vector2[] uv;
    private int[][] submeshTriangles;

    /// //////////////////////////////
   
    Vector3[] tan1 ;
    Vector3[] tan2 ;

    Vector4[] tangents;

    
    private void Awake()
    {
        cosineThreshold = Mathf.Cos(angle * Mathf.Deg2Rad);
        vertexCount = mesh.vertexCount;

        vertices = mesh.vertices;
        CalculatedNormals = new Vector3[vertexCount];

        // Holds the normal of each triangle in each sub mesh.
        triNormals = new Vector3[mesh.subMeshCount][];

        triangles = mesh.triangles;
        triangleCount = mesh.triangles.Length;
        uv = mesh.uv;

        dictionary = new Dictionary<VertexKey, List<VertexEntry>>(vertexCount);


        submeshTriangles = new int[mesh.subMeshCount][];
        
        tan1 = new Vector3[vertexCount];
        tan2 = new Vector3[vertexCount];

        tangents = new Vector4[vertexCount];
        
        for (int subMeshIndex = 0; subMeshIndex < mesh.subMeshCount; ++subMeshIndex)
        {
            //int[] triangles = mesh.GetTriangles(subMeshIndex);
            submeshTriangles[subMeshIndex] =mesh.GetTriangles(subMeshIndex);

            triNormals[subMeshIndex] = new Vector3[submeshTriangles[subMeshIndex].Length / 3];

            for (int i = 0; i < triangles.Length; i += 3)
            {
                int i1 = submeshTriangles[subMeshIndex][i];
                int i2 = submeshTriangles[subMeshIndex][i + 1];
                int i3 = submeshTriangles[subMeshIndex][i + 2];

                // // Calculate the normal of the triangle
                // Vector3 p1 = vertices[i2] - vertices[i1];
                // Vector3 p2 = vertices[i3] - vertices[i1];
                // Vector3 normal = Vector3.Cross(p1, p2);
                // float magnitude = normal.magnitude;
                // if (magnitude > 0)
                // {
                //     normal /= magnitude;
                // }

                int triIndex = i / 3;

                List<VertexEntry> entry;
                VertexKey key;

                if (!dictionary.TryGetValue(key = new VertexKey(vertices[i1]), out entry))
                {
                    entry = new List<VertexEntry>(4);
                    dictionary.Add(key, entry);
                }

                entry.Add(new VertexEntry(subMeshIndex, triIndex, i1));

                if (!dictionary.TryGetValue(key = new VertexKey(vertices[i2]), out entry))
                {
                    entry = new List<VertexEntry>();
                    dictionary.Add(key, entry);
                }

                entry.Add(new VertexEntry(subMeshIndex, triIndex, i2));

                if (!dictionary.TryGetValue(key = new VertexKey(vertices[i3]), out entry))
                {
                    entry = new List<VertexEntry>();
                    dictionary.Add(key, entry);
                }

                entry.Add(new VertexEntry(subMeshIndex, triIndex, i3));
            }
        }
    }

    public Vector3[] RecalculateNormals()
    {
        for (int subMeshIndex = 0; subMeshIndex < mesh.subMeshCount; ++subMeshIndex)
        {
            //int[] triangles = mesh.GetTriangles(subMeshIndex);

            //triNormals[subMeshIndex] = new Vector3[triangles.Length / 3];

            for (int i = 0; i < submeshTriangles[subMeshIndex].Length; i += 3)
            {
                int i1 = submeshTriangles[subMeshIndex][i];
                int i2 = submeshTriangles[subMeshIndex][i + 1];
                int i3 = submeshTriangles[subMeshIndex][i + 2];

                // Calculate the normal of the triangle
                Vector3 p1 = newVertices[i2] - newVertices[i1];
                Vector3 p2 = newVertices[i3] - newVertices[i1];
                Vector3 normal = Vector3.Cross(p1, p2);
                float magnitude = normal.magnitude;
                if (magnitude > 0)
                {
                    normal /= magnitude;
                }


                int triIndex = i / 3;
                triNormals[subMeshIndex][triIndex] = normal;
            }
        }

        //Each entry in the dictionary represents a unique vertex position.

        foreach (List<VertexEntry> vertList in dictionary.Values)
        {
            var count = vertList.Count;

            for (int i = 0; i < count; ++i)
            {
                Vector3 sum = new Vector3();
                VertexEntry lhsEntry = vertList[i];

                for (int j = 0; j < count; ++j)
                {
                    VertexEntry rhsEntry = vertList[j];

                    if (lhsEntry.VertexIndex == rhsEntry.VertexIndex)
                    {
                        sum += triNormals[rhsEntry.MeshIndex][rhsEntry.TriangleIndex];
                        //sum = Add2(sum, triNormals[rhsEntry.MeshIndex][rhsEntry.TriangleIndex]);
                    }
                    else
                    {
                        // The dot product is the cosine of the angle between the two triangles.
                        // A larger cosine means a smaller angle.
                        float dot = Vector3.Dot(
                            triNormals[lhsEntry.MeshIndex][lhsEntry.TriangleIndex],
                            triNormals[rhsEntry.MeshIndex][rhsEntry.TriangleIndex]);
                        if (dot >= cosineThreshold)
                        {
                            sum += triNormals[rhsEntry.MeshIndex][rhsEntry.TriangleIndex];
                            //sum = Add2(sum, triNormals[rhsEntry.MeshIndex][rhsEntry.TriangleIndex]);
                        }
                    }
                }

                CalculatedNormals[lhsEntry.VertexIndex] = sum.normalized;
            }
        }

        return CalculatedNormals;
    }

    private struct VertexKey
    {
        private readonly long _x;
        private readonly long _y;
        private readonly long _z;

        // Change this if you require a different precision.
        private const int Tolerance = 100000;

        // Magic FNV values. Do not change these.
        private const long FNV32Init = 0x811c9dc5;
        private const long FNV32Prime = 0x01000193;

        public VertexKey(Vector3 position)
        {
            _x = (long)(Mathf.Round(position.x * Tolerance));
            _y = (long)(Mathf.Round(position.y * Tolerance));
            _z = (long)(Mathf.Round(position.z * Tolerance));
        }

        public override bool Equals(object obj)
        {
            VertexKey key = (VertexKey)obj;
            return _x == key._x && _y == key._y && _z == key._z;
        }

        public override int GetHashCode()
        {
            long rv = FNV32Init;
            rv ^= _x;
            rv *= FNV32Prime;
            rv ^= _y;
            rv *= FNV32Prime;
            rv ^= _z;
            rv *= FNV32Prime;

            return rv.GetHashCode();
        }
    }

    private struct VertexEntry
    {
        public int MeshIndex;
        public int TriangleIndex;
        public int VertexIndex;

        public VertexEntry(int meshIndex, int triIndex, int vertIndex)
        {
            MeshIndex = meshIndex;
            TriangleIndex = triIndex;
            VertexIndex = vertIndex;
        }
    }

    public static Vector3 Add2(Vector3 a, Vector3 b)
    {
        a.x += b.x;
        a.y += b.y;
        a.z += b.z;
        return a;
    }


    public Vector4[] RecalculateTangents()
    {
        // int[] triangles = mesh.triangles;
        // Vector3[] vertices = mesh.vertices;
        // Vector2[] uv = mesh.uv;
         //Vector3[] normals = mesh.normals;
        //
        // int triangleCount = triangles.Length;
        // int vertexCount = vertices.Length;

        Array.Clear(tan1,0,tan1.Length);
        Array.Clear(tan2,0,tan2.Length);
        Array.Clear(tangents,0,tangents.Length);




        for (int a = 0; a < triangleCount; a += 3)
        {
            int i1 = triangles[a + 0];
            int i2 = triangles[a + 1];
            int i3 = triangles[a + 2];

            Vector3 v1 = newVertices[i1];
            Vector3 v2 = newVertices[i2];
            Vector3 v3 = newVertices[i3];

            Vector2 w1 = uv[i1];
            Vector2 w2 = uv[i2];
            Vector2 w3 = uv[i3];

            float x1 = v2.x - v1.x;
            float x2 = v3.x - v1.x;
            float y1 = v2.y - v1.y;
            float y2 = v3.y - v1.y;
            float z1 = v2.z - v1.z;
            float z2 = v3.z - v1.z;

            float s1 = w2.x - w1.x;
            float s2 = w3.x - w1.x;
            float t1 = w2.y - w1.y;
            float t2 = w3.y - w1.y;

            float div = s1 * t2 - s2 * t1;
            float r = div == 0.0f ? 0.0f : 1.0f / div;

            Vector3 sDir = new Vector3((t2 * x1 - t1 * x2) * r, (t2 * y1 - t1 * y2) * r, (t2 * z1 - t1 * z2) * r);
            Vector3 tDir = new Vector3((s1 * x2 - s2 * x1) * r, (s1 * y2 - s2 * y1) * r, (s1 * z2 - s2 * z1) * r);

            tan1[i1] += sDir;
            tan1[i2] += sDir;
            tan1[i3] += sDir;

            tan2[i1] += tDir;
            tan2[i2] += tDir;
            tan2[i3] += tDir;
        }

        for (int a = 0; a < vertexCount; ++a)
        {
            Vector3 n = CalculatedNormals[a];
            Vector3 t = tan1[a];

            Vector3.OrthoNormalize(ref n, ref t);
            tangents[a].x = t.x;
            tangents[a].y = t.y;
            tangents[a].z = t.z;

            tangents[a].w = (Vector3.Dot(Vector3.Cross(n, t), tan2[a]) < 0.0f) ? -1.0f : 1.0f;
        }


        return tangents;
    }
}