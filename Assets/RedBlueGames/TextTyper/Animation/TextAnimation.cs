namespace RedBlueGames.Tools.TextTyper {
    using System.Collections;
    using System.Collections.Generic;
    using TMPro;
    using UnityEngine;
    using UnityEngine.Events;
    using UnityEngine.UI;

    [RequireComponent(typeof(TextMeshProUGUI))]
    public abstract class TextAnimation : MonoBehaviour 
    {

        [SerializeField]
        private bool animateAllCharacters;

        [SerializeField]
        [Tooltip("Event that's called when the animation has completed.")]
        private UnityEvent animationCompleted = new UnityEvent();

        public bool[] CharacterShouldAnimate { get; private set; }
        private float lastAnimateTime;
        private const float frameRate = 15f;
        private static readonly float timeBetweenAnimates = 1f / frameRate;
        private TextMeshProUGUI textComponent;
        private TMP_TextInfo textInfo;
        private TMP_MeshInfo[] cachedMeshInfo;

        public UnityEvent AnimationCompleted
        {
            get
            {
                return this.animationCompleted;
            }
        }

        private TextMeshProUGUI TextComponent
        {
            get
            {
                if (this.textComponent == null) 
                {
                    this.textComponent = this.GetComponent<TextMeshProUGUI>();
                }

                return this.textComponent;
            }
        }

        /// <summary>
        /// Method to animate vertices of a TMP Text object.
        /// </summary>
        /// <returns></returns>
        private void AnimateAll() 
        {
            int characterCount = textInfo.characterCount;

            // If No Characters do nothing
            if (characterCount == 0) 
            {
                return;
            }

            for (int i = 0; i < characterCount; i++) 
            {
                TMP_CharacterInfo charInfo = textInfo.characterInfo[i];

                // Skip characters that are not visible and thus have no geometry to manipulate.
                if (!charInfo.isVisible)
                {
                    continue;
                }

                // Get the index of the material used by the current character.
                int materialIndex = textInfo.characterInfo[i].materialReferenceIndex;

                // Get the index of the first vertex used by this text element.
                int vertexIndex = textInfo.characterInfo[i].vertexIndex;

                // Get the cached vertices of the mesh used by this text element (character or sprite).
                Vector3[] sourceVertices = cachedMeshInfo[materialIndex].vertices;

                // Determine the center point of each character at the baseline.
                //Vector2 charMidBasline = new Vector2((sourceVertices[vertexIndex + 0].x + sourceVertices[vertexIndex + 2].x) / 2, charInfo.baseLine);
                // Determine the center point of each character.
                Vector2 charMidBasline = (sourceVertices[vertexIndex + 0] + sourceVertices[vertexIndex + 2]) / 2;

                // Need to translate all 4 vertices of each quad to aligned with middle of character / baseline.
                // This is needed so the matrix TRS is applied at the origin for each character.
                Vector3 offset = charMidBasline;

                Vector3[] destinationVertices = textInfo.meshInfo[materialIndex].vertices;

                destinationVertices[vertexIndex + 0] = sourceVertices[vertexIndex + 0] - offset;
                destinationVertices[vertexIndex + 1] = sourceVertices[vertexIndex + 1] - offset;
                destinationVertices[vertexIndex + 2] = sourceVertices[vertexIndex + 2] - offset;
                destinationVertices[vertexIndex + 3] = sourceVertices[vertexIndex + 3] - offset;

                Vector2 translation;
                float rotation, scale;

                // This is where the derived class sets translation/rotation/scale
                Animate( i, out translation, out rotation, out scale );
                Matrix4x4 matrix = Matrix4x4.TRS( translation, Quaternion.Euler( 0f, 0f, rotation ), scale * Vector3.one );

                destinationVertices[vertexIndex + 0] = matrix.MultiplyPoint3x4(destinationVertices[vertexIndex + 0]);
                destinationVertices[vertexIndex + 1] = matrix.MultiplyPoint3x4(destinationVertices[vertexIndex + 1]);
                destinationVertices[vertexIndex + 2] = matrix.MultiplyPoint3x4(destinationVertices[vertexIndex + 2]);
                destinationVertices[vertexIndex + 3] = matrix.MultiplyPoint3x4(destinationVertices[vertexIndex + 3]);

                destinationVertices[vertexIndex + 0] += offset;
                destinationVertices[vertexIndex + 1] += offset;
                destinationVertices[vertexIndex + 2] += offset;
                destinationVertices[vertexIndex + 3] += offset;
            }

            ApplyChangesToMesh();
        }

        /// <summary>
        /// Set component to animate all visible characters
        /// </summary>
        public void SetAnimateAll() 
        {
            string taglessText = TextTagParser.RemoveAllTags(this.TextComponent.text);
            CharacterShouldAnimate = new bool[taglessText.Length];

            for (int i = 0; i < taglessText.Length; i++) 
            {
                CharacterShouldAnimate[i] = true;
            }
        }

        protected virtual void Start() 
        {
            this.TextComponent.ForceMeshUpdate();
            this.lastAnimateTime = float.MinValue;
        }

        protected virtual void OnEnable() 
        {
            TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
        }

        protected virtual void OnDisable() 
        {
            TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
        }

        protected virtual void OnTextChanged(Object obj) 
        {
            if (obj == TextComponent ) 
            {
                // We force an update of the text object since it would only be updated at the end of the frame. Ie. before this code is executed on the first frame.
                // Alternatively, we could yield and wait until the end of the frame when the text object will be generated.
                //TextComponent.ForceMeshUpdate( );

                textInfo = TextComponent.textInfo;

                // Cache the vertex data of the text object as the Jitter FX is applied to the original position of the characters.
                cachedMeshInfo = textInfo.CopyMeshInfoVertexData();

                if (animateAllCharacters) 
                {
                    SetAnimateAll();
                }
            }
        }

        protected virtual void Update() 
        {
            if (Time.time > this.lastAnimateTime + timeBetweenAnimates) 
            {
                AnimateAll();
                this.lastAnimateTime = Time.time;
            }
        }

        protected abstract void Animate(int characterIndex, out Vector2 translation, out float rotation, out float scale);

        /// <summary>
        /// Apply the modified vertices (calculated by Animate) to the mesh
        /// </summary>
        private void ApplyChangesToMesh() 
        {
            TMP_TextInfo textInfo = TextComponent.textInfo;
            
            for (int i = 0; i < textInfo.meshInfo.Length; i++) 
            {
                textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
                TextComponent.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
            }
        }

        private void OnAnimationComplete() 
        {
            if (this.AnimationCompleted != null) 
            {
                this.AnimationCompleted.Invoke();
            }
        }
   }
}