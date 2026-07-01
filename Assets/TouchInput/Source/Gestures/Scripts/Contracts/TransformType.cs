using System;

namespace TouchInput.Source.Gestures.Scripts.Contracts
{
    [Flags]
    public enum TransformType
    {
        Nothing = 0,
        Translation = 1 << 1,
        Rotation = 1 << 2,
        Scaling = 1 << 3,
        Everything = Translation | Rotation | Scaling
    }
}