using System.Linq;
using UnityEngine;

namespace Wholesome
{
    public class Bases
    {
        public static Base Generic = new Base
        {
            Name = "Generic",
            Hand = new Base.Offset
            {
                Positon = new Vector3(0, 0.0472f, -0.0244f),
                EulerAngles = new Vector3(0, 90, 0)
            },
            Pussy = new Base.Offset
            {
                Positon = new Vector3(0, -0.072f, -0.012f),
                EulerAngles = new Vector3(90, 0, 0)
            },
            Anal = new Base.Offset
            {
                Positon = new Vector3(0, -0.054f, -0.04f),
                EulerAngles = new Vector3(135, 0, 0)
            },
            Titjob = new Base.Offset
            {
                Positon = new Vector3(),
                EulerAngles = new Vector3(),
            },
            Assjob = new Base.Offset
            {
                Positon = new Vector3(),
                EulerAngles = new Vector3(),
            },
            SoleFlat = new Base.Offset
            {
                Positon = new Vector3(0, 0.0751f, 0.0354f),
                EulerAngles = new Vector3(60f, 0, 0),
            },
            DefaultHipLength = 0.0807063f
        };
        
        public static Base ImLeXz = new Base
        {
            Name = "ImLeXz",
            DefaultHipLength = 0.0807063f,
        };
        
        public static Base Panda = new Base
        {
            Name = "Panda",
            Hand = new Base.Offset
            {
                Positon = new Vector3(0, 0.0472f, -0.0244f),
                EulerAngles = new Vector3(0, -90, 0)
            },
            Pussy = new Base.Offset
            {
                Positon = new Vector3(0, -0.072f, -0.012f),
                EulerAngles = new Vector3(90, 0, 0)
            },
            Anal = new Base.Offset
            {
                Positon = new Vector3(0, -0.054f, -0.04f),
                EulerAngles = new Vector3(135, 0, 0)
            },
            Titjob = new Base.Offset
            {
                Positon = new Vector3(0, 0.0719f, 0.1095f),
                EulerAngles = new Vector3(86.89201f, 0, 0),
            },
            Assjob = new Base.Offset
            {
                Positon = new Vector3(0, -0.0566f, -0.1204f),
                EulerAngles = new Vector3(90, 0, 0),
            },
            Thighjob = new Base.Offset(new Vector3(0, 0.0995f, 0), new Vector3()),
            SoleFlat = new Base.Offset
            {
                Positon = new Vector3(0, 0.0751f, 0.0354f),
                EulerAngles = new Vector3(60f, 0, 0),
            },
            SoleHeeled = new Base.Offset
            {
                Positon = new Vector3(0, 0.134f, 0.0222f),
                EulerAngles = new Vector3(37.841f, 0, 0),
            },
            FootjobFlat = new Base.Offset
            {
                Positon = new Vector3(0, 0.0346f, 0.0181f),
                EulerAngles = new Vector3(-25.567f, 0, 0),
            },
            FootjobHeeled = new Base.Offset
            {
                Positon = new Vector3(0, 0.0582f, 0.0059f),
                EulerAngles = new Vector3(),
            },
            DefaultHipLength = 0.0807063f
        };

        public static Base ToriBase = new Base
        {
            Name = "Tori Base",
            Hand = new Base.Offset(new Vector3(-0.0136f, 0.0537f, -0.0272f), new Vector3(0, -65.05901f, 0)),
            Pussy = new Base.Offset(new Vector3(0, -0.127297f, -0.01245906f), new Vector3(90, 0, 0)),
            Anal = new Base.Offset(new Vector3(0, -0.100751f, -0.05263702f), new Vector3(126.387f,0, 0)),
            Titjob = new Base.Offset(new Vector3(0, 0.0069f, 0.1068f), new Vector3(76.802f, 0, 0)),
            Assjob = new Base.Offset(new Vector3(0, -0.0926f, -0.0978f), new Vector3(90, 0, 0)),
            SoleFlat = new Base.Offset(new Vector3(0, 0.0713f, 0.0586f), new Vector3(70.57101f, 0, 0)),
            SoleHeeled = new Base.Offset(new Vector3(0, 0.0933f, 0.1213f), new Vector3(70.57101f, 0, 0)),
            PussyBlendshape = "PUSSY_open",
            AnalBlendshape = "BUTTHOLE_open",
            DefaultHipLength = 0.102655f
        };
        
        public static Base ZinFit = new Base
        {
            Name = "Zin Fit",
            Hand = new Base.Offset
            {
                Positon = new Vector3(0, 0.0432f, -0.0284f),
                EulerAngles = new Vector3(0, -90, 0)
            },
            Pussy = new Base.Offset
            {
                Positon = new Vector3(0, -0.072f, -0.012f),
                EulerAngles = new Vector3(90, 0, 0)
            },
            Anal = new Base.Offset
            {
                Positon = new Vector3(0, -0.054f, -0.04f),
                EulerAngles = new Vector3(135, 0, 0)
            },
            Titjob = new Base.Offset
            {
                Positon = new Vector3(0, 0.042f, 0.0996f),
                EulerAngles = new Vector3(84.929f, 0, 0),
            },
            Assjob = new Base.Offset
            {
                Positon = new Vector3(0, -0.0379f, -0.1005f),
                EulerAngles = new Vector3(90, 0, 0),
            },
            Thighjob = new Base.Offset(new Vector3(0, 0.0995f, 0), new Vector3()),
            SoleFlat = new Base.Offset
            {
                Positon = new Vector3(0, 0.0751f, 0.0354f),
                EulerAngles = new Vector3(60f, 0, 0),
            },
            SoleHeeled = new Base.Offset
            {
                Positon = new Vector3(0, 0.12531f, 0.0054f),
                EulerAngles = new Vector3(22.827f, 0, 0),
            },
            FootjobFlat = new Base.Offset(new Vector3(0, 0.0674f, 0.0083f), new Vector3(-31f, 0, 0)),
            FootjobHeeled = new Base.Offset(new Vector3(0, 0.0674f, 0.0083f), new Vector3(-15f, 0, 0)),
            PussyBlendshape = "Coochy Open",
            DefaultHipLength = 0.0715912f
        };
        public static Base ZinRP = new Base
        {
            Name = "Zin RP",
            Hand = new Base.Offset
            {
                Positon = new Vector3(0, 0.0472f, -0.0244f),
                EulerAngles = new Vector3(0, 90, 0)
            },
            Pussy = new Base.Offset
            {
                Positon = new Vector3(0, -0.072f, -0.012f),
                EulerAngles = new Vector3(90, 0, 0)
            },
            Anal = new Base.Offset
            {
                Positon = new Vector3(0, -0.054f, -0.04f),
                EulerAngles = new Vector3(135, 0, 0)
            },
            Titjob = new Base.Offset
            {
                Positon = new Vector3(),
                EulerAngles = new Vector3(),
            },
            Assjob = new Base.Offset
            {
                Positon = new Vector3(),
                EulerAngles = new Vector3(),
            },
            SoleFlat = new Base.Offset
            {
                Positon = new Vector3(0, 0.0751f, 0.0354f),
                EulerAngles = new Vector3(60f, 0, 0),
            },
            PussyBlendshape = "Coochy Open"
        };

        public static readonly Base[] All = { Generic, Panda, ImLeXz, ToriBase, ZinFit, ZinRP };
        public static string[] Names => All.Select(b => b.Name).ToArray();
    }
}