﻿using System.Linq;
using UnityEngine;

namespace Wholesome
{
    public class Bases
    {
        public static Base Generic = new Base
        {
            Name = "Generic",
            Hand = new Base.Offset(new Vector3(0, 0.0472f, -0.0244f), new Vector3(0, -90, 0)),
            Pussy = new Base.Offset(new Vector3(0, -0.072f, -0.012f), new Vector3(90, 0, 0)),
            Anal = new Base.Offset(new Vector3(0, -0.054f, -0.04f), new Vector3(135, 0, 0)),
            Titjob = new Base.Offset(new Vector3(0, 0.0719f, 0.1095f), new Vector3(86.89201f, 0, 0)),
            Assjob = new Base.Offset(new Vector3(0, -0.0566f, -0.1204f), new Vector3(90, 0, 0)),
            Thighjob = new Base.Offset(new Vector3(0, 0.0995f, 0), new Vector3()),
            SoleFlat = new Base.Offset(new Vector3(0, 0.0751f, 0.0354f), new Vector3(64.428f, 0, 0)),
            SoleHeeled = new Base.Offset(new Vector3(0, 0.134f, 0.0222f), new Vector3(37.841f, 0, 0)),
            FootjobFlat = new Base.Offset(new Vector3(0, 0.0346f, 0.0181f), new Vector3(-25.567f, 0, 0)),
            FootjobHeeled = new Base.Offset(new Vector3(0, 0.0582f, 0.0059f), new Vector3()),
            DefaultTorsoLength = 0.3600037f
        };

        public static Base ImLeXz = new Base
        {
            Name = "ImLeXz",
            Hand = new Base.Offset(new Vector3(0, 0.057f, -0.0254f), new Vector3(0, -90, 0)),
            Pussy = new Base.Offset(new Vector3(0, -0.0891f, -0.00446f), new Vector3(90, 0, 0)),
            Anal = new Base.Offset(new Vector3(0, -0.083f, -0.0299f), new Vector3(100.454f, 0, 0)),
            Titjob = new Base.Offset(new Vector3(0, 0.0603f, 0.0937f), new Vector3(90f, 0, 0)),
            Assjob = new Base.Offset(new Vector3(0, -0.0449f, -0.0968f), new Vector3(90, 0, 0)),
            Thighjob = new Base.Offset(new Vector3(0, 0.0995f, 0), new Vector3()),
            SoleFlat = new Base.Offset(new Vector3(0, 0.0933f, 0.0435f), new Vector3(72.415f, 0, 0)),
            SoleHeeled = new Base.Offset(new Vector3(0, 0.0933f, 0.0435f), new Vector3(72.415f, 0, 0)),
            FootjobFlat = new Base.Offset(new Vector3(-0.0331f, 0.0474f, 0.0117f), new Vector3(-17.653f, 2.02f, -1.671f)),
            FootjobHeeled = new Base.Offset(new Vector3(-0.0331f, 0.0474f, 0.0117f), new Vector3(-17.653f, 2.02f, -1.671f)),
            PussyBlendshape = "Pussy_1",
            AnalBlendshape = "Ass_1",
            DefaultHipLength = 0.1005178f,
        };

        public static Base Panda = new Base
        {
            Name = "Panda",
            Hand = new Base.Offset(new Vector3(0, 0.0472f, -0.0244f), new Vector3(0, -90, 0)),
            Pussy = new Base.Offset(new Vector3(0, -0.072f, -0.012f), new Vector3(90, 0, 0)),
            Anal = new Base.Offset(new Vector3(0, -0.054f, -0.04f), new Vector3(135, 0, 0)),
            Titjob = new Base.Offset(new Vector3(0, 0.0719f, 0.1095f), new Vector3(86.89201f, 0, 0)),
            Assjob = new Base.Offset(new Vector3(0, -0.0566f, -0.1204f), new Vector3(90, 0, 0)),
            Thighjob = new Base.Offset(new Vector3(0, 0.0995f, 0), new Vector3()),
            SoleFlat = new Base.Offset(new Vector3(0, 0.0751f, 0.0354f), new Vector3(64.428f, 0, 0)),
            SoleHeeled = new Base.Offset(new Vector3(0, 0.134f, 0.0222f), new Vector3(37.841f, 0, 0)),
            FootjobFlat = new Base.Offset(new Vector3(0, 0.0346f, 0.0181f), new Vector3(-25.567f, 0, 0)),
            FootjobHeeled = new Base.Offset(new Vector3(0, 0.0582f, 0.0059f), new Vector3()),
            DefaultHipLength = 0.0807063f
        };

