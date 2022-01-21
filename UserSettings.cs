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


namespace ClipPlayer
{
    [Serializable]
    public class UserSettings
    {
        #region Persisted editable properties
        [DisplayName("Auto Close")]
        [Description("Automatically close after playing the file.")]
        [Category("Navigator")]
        [Browsable(true)]
        public bool AutoClose { get; set; } = true;

        [DisplayName("Wave Output Device")]
        [Description("How to play the audio files.")]
        [Category("Audio")]
        [Browsable(true)]
        [TypeConverter(typeof(FixedListTypeConverter))]
        public string WavOutDevice { get; set; } = "Microsoft Sound Mapper";

        [DisplayName("Latency")]
        [Description("What's the hurry?")]
        [Category("Audio")]
        [Browsable(true)]
        [TypeConverter(typeof(FixedListTypeConverter))]
        public string Latency { get; set; } = "200";

        [DisplayName("Midi Output Device")]
        [Description("How to play the midi files.")]
        [Category("Midi")]
        [Browsable(true)]
        [TypeConverter(typeof(FixedListTypeConverter))]
        public string MidiOutDevice { get; set; } = "Microsoft GS Wavetable Synth";

        [DisplayName("Default Tempo")]
        [Description("Use this tempo if it's not in the file.")]
        [Category("Midi")]
        [Browsable(true)]
        public int DefaultTempo { get; set; } = 100;

        [DisplayName("Control Color")]
        [Description("Pick what you like.")]
        [Category("Cosmetics")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonColorConverter))]
        public Color ControlColor { get; set; } = Color.MediumOrchid;
        #endregion

        #region Persisted Non-editable Properties
        [Browsable(false)]
        [JsonConverter(typeof(JsonPointFConverter))]
        public PointF Position { get; set; } = new(50, 50);

        [Browsable(false)]
        public double Volume { get; set; } = 0.7;
        #endregion

        #region Fields
        /// <summary>The file name.</summary>
        string _fn = "???";
        #endregion

        #region Persistence
        /// <summary>Save object to file.</summary>
        public void Save()
        {
            if(File.Exists(_fn))
            {
                JsonSerializerOptions opts = new() { WriteIndented = true };
                string json = JsonSerializer.Serialize(this, opts);
                File.WriteAllText(_fn, json);
            }
        }

        /// <summary>Create object from file.</summary>
        public static void Load(string appDir)
        {
            string fn = Path.Combine(appDir, "settings.json");

            if (File.Exists(fn))
            {
                string json = File.ReadAllText(fn);
                UserSettings? set = JsonSerializer.Deserialize<UserSettings>(json);
                Common.Settings = set ?? new();
                Common.Settings._fn = fn;
            }
            else
            {
                // Doesn't exist, create a new one.
                Common.Settings = new()
                {
                    _fn = fn
                };
            }
        }
        #endregion
    }

    /// <summary>Converter for selecting property value from known lists.</summary>
    public class FixedListTypeConverter : TypeConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) { return true; }
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) { return true; }

        // Get the specific list based on the property name.
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            List<string>? rec = null;

            switch (context.PropertyDescriptor.Name)
            {
                case "Latency":
                    rec = new List<string>()
                    {
                        "25", "50", "100", "150", "200", "300", "400", "500"
                    };
                    break;

                case "WavOutDevice":
                    rec = new List<string>();
                    for (int id = -1; id < WaveOut.DeviceCount; id++) // –1 indicates the default output device, while 0 is the first output device
                    {
                        var cap = WaveOut.GetCapabilities(id);
                        rec.Add(cap.ProductName);
                    }
                    break;

                case "MidiOutDevice":
                    rec = new List<string>();
                    for (int devindex = 0; devindex < MidiOut.NumberOfDevices; devindex++)
                    {
                        rec.Add(MidiOut.DeviceInfo(devindex).ProductName);
                    }
                    break;
            }

            return new StandardValuesCollection(rec);
        }
    }
}
