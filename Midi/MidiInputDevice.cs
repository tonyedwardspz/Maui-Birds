// // Basically taken from the Buddah project
// // https://github.com/eidetic-av/Buddah/blob/master/Buddah/Midi/MidiInputDevice.cs
//
// using System.Diagnostics;
// using Commons.Music.Midi;
//
// namespace Maui_Birds.Midi;
//
// public class MidiInputDevice
// {
//     IMidiInput MidiInput;
//
//     public Action<MidiReceivedEventArgs> MessageReceived;
//     public Action<int, int> NoteOn;
//     public Action<int> NoteOff;
//     public Action<int, int> ControlChange;
//
//     public MidiInputDevice(IMidiInput midiInput)
//     {
//         MidiInput = midiInput;
//
//         // Initialise these higher-level actions with empty callbacks
//         MessageReceived = (MidiReceivedEventArgs e) => { };
//         NoteOn = (int noteNumber, int velocity) => { };
//         NoteOff = (int noteNumber) => { };
//         ControlChange = (int ccNumber, int value) => { };
//
//         // And add the message routing method to the base callback
//         MidiInput.MessageReceived += ProcessMessageReceived;
//     }
//
//     public async void Close() => await MidiInput.CloseAsync();
//
//     void ProcessMessageReceived(object sender, MidiReceivedEventArgs e)
//     {
//         var messageType = (MessageType)(e.Data[0] >> 4);
//
//         // managed-midi breaks on clock message through Port.Send
//         if (messageType != MessageType.Clock)
//             MessageReceived.Invoke(e);
//
//         // Extract the information from the MIDI byte array,
//         // and invoke the respective callbacks
//         switch (messageType)
//         {
//             case MessageType.NoteOn:
//                 {
//                     var noteNumber = (int)e.Data[1];
//                     // Debug.WriteLine($"Note On: {noteNumber}");
//                     var velocity = (int)e.Data[2];
//                     if (velocity != 0) NoteOn.Invoke(noteNumber, velocity);
//                     else NoteOff.Invoke(noteNumber);
//                     break;
//                 }
//             case MessageType.NoteOff:
//                 {
//                     var noteNumber = (int)e.Data[1];
//                     // Debug.WriteLine($"Note Off: {noteNumber}");
//                     NoteOff.Invoke(noteNumber);
//                     break;
//                 }
//             case MessageType.ControlChange:
//                 {
//                     var ccNumber = (int)e.Data[1];
//                     var value = (int)e.Data[2];
//                     ControlChange.Invoke(ccNumber, value);
//                     break;
//                 }
//             case MessageType.Clock:
//                 {
//                     CalculateMidiClock(e.Timestamp);
//                     break;
//                 }
//             default: break;
//         }
//     }
//
//     public float BPM { get; private set; }
//     Queue<long> MidiClockTimestamps = new Queue<long>();
//     void CalculateMidiClock(long timestamp)
//     {
//         MidiClockTimestamps.Enqueue(timestamp);
//         if (MidiClockTimestamps.Count == 25)
//         {
//             var earliestTime = MidiClockTimestamps.Dequeue();
//             BPM = 60000f / (timestamp - earliestTime);
//         }
//     }
//
//     enum MessageType
//     {
//         NoteOn = 9, NoteOff = 8, ControlChange = 11, Clock = 15
//     }
// }
