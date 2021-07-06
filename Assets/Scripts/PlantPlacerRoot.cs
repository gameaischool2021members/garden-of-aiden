using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlantPlacerRoot : MonoBehaviour
{
#pragma warning disable 0414
    [SerializeField]
    private PlantPlacerModel model = null;
    public const string modelPropertyName = nameof(model);
#pragma warning restore 0414
}
