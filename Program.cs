using NAudio.Midi;

class Program
{
    static void Main(string[] args)
    {
        string midiPath = args[0];
        string outputPath = args[1];

        // Load the MIDI file
        MidiFile midi = new MidiFile(midiPath, false);

        // Initialize lists for piano events and other instrument events
        List<MidiEvent> combinedEvents = [];

        // Extract MetaEvents (like tempo changes) and ensure they are applied to all tracks
        IEnumerable<MidiEvent> metaEvents = 
            from track in midi.Events 
            from midiEvent in track 
            where midiEvent.CommandCode == MidiCommandCode.MetaEvent 
            select midiEvent.Clone();

        // Add meta events to the combined events list first to ensure proper timing
        combinedEvents.AddRange(metaEvents);

        // Iterate through each track in the MIDI file
        for (int trackIndex = 0; trackIndex < midi.Events.Count(); trackIndex++)
        {
            IList<MidiEvent> trackEvents = midi.Events[trackIndex];

            // Check if the event is from the piano tracks
            bool isPianoTrack = false;
            if (trackEvents.FirstOrDefault() is TextEvent textEvent)
            {
                if (textEvent.Text.Contains("piano", StringComparison.InvariantCultureIgnoreCase))
                {
                    isPianoTrack = true;
                }
            }

            foreach (MidiEvent midiEvent in trackEvents)
            {
                // Ignore MetaEvents since they are already added
                if (midiEvent.CommandCode == MidiCommandCode.MetaEvent) continue;

                // Clone the event to avoid altering the original MIDI file's events
                MidiEvent clonedEvent = midiEvent.Clone();

                // Assign channel based on whether it's a piano track
                clonedEvent.Channel = isPianoTrack ? 1 : 2;

                // Add cloned event to the combined events list
                combinedEvents.Add(clonedEvent);
            }
        }

        // Sort all events by absolute time to maintain correct sequence
        combinedEvents.Sort((e1, e2) => e1.AbsoluteTime.CompareTo(e2.AbsoluteTime));

        // Create a new MIDI event collection for the combined events
        MidiEventCollection newMidiCollection = new MidiEventCollection(midi.FileFormat, midi.DeltaTicksPerQuarterNote);
        newMidiCollection.AddTrack(combinedEvents); // Add combined events as a single track

        // Save the new MIDI file
        MidiFile.Export(outputPath, newMidiCollection);
        Console.WriteLine("MIDI file saved to " + outputPath);
    }
}
