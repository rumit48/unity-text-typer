namespace RedBlueGames.Tools.TextTyper {
    using System.Collections;
    using System.Collections.Generic;
    using TMPro;
    using UnityEngine;
    using UnityEngine.Events;
    using UnityEngine.UI;

    [RequireComponent(typeof(TextMeshProUGUI))]
    public class ShakeAnimation : TextAnimation 
    {
        [SerializeField]
        [Tooltip("The library of ShakePresets that can be used by this component.")]
        private ShakeLibrary shakeLibrary;

        [SerializeField]
        [Tooltip("The name (key) of the shake preset this animation should use")]
        private string shakePresetKey;

        private ShakePreset shakePreset;

        public void LoadPreset(ShakeLibrary library, string presetKey) 
        {
            this.shakeLibrary = library;
            this.shakePresetKey = presetKey;
            this.shakePreset = library[presetKey];
        }

        protected override void OnEnable() 
        {
            LoadPreset(shakeLibrary, shakePresetKey);
            base.OnEnable( );
        }

        protected override void Animate(int characterIndex, out Vector2 translation, out float rotation, out float scale) 
        {
            translation = Vector2.zero;
            rotation = 0f;
            scale = 1f;

            if (this.shakePreset == null) 
            {
                return;
            }

            if (characterIndex >= this.firstCharToAnimate && characterIndex <= this.lastCharToAnimate) 
            {
                float randomX = Random.Range(-this.shakePreset.xPosStrength, this.shakePreset.xPosStrength);
                float randomY = Random.Range(-this.shakePreset.yPosStrength, this.shakePreset.yPosStrength);
                translation = new Vector2(randomX, randomY);

                rotation = Random.Range(-this.shakePreset.RotationStrength, this.shakePreset.RotationStrength);

                scale = 1f + Random.Range(-this.shakePreset.ScaleStrength, this.shakePreset.ScaleStrength);
            }
        }
    }
}
