using System.Collections;
using System.Collections.Generic;
using Ica.Tests.Shared;
using Ica.Utils;
using Unity.Collections;
using UnityEngine;

namespace Ica.Normal
{
    public class IcaRec : MonoBehaviour
    {
        public int seg1;
        public int seg2;
        public float rad;

        public Mesh mesh;
        [Range(0,180)]
        public float Angle;
        private void Start()
        {

            var o = MeshCreate.CreateUvSphere(seg1,seg2,rad);
            mesh = o.GetComponent<MeshFilter>().sharedMesh;

        }

        private void Update()
        {
            mesh.RecalculateNormalsIca(Angle);
        }
    }
    
}