        public static Base ToriBase = new Base
        {
            Name = "Tori",
            Hand = new Base.Offset(new Vector3(0, 0.0644f, -0.0307f), new Vector3(0, -90f, 0)),
            Pussy = new Base.Offset(new Vector3(0, -0.127297f, -0.01245906f), new Vector3(90, 0, 0)),
            Anal = new Base.Offset(new Vector3(0, -0.100751f, -0.05263702f), new Vector3(126.387f, 0, 0)),
            Titjob = new Base.Offset(new Vector3(0, 0.0069f, 0.1068f), new Vector3(76.802f, 0, 0)),
            Assjob = new Base.Offset(new Vector3(0, -0.0926f, -0.0978f), new Vector3(90, 0, 0)),
            Thighjob = new Base.Offset(new Vector3(0, 0.0996f, 0), new Vector3()),
            SoleFlat = new Base.Offset(new Vector3(0, 0.0713f, 0.0586f), new Vector3(70.57101f, 0, 0)),
            SoleHeeled = new Base.Offset(new Vector3(0, 0.0933f, 0.1213f), new Vector3(70.57101f, 0, 0)),
            FootjobFlat = new Base.Offset(new Vector3(0, 0.0523f, 0.0238f), new Vector3(-20.642f, 0, 0)),
            FootjobHeeled = new Base.Offset(new Vector3(0, 0.03769f, 0.03854f), new Vector3(44.667f, 0, 0)),
            PussyBlendshape = "PUSSY_open",
            AnalBlendshape = "BUTTHOLE_open",
            DefaultHipLength = 0.102655f
        };

        public static Base ZinFit = new Base
        {
            Name = "Zin Fit",
            Hand = new Base.Offset(new Vector3(0, 0.0432f, -0.0284f), new Vector3(0, -90, 0)),
            Pussy = new Base.Offset(new Vector3(0, -0.072f, -0.012f), new Vector3(90, 0, 0)),
            Anal = new Base.Offset(new Vector3(0, -0.054f, -0.04f), new Vector3(135, 0, 0)),
            Titjob = new Base.Offset(new Vector3(0, 0.042f, 0.0996f), new Vector3(84.929f, 0, 0)),
            Assjob = new Base.Offset(new Vector3(0, -0.0379f, -0.1005f), new Vector3(90, 0, 0)),
            Thighjob = new Base.Offset(new Vector3(0, 0.0995f, 0), new Vector3()),
            SoleFlat = new Base.Offset(new Vector3(0, 0.0751f, 0.0354f), new Vector3(60f, 0, 0)),
            SoleHeeled = new Base.Offset(new Vector3(0, 0.12531f, 0.0054f), new Vector3(22.827f, 0, 0)),
            FootjobFlat = new Base.Offset(new Vector3(0, 0.0674f, 0.0083f), new Vector3(-31f, 0, 0)),
            FootjobHeeled = new Base.Offset(new Vector3(0, 0.0674f, 0.0083f), new Vector3(-15f, 0, 0)),
            PussyBlendshape = "Coochy Open",
            DefaultHipLength = 0.0715912f
        };

        public static Base ZinRP = new Base
        {
            Name = "Zin RP",
            Hand = new Base.Offset(new Vector3(0, 0.0432f, -0.0284f), new Vector3(0, -90, 0)),
            Pussy = new Base.Offset(new Vector3(0, -0.07113f, -0.012f), new Vector3(90, 0, 0)),
            Anal = new Base.Offset(new Vector3(0, -0.0582f, -0.0497f), new Vector3(116.692f, 0, 0)),
            Titjob = new Base.Offset(new Vector3(0, 0.064f, 0.0996f), new Vector3(84.929f, 0, 0)),
            Assjob = new Base.Offset(new Vector3(0, -0.0379f, -0.1005f), new Vector3(90, 0, 0)),
            Thighjob = new Base.Offset(new Vector3(0, 0.0995f, 0), new Vector3()),
            SoleFlat = new Base.Offset(new Vector3(0, 0.08f, 0.0421f), new Vector3(54.025f, 0, 0)),
            SoleHeeled = new Base.Offset(new Vector3(0, 0.1388f, 0.0117f), new Vector3(16.739f, 0, 0)),
            FootjobFlat = new Base.Offset(new Vector3(0, 0.0674f, 0.0083f), new Vector3(-31f, 0, 0)),
            FootjobHeeled = new Base.Offset(new Vector3(0, 0.0674f, 0.0083f), new Vector3(-15f, 0, 0)),
            PussyBlendshape = "Coochy Open",
            AnalBlendshape = "Butt Hole Open",
            DefaultHipLength = 0.0810318f
        };

