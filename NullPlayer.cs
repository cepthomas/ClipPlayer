using System;


namespace ClipPlayer
{
    /// <summary>Default player that doesn't do anything.</summary>
    public class NullPlayer : IPlayer
    {
        public RunState State { get; set; } = RunState.Stopped;

        public TimeSpan Length { get; } = new TimeSpan();

        public double Volume { get; set; }

        public bool Valid { get { return false; } }

        public TimeSpan Current { get; set; } = new TimeSpan();

        public event EventHandler<StatusChangeEventArgs>? StatusChange;

        public bool OpenFile(string fn) { return true; }

        public string GetInfo() { return "Big dummy"; }

        public NullPlayer()
        {
            StatusChange?.Invoke(this, new() { Progress = 0 });
        }

        public RunState Play()
        {
            State = RunState.Playing;
            return State;
        }

        public RunState Stop()
        {
            State = RunState.Stopped;
            return State;
        }

        public void Rewind() { }

        public void UpdateSettings() { }

        public void Dispose() { }
    }
}
