using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;
using System.Drawing;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfUis;
using Ephemera.MidiLib;
using Ephemera.AudioLib;
using System.Drawing.Design;


namespace ClipPlayer
{
    [Serializable]
    public sealed class UserSettings : SettingsCore
    {
        #region Persisted editable properties
        [DisplayName("Auto Close")]
        [Description("Automatically close after playing the file.")]
        [Browsable(true)]
        public bool AutoClose { get; set; } = true;

        [DisplayName("Control Color")]
        [Description("The color used for active control surfaces.")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonColorConverter))]
        public Color DrawColor { get; set; } = Color.MediumOrchid;

        [DisplayName("Selection Color")]
        [Description("The color used for selections.")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonColorConverter))]
        public Color SelectedColor { get; set; } = Color.LightYellow;

        [DisplayName("Debug")]
        [Description("Do not press this!!!")]
        [Browsable(false)] // Hide for now.
        public bool Debug { get; set; } = false;

        [DisplayName("File Log Level")]
        [Description("Log level for file write.")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public LogLevel FileLogLevel { get; set; } = LogLevel.Trace;

        [DisplayName("File Log Level")]
        [Description("Log level for UI notification.")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public LogLevel NotifLogLevel { get; set; } = LogLevel.Debug;

        [DisplayName("Midi Device")]
        [Description("Midi Device.")]
        [Browsable(true)]
        [Editor(typeof(GenericListTypeEditor), typeof(UITypeEditor))]
        public string MidiDeviceName { get; set; } = "";

        [DisplayName("Wave Output Device")]
        [Description("How to play the audio files.")]
        [Browsable(true)]
        [TypeConverter(typeof(AudioSettingsConverter))]
        public string WavOutDevice { get; set; } = "Microsoft Sound Mapper";

        [DisplayName("Latency")]
        [Description("What's the hurry?")]
        [Browsable(true)]
        [TypeConverter(typeof(AudioSettingsConverter))]
        public string Latency { get; set; } = "200";
        #endregion

        #region Persisted Non-editable Properties
        [Browsable(false)]
        public double Volume { get; set; } = 0.7;
        #endregion
    }
}
