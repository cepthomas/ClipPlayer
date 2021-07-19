using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Design;
using System.ComponentModel.Design;
using System.Windows.Forms.Design;
using Newtonsoft.Json;
using NAudio.Wave;
using NAudio.Midi;


namespace ClipPlayer
{
    [Serializable]
    public class UserSettings
    {
        #region Persisted editable properties
        // [DisplayName("Root Directories")]
        // [Description("Where to look in order as they appear.")]
        // [Category("Navigator")]
        // [Browsable(true)]
        // [Editor(typeof(ListEditor), typeof(UITypeEditor))]
        // public List<string> RootDirs { get; set; } = new List<string>();

// AutoClose:true
        [DisplayName("Auto Close")]
        [Description("Automatically close after playing the file.")]
        [Category("Navigator")]
        [Browsable(true)]
        public bool AutoClose { get; set; } = true;

// WavOutDevice:"Microsoft Sound Mapper"
        [DisplayName("Wave Output Device")]
        [Description("How to play the audio files.")]
        [Category("Audio")]
        [Browsable(true)]
        [TypeConverter(typeof(FixedListTypeConverter))]
        public string WavOutDevice { get; set; } = "";

// Latency:200
        [DisplayName("Latency")]
        [Description("What's the hurry?")]
        [Category("Audio")]
        [Browsable(true)]
        [TypeConverter(typeof(FixedListTypeConverter))]
        public string Latency { get; set; } = "200";

// MidiOutDevice:"Microsoft GS Wavetable Synth"
        [DisplayName("Midi Output Device")]
        [Description("How to play the midi files.")]
        [Category("Midi")]
        [Browsable(true)]
        [TypeConverter(typeof(FixedListTypeConverter))]
        public string MidiOutDevice { get; set; } = "";


// Tempo:105                                  "tempo/bpm if not in file",
        [DisplayName("Default Tempo")]
        [Description("Use this tempo if it's not in the file.")]
        [Category("Midi")]
        [Browsable(true)]
        public int DefaultTempo { get; set; } = 100;


// DrumChannel:1
        [DisplayName("Drum Channel")]
        [Description("Some files have drums on other than channel 10 so map those from that channel.")]
        [Category("Midi")]
        [Browsable(true)]
        public int DrumChannel { get; set; } = 1;

        // [DisplayName("Enable Mapping Drum Channel")]
        // [Description("Turn Drum Channel option on or off.")]
        // [Category("Midi")]
        // [Browsable(true)]
        // public bool MapDrumChannel { get; set; } = false;

        // [DisplayName("Log Midi Events")]
        // [Description("Write to text viewer. Warning! could be very busy.")]
        // [Category("Midi")]
        // [Browsable(true)]
        // public bool LogEvents { get; set; } = false;

        // [DisplayName("Snap To Grid")]
        // [Description("Snap to bar | beat | tick.")]
        // [Category("Midi")]
        // [Browsable(true)]
        // public BarBar.SnapType Snap { get; set; } = BarBar.SnapType.Bar;

        // [DisplayName("Slider Color")]
        // [Description("Pick what you like.")]
        // [Category("Cosmetics")]
        // [Browsable(true)]
        // public Color SliderColor { get; set; } = Color.MediumOrchid;

        // [DisplayName("Meter Color")]
        // [Description("Pick what you like.")]
        // [Category("Cosmetics")]
        // [Browsable(true)]
        // public Color MeterColor { get; set; } = Color.LimeGreen;

        // [DisplayName("Bar Color")]
        // [Description("Pick what you like.")]
        // [Category("Cosmetics")]
        // [Browsable(true)]
        // public Color BarColor { get; set; } = Color.Aqua;
        #endregion

        #region Persisted Non-editable Properties
// Pos:30,30
        [Browsable(false)]
        public Point Position { get; set; } = new Point(50, 50);

// Volume:0.9
        [Browsable(false)]
        public double Volume { get; set; } = 0.7;

        // [Browsable(false)]
        // public List<string> RecentFiles { get; set; } = new List<string>();

        // [Browsable(false)]
        // public Dictionary<string, bool> AllTags { get; set; } = new Dictionary<string, bool>();

        // [Browsable(false)]
        // public Dictionary<string, string> TaggedPaths { get; set; } = new Dictionary<string, string>();
        #endregion

        #region Fields
        /// <summary>The file name.</summary>
        string _fn = "???";
        #endregion

        #region Persistence
        /// <summary>Save object to file.</summary>
        public void Save()
        {
            string json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(_fn, json);
        }

        /// <summary>Create object from file.</summary>
        public static void Load(string appDir)
        {
            Common.Settings = null;
            string fn = Path.Combine(appDir, "settings.json");

            if (File.Exists(fn))
            {
                string json = File.ReadAllText(fn);
                Common.Settings = JsonConvert.DeserializeObject<UserSettings>(json);

                Common.Settings._fn = fn;
            }
            else
            {
                // Doesn't exist, create a new one.
                Common.Settings = new UserSettings
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
            List<string> rec = null;

            switch (context.PropertyDescriptor.Name)
            {
                case "Latency":
                    rec = new List<string>() { "25", "50", "100", "150", "200", "300", "400", "500" };
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
