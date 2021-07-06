using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlantPlacerRoot : MonoBehaviour
{
    [SerializeField]
    private PlantPlacerModel model = null;
    public const string modelPropertyName = nameof(model);
}
