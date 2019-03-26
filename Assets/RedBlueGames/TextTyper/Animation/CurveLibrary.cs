namespace RedBlueGames.Tools.TextTyper {
    using UnityEngine;
    using System;
    using System.Collections;
    using System.Collections.Generic;


    [Serializable]
    public class CurvePreset 
    {
        [Tooltip("Name identifying this preset. Can also be used as a CurveLibrary indexer key.")]
        public string Name;

        [Range( 0f, 0.5f )]
        public float timeOffsetPerChar;

        public AnimationCurve xPosCurve;
        [Range( 0, 20 )]
        public float xPosMultiplier;

        public AnimationCurve yPosCurve;
        [Range( 0, 20 )]
        public float yPosMultiplier;

        public AnimationCurve rotationCurve;
        [Range( 0, 90 )]
        public float rotationMultiplier;

        public AnimationCurve scaleCurve;
        [Range( 0, 100 )]
        public float scaleMultiplier;
    }


    [CreateAssetMenu(fileName = "CurveLibrary", menuName = "Text Typer/Curve Library", order = 1)]
    public class CurveLibrary : ScriptableObject 
    {
        public List<CurvePreset> CurvePresets;

        public CurvePreset this[string key]
        {
            get
            {
                foreach(CurvePreset preset in CurvePresets)
                {
                    if (preset.Name.ToUpper() == key.ToUpper()) 
                    {
                        return preset;
                    }
                }

                throw new KeyNotFoundException();
            }
        }
    }
}