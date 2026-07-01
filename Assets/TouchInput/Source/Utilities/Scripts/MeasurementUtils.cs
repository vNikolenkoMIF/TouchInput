using UnityEngine;

public static class MeasurementUtils
{
    private const float INCHES_IN_CM = 2.54f;
    
    public static float CmToInches(float cm)
    {
        return cm / INCHES_IN_CM;
    }
}
