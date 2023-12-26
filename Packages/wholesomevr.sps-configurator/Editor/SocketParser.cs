using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using UnityEngine;
using VF.Component;
using VF.Model;
using VF.Model.Feature;
using VRC.SDK3.Avatars.Components;

namespace Wholesome
{
    
    
    public class SocketParser
    {
        private GameObject avatarObject;
        private SPSConfigurator.AvatarArmature avatarArmature;

        public SocketParser(GameObject avatarObject)
        {
            if (avatarObject.GetComponent<VRCAvatarDescriptor>() == null)
                throw new ArgumentException("avatarObject doesn't have a VRC Avatar Descriptor");
            this.avatarObject = avatarObject;
            avatarArmature = new SPSConfigurator.AvatarArmature(avatarObject);
        }
        
        public class Result
        {
            public Dictionary<string, VRCFuryHapticSocket> Sockets;
            public string SpsPath;
            public Dictionary<string, GuidTexture2d> CategoryIcons;
            public bool NewVersion;

            public bool HasExistingSockets => Sockets.Values.Any(socket => socket != null);
        }

        public Result Parse()
        {
            return new Result
            {
                Sockets = ParseSockets(),
                SpsPath = ParseSpsPath(),
                CategoryIcons = IsNewVersion() ? ParseCategoryIconsOnSpsObject() : ParseCategoryIconsOnAvatar(),
                NewVersion = IsNewVersion()
            };
        }
        
        public Dictionary<string, VRCFuryHapticSocket> ParseSockets()
        {
            var sockets = avatarObject.GetComponentsInChildren<VRCFuryHapticSocket>(true);
            var filteredSockets = sockets.Where(socket =>
                    Names.All.Contains(socket.gameObject.name) && (socket.transform.parent.name == "SPS" || socket.transform.parent.parent.name == "SPS"))
                .ToDictionary(socket => socket.gameObject.name);

            // Delete SFX

            // do it in Cleaner
            /*
            foreach (var socket in filteredSockets.Values)
            {
                socket.transform.SetParent(null, false);
            }*/

            return filteredSockets;
        }

        public string ParseSpsPath()
        {
            var furies = avatarObject.GetComponents<VRCFury>();
            
            var spsOptions = furies.SelectMany(fury => fury.config.features.OfType<SpsOptions>()).FirstOrDefault();
            if (spsOptions != null) return spsOptions.menuPath;
            
            var move = furies
                .SelectMany(fury => fury.config.features.OfType<MoveMenuItem>())
                .FirstOrDefault(m => m.fromPath == "SPS");
            if (move != null) return move.toPath;
            
            return null;
        }

        /*public Dictionary<string, GuidTexture2d> ParseCategoryIcons()
        {
            //Parse
        }*/

        public bool IsNewVersion()
        {
            var sockets = avatarObject.GetComponentsInChildren<VRCFuryHapticSocket>(true);
            var sps = avatarObject.transform.Find("SPS");
            return (sps != null);
        }

        private Dictionary<string, GuidTexture2d> ParseCategoryIconsOnAvatar()
        {
            var catIcons = new Dictionary<string, GuidTexture2d>();
            var furies = avatarObject.GetComponents<VRCFury>();
            var spsOptions = furies.SelectMany(fury => fury.config.features.OfType<SpsOptions>()).FirstOrDefault();
            var spsPath = spsOptions?.menuPath ?? "SPS";
            var icons = furies.SelectMany(fury => fury.config.features.OfType<SetIcon>()).ToList();
            var hjIcon = icons.FirstOrDefault(icon => icon.path == $"{spsPath}/Handjob");
            if(hjIcon != null) catIcons.Add("Handjob", hjIcon.icon);
            var specialIcon = icons.FirstOrDefault(icon => icon.path == $"{spsPath}/Special");
            if(specialIcon != null) catIcons.Add("Special", specialIcon.icon);
            var feetIcon = icons.FirstOrDefault(icon => icon.path == $"{spsPath}/Feet");
            if(feetIcon != null) catIcons.Add("Feet", feetIcon.icon);
            return catIcons;
        }
        
        private Dictionary<string, GuidTexture2d> ParseCategoryIconsOnSpsObject()
        {
            var catIcons = new Dictionary<string, GuidTexture2d>();
            var sps = avatarObject.transform.Find("SPS");
            if (sps == null) return catIcons;
            var hj = GetSetIcon(sps.gameObject, "Handjob");
            if (hj != null) catIcons.Add("Handjob", hj);
            var special = GetSetIcon(sps.gameObject, "Special");
            if (special != null) catIcons.Add("Special", special);
            var feet = GetSetIcon(sps.gameObject, "Feet");
            if (feet != null) catIcons.Add("Feet", hj);
            return catIcons;
        }

        private GuidTexture2d GetSetIcon(GameObject spsObject, string category)
        {
            var cat = spsObject.transform.Find(category);
            if (cat == null) return null;
            var furies = cat.GetComponents<VRCFury>();
            var icons = furies.SelectMany(fury => fury.config.features.OfType<SetIcon>()).ToList();
            var icon = icons.FirstOrDefault(ic => ic.path.EndsWith(category));
            return icon?.icon;
        }

        private string GetSpsPath()
        {
            var furies = avatarObject.GetComponents<VRCFury>();
            var spsOptions = furies.SelectMany(fury => fury.config.features.OfType<SpsOptions>()).FirstOrDefault();
            var spsPath = spsOptions?.menuPath ?? "SPS";
            return spsPath;
        }
    }
}