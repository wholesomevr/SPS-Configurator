using System;
using System.Collections.Generic;
using System.Linq;
using VF.Component;
using UnityEngine;
using VF.Model;
using VF.Model.Feature;
using VRC.SDK3.Avatars.Components;
using Object = UnityEngine.Object;

namespace Wholesome
{
    public class SocketCleaner
    {
        private GameObject avatarObject;
        private SPSConfigurator.AvatarArmature avatarArmature;

        public SocketCleaner(GameObject avatarObject)
        {
            if (avatarObject.GetComponent<VRCAvatarDescriptor>() == null)
                throw new ArgumentException("avatarObject doesn't have a VRC Avatar Descriptor");
            this.avatarObject = avatarObject;
            avatarArmature = new SPSConfigurator.AvatarArmature(avatarObject);
        }

        public void Clean(bool newVersion = true, bool keep = false)
        {
            if (newVersion)
            {
                CleanNew(keep: keep);
            }
            else
            {
                CleanOld(keep: keep);
            }
        }

        public static void DeleteNotUsedSockets(IEnumerable<VRCFuryHapticSocket> sockets)
        {
            if (sockets == null) return;
            foreach (var socket in sockets)
            {
                Object.DestroyImmediate(socket);
            }
        }

        private void CleanNew(string spsPath = "SPS", bool keep = false)
        {
            var sps = avatarObject.transform.Find("SPS");
            if (sps == null) return;
            var sockets = sps.GetComponentsInChildren<VRCFuryHapticSocket>(true);
            RemoveSockets(sockets, keep);
            var hj = sps.Find("Handjob");
            if (hj != null) RemoveIconSet(hj.gameObject, spsPath, "Handjob");
            var special = sps.Find("Special");
            if (special != null) RemoveIconSet(special.gameObject, spsPath, "Special");
            var feet = sps.Find("Feet");
            if (feet != null) RemoveIconSet(feet.gameObject, spsPath, "Feet");
            if (sps != null && sps.childCount == 0 && sps.GetComponents(typeof(MonoBehaviour)).Length == 0)
                Object.DestroyImmediate(sps.gameObject);
        }

        private void CleanOld(string spsPath = "SPS", bool keep = false)
        {
            var armature = avatarArmature.FindBone(HumanBodyBones.Hips).parent;
            var furies = avatarObject.GetComponents<VRCFury>();
            string[] possiblePaths =
            {
                "Sockets/Handjob", "Sockets/Special", "Sockets/Feet", "SPS/Handjob", "SPS/Special", "SPS/Feet",
                $"{spsPath}/Handjob", $"{spsPath}/Special", $"{spsPath}/Feet"
            };
            var possibleIcons = Names.All.SelectMany(name =>
                new[]
                {
                    $"SPS/{name}", $"Sockets/{name}"
                }).Concat(possiblePaths).ToList();
            foreach (var fury in furies)
            {
                fury.config.features.RemoveAll(feature =>
                    feature is MoveMenuItem m && (m.fromPath == "SPS" || m.fromPath == "Sockets"));
                fury.config.features.RemoveAll(feature =>
                    feature is MoveMenuItem m && possiblePaths.Contains(m.fromPath));
                fury.config.features.RemoveAll(feature =>
                    feature is SetIcon i && (possibleIcons.Contains(i.path) || i.path == "SPS" || i.path == "Sockets"));
                fury.config.features.RemoveAll(feature => feature is SpsOptions);
                if (fury.config.features.Count == 0)
                {
                    Object.DestroyImmediate(fury);
                }
            }
            
            var sockets = armature.GetComponentsInChildren<VRCFuryHapticSocket>(true);
            RemoveSockets(sockets, keep);
        }

        private void RemoveSockets(IEnumerable<VRCFuryHapticSocket> sockets, bool keep = false)
        {
            foreach (var socket in sockets)
            {
                if (Names.All.Contains(socket.gameObject.name))
                {
                    Transform parent = socket.transform.parent;
                    if (keep)
                    {
                        socket.transform.parent = null;
                    }
                    else
                    {
                        Object.DestroyImmediate(socket.gameObject);
                    }
                    var comps = parent.GetComponents(typeof(MonoBehaviour));
                    if (parent.childCount == 0 && comps.Length == 0)
                    {
                        Object.DestroyImmediate(parent.gameObject);
                    }
                }
            }
        }

        private void RemoveIconSet(GameObject gameObject, string spsPath, string category)
        {
            var furies = gameObject.GetComponents<VRCFury>();
            foreach (var fury in furies)
            {
                fury.config.features.RemoveAll(f => f is SetIcon i && i.path == $"{spsPath}/{category}");
                if (fury.config.features.Count == 0)
                {
                    Object.DestroyImmediate(fury);                    
                }
            }
            if(gameObject.transform.childCount == 0 && gameObject.GetComponents(typeof(MonoBehaviour)).Length == 0) Object.DestroyImmediate(gameObject);
        }


