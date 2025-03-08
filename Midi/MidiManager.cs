// Basically taken from the Buddah project
// https://github.com/eidetic-av/Buddah/blob/master/Buddah/Midi/MidiManager.cs

using Commons.Music.Midi;
using System.Diagnostics;

namespace Maui_Birds.Midi;

public static class MidiManager
{
    public static IMidiAccess AccessManager { get; private set; }

    public static List<IMidiPortDetails> AvailableInputDevices => AccessManager.Inputs.ToList();
    public static List<IMidiPortDetails> AvailableOutputDevices => AccessManager.Outputs.ToList();

    public static Dictionary<string, MidiInputDevice> ActiveInputDevices { get; private set; } = new Dictionary<string, MidiInputDevice>();
    public static Dictionary<string, MidiOutputDevice> ActiveOutputDevices { get; private set; } = new Dictionary<string, MidiOutputDevice>();

    static MidiManager()
    {
        AccessManager = MidiAccessManager.Default;
        Debug.WriteLine("Available MIDI Inputs ({0}): ", AvailableInputDevices.Count());
        AvailableInputDevices.ForEach(i => Debug.WriteLine("    " + i.Name));
        Debug.WriteLine("");
    }

    public static async Task<bool> OpenInput(string inputDeviceName)
    {
        var inputInfo = AvailableInputDevices.SingleOrDefault(i => i.Name.ToLower() == inputDeviceName.ToLower());
        if (inputInfo == default) return false;
        ActiveInputDevices[inputDeviceName] = new MidiInputDevice(await AccessManager.OpenInputAsync(inputInfo.Id));
        Debug.WriteLine($"Successfully opened input device {0}", inputDeviceName);
        return true;
    }

    public static async Task<bool> EnsureInputReady(string inputDeviceName)
    {
        if (ActiveInputDevices.ContainsKey(inputDeviceName)) return true;
        var opened = await OpenInput(inputDeviceName);
        if (!opened && ActiveInputDevices.ContainsKey(inputDeviceName))
            ActiveInputDevices.Remove(inputDeviceName);
        return opened;
    }

    public static async Task<bool> OpenOutput(string outputDeviceName)
    {
        var outputInfo = AvailableOutputDevices.SingleOrDefault(o => o.Name.ToLower() == outputDeviceName.ToLower());
        if (outputInfo == default) return false;
        ActiveOutputDevices[outputDeviceName] = new MidiOutputDevice(await AccessManager.OpenOutputAsync(outputInfo.Id));
        return true;
    }

    public static async Task<bool> EnsureOutputReady(string outputDeviceName)
    {
        if (ActiveOutputDevices.ContainsKey(outputDeviceName)) return true;
        var opened = await OpenOutput(outputDeviceName);
        if (!opened && ActiveOutputDevices.ContainsKey(outputDeviceName))
            ActiveOutputDevices.Remove(outputDeviceName);
        return opened;
    }

    public static void Close()
    {
        Debug.WriteLine("Closing Midi Devices");
        foreach (var input in ActiveInputDevices) input.Value.Close();
    }
}
