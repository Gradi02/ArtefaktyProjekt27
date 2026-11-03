using System;

namespace ANTK.Game.Common
{
    [Serializable]
    public readonly struct Energy
    {
        public readonly int value;

        public Energy(int value)
        {
            this.value = Math.Max(0, value);
        }

        #region Arithmetics
        
        public static Energy operator +(Energy a, Energy b) 
            => new Energy(a.value + b.value);
        
        public static Energy operator -(Energy a, Energy b) 
            => new Energy(a.value - b.value);
        
        public static Energy operator *(Energy a, Energy b) 
            => new Energy(a.value * b.value);
        
        public static Energy operator /(Energy a, Energy b) 
            => new Energy(a.value / b.value);
        
        #endregion

        #region Comparison

        public static bool operator >=(Energy a, Energy b)
            => a.value >= b.value;
        
        public static bool operator <=(Energy a, Energy b)
            => a.value >= b.value;
        
        public static bool operator >(Energy a, Energy b)
            => a.value >= b.value;
        
        public static bool operator <(Energy a, Energy b)
            => a.value >= b.value;

        #endregion

        #region Conversion
        
        public static implicit operator int(Energy a)
            => a.value;

        public static explicit operator Energy(int a)
            => new Energy(a);

        #endregion
    }
}