using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VF.Component;

namespace Wholesome
{
    public class Base
    {
        public string Name;

        [Socket(Name = "Handjob", Category = Category.Handjob, Type = SocketType.Ring, Bone = Bone.Hand, Symmetric = true, Both = true, BothName = "Double Handjob")]
        public Offset Hand;

        [Socket(Type = SocketType.Hole, Bone = Bone.Hips, Blendshape = true)]
        public Offset Pussy;

        [Socket(Type = SocketType.Hole, Bone = Bone.Hips, Blendshape = true)]
        public Offset Anal;

        [Socket(Category = Category.Special, Bone = Bone.Chest, Type = SocketType.Ring)]
        public Offset Titjob;

        [Socket(Category = Category.Special, Bone = Bone.Hips, Type = SocketType.Ring)]
        public Offset Assjob;

        [Socket(Category = Category.Special, Bone = Bone.Leg, Type = SocketType.Ring, Parent = true)]
        public Offset Thighjob;

        [Socket(Name = "Steppies", Category = Category.Feet, Type = SocketType.Ring, Bone = Bone.Foot, Symmetric = true,
            FootType = FootType.Flat)]
        public Offset SoleFlat;

        [Socket(Name = "Steppies", Category = Category.Feet, Type = SocketType.Ring, Bone = Bone.Foot, Symmetric = true,
            FootType = FootType.Heeled)]
        public Offset SoleHeeled;
        
        [Socket(Name = "Footjob", Category = Category.Feet, Type = SocketType.Ring, Bone = Bone.Foot, Parent = true,
            FootType = FootType.Flat)]
        public Offset FootjobFlat;
        
        [Socket(Name = "Footjob", Category = Category.Feet, Type = SocketType.Ring, Bone = Bone.Foot, Parent = true,
            FootType = FootType.Heeled)]
        public Offset FootjobHeeled;
        
        //TODO: Armpit? Pussyrub

        [Blendshape("Pussy")] public string PussyBlendshape;
        [Blendshape("Anal")] public string AnalBlendshape;

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

        public enum Category
        {
            Default,
            Handjob,
            Special,
            Feet
        }

        public static Category[] Categories => Enum.GetValues(typeof(Category)) as Category[];

        public enum SocketType
        {
            Hole,
            Ring
        }

        public enum FootType
        {
            Flat,
            Heeled,
            Both
        }

        public enum Bone
        {
            Hips,
            Spine,
            Chest,
            Head,
            Hand,
            Leg,
            Foot
        }

        public enum Direction
        {
            Left, Right
        }

        public static string BoneName(Bone bone, Direction direction = Direction.Left)
        {
            var dir = direction.ToString();
            switch (bone)
            {
                case Bone.Hand:
                    return $"{dir}Hand";
                case Bone.Leg:
                    return $"{dir}UpperLeg";
                case Bone.Foot:
                    return $"{dir}Foot";
                default:
                    return bone.ToString();
            }
        }

        [AttributeUsage(AttributeTargets.Field)]
        private class SocketAttribute : Attribute
        {
            //public string Name;
            public string Name;
            public string BothName;
            public Category Category = Category.Default;
            public SocketType Type;
            public Bone Bone;
            public bool Blendshape = false;
            public bool Symmetric = false;
            public bool Both = false;
            public bool Parent = false;
            public FootType FootType = FootType.Both;
        }

        [AttributeUsage(AttributeTargets.Field)]
        private class BlendshapeAttribute : Attribute
        {
            //public string Name;
            public string SocketName;

            public BlendshapeAttribute(string socketName)
            {
                SocketName = socketName;
            }
        }

        public struct SocketInfo
        {
            public string Name;
            public string DisplayName;
            public string BothName;
            public Category Category;
            public SocketType Type;
            public Bone Bone;
            public bool Blendshape;
            public bool Symmetric;
            public bool Both;
            public bool Parent;
            public FootType FootType;
        }

        public struct Socket
        {
            public SocketInfo Info;
            public Offset Location;
            public string Blendshape;
        }

