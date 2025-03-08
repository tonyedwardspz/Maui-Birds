// Basically taken from the Buddah project
// https://github.com/eidetic-av/Buddah/blob/master/Buddah/Midi/MidiInputDevice.cs


using Commons.Music.Midi;

namespace Maui_Birds.Midi;


public class MidiOutputDevice
{
    IMidiOutput MidiOutput;
    
    public MidiOutputDevice(IMidiOutput midiOutput)
    {
        MidiOutput = midiOutput;
    }

    public async void Send(byte[] midiData, int start, int length, long timestamp) {
        if (await MidiManager.EnsureOutputReady(MidiOutput.Details.Name))
            MidiOutput.Send(midiData, start, length, timestamp);
    }

    public async void Close() => await MidiOutput.CloseAsync();
}