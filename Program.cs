using NAudio.Midi;

class Program
{
    static void Main(string[] args)
    {
        string midiPath = args[0];
        string outputPath = args[1];

        // Load the MIDI file
        MidiFile midi = new MidiFile(midiPath, false);

        // Create a new track for combining all tracks from the original MIDI
        List<MidiEvent> combinedTrack = [];

        // Iterate through each track in the MIDI file
        foreach (IList<MidiEvent> trackEvents in midi.Events)
        {
            // Iterate through each event in the track
            foreach (MidiEvent midiEvent in trackEvents)
            {
                // Clone the event to avoid altering the original MIDI file's events
                MidiEvent clonedEvent = midiEvent.Clone();
                clonedEvent.Channel = 1;
                combinedTrack.Add(clonedEvent);
            }
        }

        // Sort the combined events by time
        combinedTrack.Sort((e1, e2) => e1.AbsoluteTime.CompareTo(e2.AbsoluteTime));

        // Create a new MIDI file with one track
        MidiEventCollection singleTrackMidi = new MidiEventCollection(midi.FileFormat, midi.DeltaTicksPerQuarterNote);
        singleTrackMidi.AddTrack(combinedTrack);

        // Save the new MIDI file
        MidiFile.Export(outputPath, singleTrackMidi);
        Console.WriteLine("MIDI file saved to " + outputPath);
    }
}