        private static Dictionary<string, SocketInfo> _socketInfos;
        private static Dictionary<Category, List<string>> _categoryToSocket;
        private Dictionary<string, Socket> _sockets;

        static Base()
        {
            var type = typeof(Base);
            _socketInfos = type.GetFields()
                .Where(field => Attribute.GetCustomAttribute(field, typeof(SocketAttribute)) != null)
                .Select(field =>
                {
                    var socketAttribute =
                        Attribute.GetCustomAttribute(field, typeof(SocketAttribute)) as SocketAttribute;
                    return new SocketInfo
                    {
                        Name = field.Name,
                        DisplayName = socketAttribute.Name ?? field.Name,
                        BothName = socketAttribute.BothName,
                        Category = socketAttribute.Category,
                        Type = socketAttribute.Type,
                        Bone = socketAttribute.Bone,
                        Blendshape = socketAttribute.Blendshape,
                        Symmetric = socketAttribute.Symmetric,
                        Both = socketAttribute.Both,
                        Parent = socketAttribute.Parent,
                        FootType = socketAttribute.FootType
                    };
                }).ToDictionary(info => info.Name, info => info);
            _categoryToSocket = new Dictionary<Category, List<string>>();
            foreach (var info in _socketInfos.Values)
            {
                if (_categoryToSocket.TryGetValue(info.Category, out var value))
                {
                    value.Add(info.Name);
                }
                else
                {
                    _categoryToSocket[info.Category] = new List<string> { info.Name };
                }
            }
        }

        public Base()
        {
            var type = typeof(Base);
            _sockets = type.GetFields()
                .Where(field => Attribute.GetCustomAttribute(field, typeof(SocketAttribute)) != null)
                .Select(field => new Socket
                {
                    Info = _socketInfos[field.Name],
                    Location = (Offset)field.GetValue(this)
                }).ToDictionary(socket => socket.Info.Name, socket => socket);
        }

        public static IEnumerable<SocketInfo> SocketsInCategory(Category category)
        {
            var sockets = _categoryToSocket[category].Select(name => _socketInfos[name]);
            return sockets;
        }

        public static IEnumerable<SocketInfo> SocketInfos => _socketInfos.Values;

        private string GetBlendshapeForSocket(string socketName)
        {
            var type = typeof(Base);
            var blendshape = type.GetFields()
                .Where(field =>
                {
                    if (Attribute.GetCustomAttribute(field, typeof(BlendshapeAttribute)) is BlendshapeAttribute attr)
                    {
                        return attr.SocketName == socketName;
                    }

                    return false;
                })
                .Select(field => field.GetValue(this) as string)
                .FirstOrDefault();
            return blendshape;
        }

        public IEnumerable<Socket> GetSocketsForFootType(FootType footType)
        {
            var type = typeof(Base);
            var sockets = type.GetFields()
                .Where(field => Attribute.GetCustomAttribute(field, typeof(SocketAttribute)) != null)
                .Select(field => new Socket
                {
                    Info = _socketInfos[field.Name],
                    Location = (Offset)field.GetValue(this),
                    Blendshape = GetBlendshapeForSocket(field.Name)
                })
                .Where(socket => socket.Info.FootType == footType || socket.Info.FootType == FootType.Both).ToList();
            return sockets;
        }

        public IEnumerable<Socket> Sockets
        {
            get
            {
                var type = typeof(Base);
                var sockets = type.GetFields()
                    .Where(field => Attribute.GetCustomAttribute(field, typeof(SocketAttribute)) != null)
                    .Select(field => new Socket
                    {
                        Info = _socketInfos[field.Name],
                        Location = (Offset)field.GetValue(this),
                        Blendshape = GetBlendshapeForSocket(field.Name)
                    }).ToList();
                return sockets;
            }
        }

        public IEnumerable<string> Blendshapes =>
            typeof(Base).GetFields()
                .Where(field => Attribute.GetCustomAttribute(field, typeof(BlendshapeAttribute)) != null)
                .Select(field => field.GetValue(this) as string)
                .Where(blendshape => blendshape != null).ToList();
    }
}