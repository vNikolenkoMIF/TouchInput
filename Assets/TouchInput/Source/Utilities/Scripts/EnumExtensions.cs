using System;

namespace TouchInput.Source.Utilities.Scripts
{
    public static class EnumExtensions
    {
        public static bool MoreThanOneFlag<TValue>(this TValue flag) where TValue : Enum
        {
            return (Convert.ToInt32(flag) & (Convert.ToInt32(flag) - 1)) != 0;
        }
    }
}
