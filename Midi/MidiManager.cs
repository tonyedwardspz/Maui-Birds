using Foundation;
using NewBindingMaciOS;
using System.Diagnostics;
using System.Collections.Generic;

public static class MidiManager
{
    private static MIDILightController? _midiController;
    private static Action<byte, byte, byte>? _midiCallback;

    public static void SetMIDICallback(Action<byte, byte, byte> callback)
    {
        _midiCallback = callback;
    }

    public static async Task InitializeAsync()
    {
        NSError? error;
        _midiController = new MIDILightController();
        Debug.WriteLine("Initializing MIDI controller...");
        var initialized = _midiController.InitializeAndReturnError(out error);
        Debug.WriteLine($"MIDI controller initialized: {initialized}, Error: {error?.LocalizedDescription ?? "none"}");
        
        // Setup MIDI input callback
        Debug.WriteLine("Setting up MIDI callback...");
        _midiController.SetMIDIReceiveCallback((byte status, byte data1, byte data2) =>
        {
            try
            {
                Debug.WriteLine("=== MIDI Callback Triggered ===");
                Debug.WriteLine($"Raw MIDI Message - Status: {status:X2}, Data1: {data1:X2}, Data2: {data2:X2}");
                
                _midiCallback?.Invoke(status, data1, data2);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in MIDI callback: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        });

        // Connect to the first available MIDI source
        var sources = _midiController.GetAvailableSourcesWithContaining("");
        Debug.WriteLine($"Available MIDI sources: {sources.Length}");
        foreach (var source in sources)
        {
            Debug.WriteLine($"Found MIDI source: {source}");
        }

        if (sources.Length > 0)
        {
            Debug.WriteLine("Attempting to connect to first MIDI source...");
            var connected = _midiController.ConnectSourceAt(0, out error);
            Debug.WriteLine($"Connection result: {connected}, Error: {error?.LocalizedDescription ?? "none"}");
        }
        else
        {
            Debug.WriteLine("No MIDI sources found!");
        }
    }

    public static void SetupLights()
    {
        if (_midiController == null) return;

        NSError? error;
        var output = _midiController.GetAvailableDevicesWithContaining("Does not matter")[0];
        _midiController.ConnectTo(0, out error);
        
        // switch off all lights by looping over 0 to 39 in hex
        for (int i = 0; i < 40; i++)
        {
            _midiController.TurnLightOnChannel((byte)0, (byte)i, (byte)0x00, out error);
        }
    }

    public static void SetupGameLights(Dictionary<byte, byte> lightConfig)
    {
        if (_midiController == null) return;

        NSError? error;
        foreach (var light in lightConfig)
        {
            _midiController.TurnLightOnChannel((byte)0, light.Key, light.Value, out error);
        }
    }

    public static void TurnOffSelectedLights(Dictionary<byte, byte> lightsToTurnOff)
    {
        if (_midiController == null) return;

        NSError? error;
        foreach (var light in lightsToTurnOff)
        {
            _midiController.TurnLightOnChannel((byte)0, light.Key, (byte)0x00, out error);
        }
    }

    public static void Cleanup()
    {
        if (_midiController != null)
        {
            // Clear the MIDI callback
            _midiController.SetMIDIReceiveCallback((_, _, _) => { });
            _midiController.Dispose();
            _midiController = null;
        }
    }
}