        public static void Clear2(GameObject avatarGameObject)
        {
            string[] socketNames =
            {
                Names.BlowjobName, $"Handjob/{Names.HandjobName} Right", $"Handjob/{Names.HandjobName} Left",
                $"Handjob/Double {Names.HandjobName}", Names.PussyName,
                Names.AnalName, $"Special/{Names.TitjobName}", $"Special/{Names.AssjobName}",
                $"Special/{Names.ThighjobName}",
                $"Feet/{Names.SoleName} Left", $"Feet/{Names.SoleName} Right",
                $"Feet/{Names.FootjobName}"
            };
            var sockets = avatarGameObject.GetComponentsInChildren<VRCFuryHapticSocket>(true);

            foreach (var socket in sockets)
            {
                if (socket != null)
                {
                    if (socketNames.Contains(socket.name))
                    {
                        Transform parent = socket.transform.parent;
                        Object.DestroyImmediate(socket.gameObject);
                        if (parent.name == "SPS")
                        {
                            if (parent.childCount == 0)
                            {
                                Object.DestroyImmediate(parent.gameObject);
                            }
                        }
                    }
                }
            }

            var furies = avatarGameObject.GetComponents<VRCFury>();
            string[] possiblePaths =
            {
                "Sockets/Handjob", "Sockets/Special", "Sockets/Feet", "SPS/Handjob", "SPS/Special", "SPS/Feet"
            };
            var possibleIcons = socketNames.SelectMany(name =>
                new[]
                {
                    $"SPS/{name}", $"Sockets/{name}"
                }).Concat(possiblePaths).ToList();
            foreach (var vrcFury in furies)
            {
                vrcFury.config.features.RemoveAll(feature =>
                    feature is MoveMenuItem m && (m.fromPath == "SPS" || m.fromPath == "Sockets"));
                vrcFury.config.features.RemoveAll(feature =>
                    feature is MoveMenuItem m && possiblePaths.Contains(m.fromPath));
                vrcFury.config.features.RemoveAll(feature =>
                    feature is SetIcon i && (possibleIcons.Contains(i.path) || i.path == "SPS" || i.path == "Sockets"));
                if (vrcFury.config.features.Count == 0)
                {
                    Object.DestroyImmediate(vrcFury);
                }
            }
        }

        // TODO: Get Socket names from Marker Component to clear VRCFury component
        public static void Clear(GameObject avatarGameObject)
        {
            Clear2(avatarGameObject);
            Clear3(avatarGameObject);
            string[] socketNames =
            {
                Names.BlowjobName, $"{Names.HandjobName} Right", $"{Names.HandjobName} Left",
                $"Double {Names.HandjobName}", Names.PussyName,
                Names.AnalName, Names.TitjobName, Names.AssjobName, Names.ThighjobName, $"{Names.SoleName} Left",
                $"{Names.SoleName} Right",
                Names.FootjobName
            };
            var sockets = avatarGameObject.GetComponentsInChildren<VRCFuryHapticSocket>(true);

            foreach (var socket in sockets)
            {
                if (socket != null)
                {
                    if (socketNames.Contains(socket.name))
                    {
                        Transform parent = socket.transform.parent;
                        Object.DestroyImmediate(socket.gameObject);
                        if (parent.name == "SPS")
                        {
                            if (parent.childCount == 0)
                            {
                                Object.DestroyImmediate(parent.gameObject);
                            }
                        }
                    }
                }
            }

            var furies = avatarGameObject.GetComponents<VRCFury>();
            var possiblePaths = socketNames.SelectMany(name =>
                new[]
                {
                    $"SPS/{name}", $"Sockets/{name}"
                }).ToList();
            foreach (var vrcFury in furies)
            {
                vrcFury.config.features.RemoveAll(feature =>
                    feature is MoveMenuItem m && (m.fromPath == "SPS" || m.fromPath == "Sockets"));
                vrcFury.config.features.RemoveAll(feature =>
                    feature is MoveMenuItem m && possiblePaths.Contains(m.fromPath));
                if (vrcFury.config.features.Count == 0)
                {
                    Object.DestroyImmediate(vrcFury);
                }
            }
        }

        public static void Clear3(GameObject avatarGameObject)
        {
            string[] socketNames =
            {
                Names.BlowjobName, $"Handjob/{Names.HandjobName} Right", $"Handjob/{Names.HandjobName} Left",
                $"Handjob/Double {Names.HandjobName}", Names.PussyName,
                Names.AnalName, $"Special/{Names.TitjobName}", $"Special/{Names.AssjobName}",
                $"Special/{Names.ThighjobName}",
                $"Feet/{Names.SoleName} Left", $"Feet/{Names.SoleName} Right",
                $"Feet/{Names.FootjobName}"
            };
            var sockets = avatarGameObject.GetComponentsInChildren<VRCFuryHapticSocket>(true);

            foreach (var socket in sockets)
            {
                if (socket != null)
                {
                    if (socketNames.Contains(socket.name))
                    {
                        Transform parent = socket.transform.parent;
                        Object.DestroyImmediate(socket.gameObject);
                        if (parent.name == "SPS")
                        {
                            if (parent.childCount == 0)
                            {
                                Object.DestroyImmediate(parent.gameObject);
                            }
                        }
                    }
                }
            }

            var furies = avatarGameObject.GetComponents<VRCFury>();

            foreach (var vrcFury in furies)
            {
                var spsPaths = vrcFury.config.features.Where(feature => feature is SpsOptions)
                    .Select(feature => (feature as SpsOptions).menuPath).ToList();
                foreach (var spsPath in spsPaths)
                {
                    string[] possiblePaths =
                    {
                        $"{spsPath}/Handjob", $"{spsPath}/Special", $"{spsPath}/Feet",
                    };
                    vrcFury.config.features.RemoveAll(feature =>
                        feature is MoveMenuItem m && possiblePaths.Contains(m.fromPath));
                    vrcFury.config.features.RemoveAll(feature =>
                        feature is SetIcon i && possiblePaths.Contains(i.path));
                }

                vrcFury.config.features.RemoveAll(feature =>
                    feature is Toggle t && t.globalParam == "WH_SFX_On");

                vrcFury.config.features.RemoveAll(feature => feature is SpsOptions sps);
                if (vrcFury.config.features.Count == 0)
                {
                    Object.DestroyImmediate(vrcFury);
                }
            }
        }
    }
}