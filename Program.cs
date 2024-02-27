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

        // Keep track of the last instrument set for each channel
        Dictionary<int, int> channelInstruments = new Dictionary<int, int>();

        // Default to piano (program number 0) for all channels initially
        for (int i = 0; i < 16; i++) // MIDI channels are 0-15
        {
            channelInstruments[i] = 0; // Assuming 0 is the program number for Acoustic Grand Piano
        }

        // Iterate through each track in the MIDI file
        foreach (IList<MidiEvent> trackEvents in midi.Events)
        {
            // Iterate through each event in the track
            foreach (MidiEvent midiEvent in trackEvents)
            {
                // Handle program change events
                if (midiEvent is PatchChangeEvent pce)
                {
                    channelInstruments[pce.Channel] = pce.Patch;
                }

                // Clone the event to avoid altering the original MIDI file's events
                MidiEvent clonedEvent = midiEvent.Clone();

                // Check if this is a NoteOn or NoteOff event, then assign the channel based on the instrument
                if (clonedEvent.CommandCode is MidiCommandCode.NoteOn or MidiCommandCode.NoteOff)
                {
                    // If the last instrument set for this channel is a piano, assign to channel 1, else assign to channel 2
                    clonedEvent.Channel = channelInstruments[clonedEvent.Channel] == 0 ? 1 : 2;
                }

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
