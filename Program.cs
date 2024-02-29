using NAudio.Midi;

class Program
{
    static void Main(string[] args)
    {
        string midiPath = args[0];
        string outputPath = args[1];

        // Load the MIDI file
        MidiFile midi = new MidiFile(midiPath, false);
        
        List<MidiEvent> combinedEvents = CloneAndCombine(midi);
        
        combinedEvents = FilterOverlapping(combinedEvents, midi);

        // Sort all events by absolute time to maintain correct sequence
        combinedEvents.Sort((e1, e2) => e1.AbsoluteTime.CompareTo(e2.AbsoluteTime));

        // Create a new MIDI event collection for the combined events
        MidiEventCollection newMidiCollection = new MidiEventCollection(midi.FileFormat, midi.DeltaTicksPerQuarterNote);
        newMidiCollection.AddTrack(combinedEvents); // Add combined events as a single track

        // Save the new MIDI file
        MidiFile.Export(outputPath, newMidiCollection);
        Console.WriteLine("MIDI file saved to " + outputPath);
        
        Environment.Exit(0);
    }

    static List<MidiEvent> CloneAndCombine(MidiFile midi)
    {
        List<MidiEvent> combinedEvents = [];
        
        // Iterate through each track in the MIDI file
        for (int trackIndex = 0; trackIndex < midi.Events.Count(); trackIndex++)
        {
            IList<MidiEvent> trackEvents = midi.Events[trackIndex];

            foreach (MidiEvent midiEvent in trackEvents)
            {
                try
                {
                    // Clone the event to avoid altering the original MIDI file's events
                    MidiEvent clonedEvent = midiEvent.Clone();

                    // Add cloned event to the combined events list
                    combinedEvents.Add(clonedEvent);
                }
                catch (NullReferenceException _)
                {
                    Console.WriteLine(
                        "Null value for MIDI end time. This means that there was an error exporting from MuseScore.");
                    Console.WriteLine("Open the midi file in MidiEditor.exe and save it again to fix this issue.");
                    Console.WriteLine("Exiting...");
                    Environment.Exit(1);
                }
            }
        }

        return combinedEvents;
    }

    static List<MidiEvent> FilterOverlapping(List<MidiEvent> combinedEvents, MidiFile midi)
    {
        // Remove overlapping instrumental events that conflict with piano events
        const int threshold = 5; // Milliseconds, adjust this value as needed
        Dictionary<int, List<MidiEvent>> noteStartTimes = new Dictionary<int, List<MidiEvent>>();

        // First, build a dictionary for piano events to quickly check for overlaps
        foreach (MidiEvent evt in combinedEvents.Where(e => e.Channel == 1)) // Assuming channel 1 is for piano
        {
            if (evt is NoteOnEvent noteOnEvent &&
                noteOnEvent.Velocity > 0) // Only consider NoteOn with velocity > 0 (actual note starts)
            {
                int noteKey = noteOnEvent.NoteNumber;
                if (!noteStartTimes.ContainsKey(noteKey))
                {
                    noteStartTimes[noteKey] = [];
                }

                noteStartTimes[noteKey].Add(evt);
            }
        }

        // Filter out overlapping instrumental events
        List<MidiEvent> filteredEvents = [];
        foreach (MidiEvent evt in combinedEvents)
        {
            if (evt.Channel == 2) // Assuming channel 2 is for other instruments
            {
                if (evt is NoteOnEvent noteOn && noteOn.Velocity > 0)
                {
                    bool isOverlapping = false;
                    if (noteStartTimes.TryGetValue(noteOn.NoteNumber, out List<MidiEvent> pianoEvents))
                    {
                        // Check if there's an overlapping piano event within the threshold
                        foreach (MidiEvent pianoEvent in pianoEvents)
                        {
                            if (Math.Abs(pianoEvent.AbsoluteTime - evt.AbsoluteTime) * midi.DeltaTicksPerQuarterNote <
                                threshold)
                            {
                                isOverlapping = true;
                                break;
                            }
                        }
                    }

                    if (!isOverlapping)
                    {
                        filteredEvents.Add(evt);
                    }
                }
                else
                {
                    // Non-note events are added without filtering
                    filteredEvents.Add(evt);
                }
            }
            else
            {
                // All piano events are added without filtering
                filteredEvents.Add(evt);
            }
        }

        // Replace combinedEvents with filteredEvents for final sorting and MIDI file creation
        return filteredEvents;
    }
}