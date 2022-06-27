using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;
using System.Drawing;
using System.Text.Json;
using System.Text.Json.Serialization;
using NAudio.Wave;
using NAudio.Midi;
using NBagOfTricks;
using NBagOfUis;
using MidiLib;
using AudioLib;

namespace ClipPlayer
{
    [Serializable]
    public class UserSettings : Settings
    {
        #region Persisted editable properties
        [DisplayName("Auto Close")]
        [Description("Automatically close after playing the file.")]
        [Browsable(true)]
        public bool AutoClose { get; set; } = true;

        [DisplayName("Control Color")]
        [Description("Pick what you like.")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonColorConverter))]
        public Color ControlColor { get; set; } = Color.MediumOrchid;

        [DisplayName("Debug")]
        [Description("Do not press this!!!")]
        [Browsable(true)]
        public bool Debug { get; set; } = false;

        [DisplayName("Midi Settings")]
        [Description("Edit midi settings.")]
        [Browsable(true)]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public MidiSettings MidiSettings { get; set; } = new();

        [DisplayName("Audio Settings")]
        [Description("Edit audio settings.")]
        [Browsable(true)]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public AudioSettings AudioSettings { get; set; } = new();
        #endregion

        #region Persisted Non-editable Properties
        [Browsable(false)]
        public double Volume { get; set; } = 0.7;
        #endregion
    }
}
