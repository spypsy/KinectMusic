using Midi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectMusic
{
    public class NoteEventArgs : EventArgs
    {
        private int noteIndex;
        private char handId;
        public NoteEventArgs(int index, char hand)
        {
            noteIndex = index;
            handId = hand;
        }

        public int GetNoteIndex()
        {
            return noteIndex;
        }

        public char GetHandId()
        {
            return handId;
        }

    }
}