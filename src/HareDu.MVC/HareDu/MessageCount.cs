using System;

namespace HareDu
{
    public class MessageCount
    {
        private decimal _count;

        public string Symbol { get; set; }
        
        public decimal DayOpen { get; private set; }
        
        public decimal DayLow { get; private set; }
        
        public decimal DayHigh { get; private set; }

        public decimal LastChange { get; private set; }

        public decimal Change
        {
            get
            {
                return Count - DayOpen;
            }
        }

        public double PercentChange
        {
            get
            {
                return (double)Math.Round(Change / Count, 4);
            }
        }

        public decimal Count
        {
            get
            {
                return _count;
            }
            set
            {
                if (_count == value)
                {
                    return;
                }

                LastChange = value - _count;
                _count = value;
                
                if (DayOpen == 0)
                {
                    DayOpen = _count;
                }
                if (_count < DayLow || DayLow == 0)
                {
                    DayLow = _count;
                }
                if (_count > DayHigh)
                {
                    DayHigh = _count;
                }
            }
        }
    }
}
