using Midi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KinectMusic
{
    public class Instrument 
    {
        // Midi output device
        private OutputDevice outputDevice;

        // Will the notes be held while the user's hand is still in
        private bool holdNote;

        // The number of notes the instrument is using
        private int numOfNotes;

        // Array for the scale's notes
        private Pitch[] scale;

        private Pitch[] performanceStartScale;

        private Pitch[] performanceStopScale;

        // An array holding info on whether a not is currently playing
        private bool[,] isNotePlayingArray = new bool[2, 10];
                
        // Indexes of the notes in the scale being played
        private int noteIndexLeft;
        private int noteIndexRight;

        // Gets the hold note property.
        public bool HoldNote
        {
            get { return this.holdNote; }
        }

        // Gets the hold note property.
        public int NumOfNotes
        {
            get { return this.numOfNotes; }
        }

        public Instrument(int numOfNotes, bool holdNote = false)
        {
            noteIndexLeft = -1;
            noteIndexRight = -1;

            for (int i = 0; i < isNotePlayingArray.Length/2; i++)
            {
                isNotePlayingArray[0, i] = false;
                isNotePlayingArray[1, i] = false;
            }

            this.holdNote = holdNote;
            this.numOfNotes = numOfNotes;
            this.scale = GetScale(numOfNotes);

            outputDevice = OutputDevice.InstalledDevices.Single(d => d.Name == "loopMIDIport1");

            this.outputDevice.Open();
            if (!outputDevice.IsOpen)
                throw new InvalidOperationException();
        }

        public Instrument(bool performanceMode)
        {
            outputDevice = OutputDevice.InstalledDevices.Single(d => d.Name == "loopMIDIport1");

            this.outputDevice.Open();
            if (!outputDevice.IsOpen)
                throw new InvalidOperationException();

            if (performanceMode)
            {
                this.performanceStartScale = GetPerformanceScaleStart();
                this.performanceStopScale = GetPerformanceScaleStop();
                this.holdNote = false;
            }

        }

        public void PlayNote(object sender, NoteEventArgs e)
        {
            char handId = e.GetHandId();
            int noteIndex = e.GetNoteIndex();
            if (handId == 'R')
            {
                noteIndexRight = noteIndex;
                new Thread(() => PlayRightHandNote(noteIndex)).Start();
                isNotePlayingArray[1, noteIndexRight] = true;
            }
            else if (handId == 'L')
            {
                noteIndexLeft = noteIndex;
                new Thread(() => PlayLeftHandNote(noteIndex)).Start();
                isNotePlayingArray[0, noteIndexLeft] = true;
            }            
        }

        public void StopNote(object sender, NoteEventArgs e)
        {
            char handId = e.GetHandId();
            int noteIndex = e.GetNoteIndex();
            if (handId == 'R')
            {
                isNotePlayingArray[1, noteIndex] = false;
                if (holdNote)
                    outputDevice.SendNoteOff(Channel.Channel1, scale[noteIndex], 100);

                noteIndexRight = -1;
            }
            else if (handId == 'L')
            {
                isNotePlayingArray[0, noteIndex] = false;
                if (holdNote)
                    outputDevice.SendNoteOff(Channel.Channel1, scale[noteIndex], 100);
                
                noteIndexLeft = -1;
            }           
        }

        public void PlayPerformanceNote(object sender, NoteEventArgs e)
        {
            char handId = e.GetHandId();
            int noteIndex = e.GetNoteIndex();
            if (handId == 'R')
            {
                noteIndexRight = noteIndex;
                new Thread(() => PlayRightHandPerformanceNote(noteIndex, true)).Start();
                isNotePlayingArray[1, noteIndexRight] = true;
            }
            else if (handId == 'L')
            {
                noteIndexLeft = noteIndex;
                new Thread(() => PlayLeftHandPerformanceNote(noteIndex, true)).Start();
                isNotePlayingArray[0, noteIndexLeft] = true;
            }
        }

        public void StopPerformanceNote(object sender, NoteEventArgs e)
        {
            char handId = e.GetHandId();
            int noteIndex = e.GetNoteIndex();
            if (handId == 'R')
            {
                isNotePlayingArray[1, noteIndex] = false;
                new Thread(() => PlayRightHandPerformanceNote(noteIndex, false)).Start();
                noteIndexRight = -1;
            }
            else if (handId == 'L')
            {
                isNotePlayingArray[0, noteIndex] = false;
                new Thread(() => PlayLeftHandPerformanceNote(noteIndex, false)).Start();
                noteIndexLeft = -1;
            }
        }

        public bool IsNotePlaying(int index, char hand)
        {
            if (hand == 'L')
                return isNotePlayingArray[0, index];
            else if (hand == 'R')
                return isNotePlayingArray[1, index];
            else
                throw new ArgumentException("Hand identifying char needs to be 'L' or 'R'");
        }    
    
        public void Close()
        {
            if (scale != null)
            {
                foreach (Pitch p in scale)
                    this.outputDevice.SendNoteOff(Channel.Channel1, p, 100);
            }
            this.outputDevice.Close();            
        }
        
        public void ChangeControl(object sender, ControlEventArgs e)
        {
            //if (e.GetControl() == null)

            Control control = e.GetControlAsControl();

            // Check if the value has been set as a Control enum
            if (control != 0)
            {
                int controlValue = e.GetControlValue();
                this.outputDevice.SendControlChange(Channel.Channel2, control, controlValue);
            }
            // if not, get the int that is assigned
            else
            {
                int controlNumber = e.GetControlAsNumber();
                int controlValue = e.GetControlValue();
                this.outputDevice.SendControlChange(Channel.Channel2, controlNumber, controlValue);
            }
        }
        
        private void PlayRightHandNote(int noteIndex)
        {
            outputDevice.SendNoteOn(Channel.Channel1, scale[noteIndex], 100);

            if (!holdNote)
            {
                Thread.Sleep(300);
                outputDevice.SendNoteOff(Channel.Channel1, scale[noteIndex], 100);
            }
        }

        private void PlayLeftHandNote(int noteIndex)
        {
            outputDevice.SendNoteOn(Channel.Channel1, scale[noteIndex], 100);
            
            if (!holdNote)
            {
                Thread.Sleep(300);
                outputDevice.SendNoteOff(Channel.Channel1, scale[noteIndex], 100);
            }
        }

        private void PlayRightHandPerformanceNote(int noteIndex, bool start)
        {
            if (start)
            {
                outputDevice.SendNoteOn(Channel.Channel1, performanceStartScale[noteIndex], 100);
                Thread.Sleep(300);
                outputDevice.SendNoteOff(Channel.Channel1, performanceStartScale[noteIndex], 100);
            }
            else
            {
                outputDevice.SendNoteOn(Channel.Channel1, performanceStopScale[noteIndex], 100);
                Thread.Sleep(300);
                outputDevice.SendNoteOff(Channel.Channel1, performanceStopScale[noteIndex], 100);
            }
        }

        private void PlayLeftHandPerformanceNote(int noteIndex, bool start)
        {
            if (start)
            {
                outputDevice.SendNoteOn(Channel.Channel1, performanceStartScale[noteIndex], 100);
                Thread.Sleep(300);
                outputDevice.SendNoteOff(Channel.Channel1, performanceStartScale[noteIndex], 100);
            }
            else
            {
                outputDevice.SendNoteOn(Channel.Channel1, performanceStopScale[noteIndex], 100);
                Thread.Sleep(300);
                outputDevice.SendNoteOff(Channel.Channel1, performanceStopScale[noteIndex], 100);
            }
        }
        
        private static Pitch[] GetScale(int numOfNotes)
        {
            // The array holding the notes
            Pitch[] pentatonicScale = new Pitch[10]
            { 
                Pitch.GSharp5, 
                Pitch.ASharp5, 
                Pitch.C6, 
                Pitch.DSharp6, 
                Pitch.F6,
                Pitch.GSharp6, 
                Pitch.ASharp6, 
                Pitch.C7, 
                Pitch.DSharp7, 
                Pitch.F7 
            };

            Pitch[] scale = new Pitch[numOfNotes];
            for (int i = 0; i < numOfNotes; i++)
            {
                scale[i] = pentatonicScale[i];
            }

            return scale;
        }
        
        private static Pitch[] GetPerformanceScaleStart()
        {
            // The array holding the notes
            return new Pitch[6]
            { 
                Pitch.C1, 
                Pitch.C2, 
                Pitch.C3, 
                Pitch.C4, 
                Pitch.C5,
                Pitch.C6
            };
        }

        private static Pitch[] GetPerformanceScaleStop()
        {
            // The array holding the notes
            return new Pitch[6]
            { 
                Pitch.B1, 
                Pitch.B2, 
                Pitch.B3, 
                Pitch.B4, 
                Pitch.B5,
                Pitch.B6
            };
        }
    }
}
