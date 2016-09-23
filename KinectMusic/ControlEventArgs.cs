using Midi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KinectMusic
{
    public class ControlEventArgs : EventArgs
    {
        private int controlValue;
        private int controlNumber;
        private Control control;

        public ControlEventArgs(Control controlId, int controlValue)
        {
            this.control = controlId;
            this.controlValue = controlValue;
        }
        
        public ControlEventArgs(int controlId, int controlValue)
        {
            this.controlNumber = controlId;
            this.controlValue = controlValue;
        }

        public Control GetControlAsControl()
        {
            return this.control;
        }

        public int GetControlAsNumber()
        {
            return this.controlNumber;
        }

        public int GetControlValue()
        {
            return this.controlValue;
        }
    }
}
