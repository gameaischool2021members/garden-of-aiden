using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlantPlacerRoot : MonoBehaviour
{
#pragma warning disable 0414
    [SerializeField]
    public PlantPlacerModel model = null;
    public const string modelPropertyName = nameof(model);
#pragma warning restore 0414

    [SerializeField]
    public float tileWidthInWorldUnits = 200f;

    [SerializeField]
    public int proximityGradientWidthInTexels = 25;

    [SerializeField]
    public int numberOfDioramaSamples = 25;
}