        public static Base TVF = new Base
        {
            Name = "TVF",
            Hand = new Base.Offset(new Vector3(0.0000f, 0.0520f, -0.0269f), new Vector3(0.0000f, 270.0000f, 0.0000f)),
            Pussy = new Base.Offset(new Vector3(-0.0237f, -0.0757f, 0.0000f), new Vector3(75.0000f, 270.0000f, 270.0000f)),
            Anal = new Base.Offset(new Vector3(0.0382f, -0.0667f, 0.0000f), new Vector3(75.0000f, 90.0000f, 90.0000f)),
            Titjob = new Base.Offset(new Vector3(-0.1033f, 0.0321f, 0.0000f), new Vector3(90.0000f, 0.0000f, 0.0000f)),
            Assjob = new Base.Offset(new Vector3(0.1051f, -0.0103f, 0.0000f), new Vector3(90.0000f, 0.0000f, 0.0000f)),
            Thighjob = new Base.Offset(new Vector3(0.0000f, 0.0995f, 0.0000f), new Vector3(0.0004f, 2.2970f, -0.0212f)),
            SoleFlat = new Base.Offset(new Vector3(0.0000f, 0.0828f, 0.0390f), new Vector3(64.4280f, 0.0000f, 0.0000f)),
            SoleHeeled = new Base.Offset(new Vector3(0.0000f, 0.0828f, 0.0390f), new Vector3(64.4280f, 0.0000f, 0.0000f)),
            FootjobFlat = new Base.Offset(new Vector3(0.0000f, 0.0346f, 0.0181f), new Vector3(-25.5670f, 0.0000f, 0.0000f)),
            FootjobHeeled = new Base.Offset(new Vector3(0.0000f, 0.0346f, 0.0181f), new Vector3(-25.5670f, 0.0000f, 0.0000f)),
            PussyBlendshape = "Orifice",
            AnalBlendshape = "Orifice2",
            DefaultHipLength = 0.0761f,
        };
        
        public static Base Venus = new Base
        {
            Name = "Venus",
            Hand = new Base.Offset(new Vector3(-0.0000f, 0.0633f, -0.0253f), new Vector3(169.439f, 79.464f, 11.657f)),
            Pussy = new Base.Offset(new Vector3(0.0000f, -0.0569f, 0.0183f), new Vector3(90.0000f, 0.0000f, 0.0000f)),
            Anal = new Base.Offset(new Vector3(0.0000f, -0.0479f, -0.0146f), new Vector3(80.7675f, 180.0000f, 180.0000f)),
            Titjob = new Base.Offset(new Vector3(0.0000f, 0.0690f, 0.1099f), new Vector3(86.8920f, 0.0000f, 0.0000f)),
            Assjob = new Base.Offset(new Vector3(0.0000f, -0.0066f, -0.1191f), new Vector3(90.0000f, 0.0000f, 0.0000f)),
            Thighjob = new Base.Offset(new Vector3(0.0000f, 0.0893f, 0.0000f), new Vector3(0.0000f, 0.0000f, 0.0000f)),
            SoleFlat = new Base.Offset(new Vector3(0.0000f, 0.1181f, -0.0563f), new Vector3(21.5814f, 0.0000f, 0.0000f)),
            SoleHeeled = new Base.Offset(new Vector3(0.0000f, 0.1324f, 0.0259f), new Vector3(67.6311f, 0.0000f, 0.0000f)),
            FootjobFlat = new Base.Offset(new Vector3(0.0000f, 0.0548f, -0.0347f), new Vector3(292.7819f, 0.0000f, 0.0000f)),
            FootjobHeeled = new Base.Offset(new Vector3(0.0000f, 0.0686f, -0.0016f), new Vector3(346.4218f, 0.0000f, 0.0000f)),
            

            PussyBlendshape = "Vagina Opening",
            AnalBlendshape = "Ass Opening",
            DefaultHipLength = 0.0929f,
        };



        public static readonly Base[] All = { Generic, Panda, ImLeXz, ToriBase, TVF, Venus, ZinFit, ZinRP };
        public static string[] Names => All.Select(b => b.Name).ToArray();
    }
}