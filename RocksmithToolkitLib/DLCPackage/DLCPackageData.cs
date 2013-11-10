﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using X360.STFS;
using System.Text.RegularExpressions;
using RocksmithToolkitLib.DLCPackage.Manifest.Tone;

namespace RocksmithToolkitLib.DLCPackage
{
    public class DLCPackageData
    {
        public GameVersion GameVersion;
        public string AppId { get; set; }
        public string Name { get; set; }
        public SongInfo SongInfo { get; set; }
        public string AlbumArtPath { get; set; }
        public string AlbumArt128 { get; set; } //RS2014 only
        public string AlbumArt64 { get; set; } //RS2014 only
        public string OggPath { get; set; }
        public string OggMACPath { get; set; } //RS2014 only
        public string OggXBox360Path { get; set; }
        public string OggPS3Path { get; set; }
        public IList<Arrangement> Arrangements { get; set; }
        public IList<Tone.Tone> Tones { get; set; }
        public IList<ToneRS2014> TonesRS2014 { get; set; } //RS2014 only
        public decimal Volume { get; set; }
        public PackageMagic SignatureType { get; set; }
        
        private List<XBox360License> xbox360Licenses = null;
        public List<XBox360License> XBox360Licenses
        {
            get
            {
                if (xbox360Licenses == null)
                {
                    xbox360Licenses = new List<XBox360License>();
                    return xbox360Licenses;
                }
                else
                    return xbox360Licenses;
            }
            set { xbox360Licenses = value; }
        }
    }
}
