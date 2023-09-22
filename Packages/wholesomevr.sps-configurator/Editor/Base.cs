
using UnityEngine;

namespace Wholesome
{
    public class Base
    {
        public string Name;
        public Offset Hand;
        public Offset Pussy;
        public Offset Anal;
        public Offset Titjob;
        public Offset Assjob;
        public Offset Thighjob;
        public Offset SoleFlat;
        public Offset SoleHeeled;
        public Offset FootjobFlat;
        public Offset FootjobHeeled;
        //TODO: Armpit? Pussyrub

        public string PussyBlendshape;
        public string AnalBlendshape;

        public float DefaultHipLength;
        public float DefaultTorsoLength;

        public struct Offset
        {
            public Vector3 Positon;
            public Vector3 EulerAngles;

            public Offset(Vector3 positon, Vector3 eulerAngles)
            {
                Positon = positon;
                EulerAngles = eulerAngles;
            }
        }
    }
}