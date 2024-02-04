namespace RedBlueGames.Tools.TextTyper
{
    using System;
    using Cysharp.Threading.Tasks;
    using NaughtyAttributes;
    using System.Collections.Generic;
    using TMPro;
    using UnityEngine;
    using UnityEngine.Events;

    /// <summary>
    /// Type text component types out Text one character at a time. Heavily adapted from synchrok's GitHub project.
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public sealed class TextTyper : MonoBehaviour
    {
        // Characters that are considered punctuation in this language. TextTyper pauses on these characters
        // a bit longer by default. Could be a setting sometime since this doesn't localize.
        private static readonly List<char> punctutationCharacters = new()
        {
            '.',
            ',',
            '!',
            '?'
        };

        [SerializeField]
        [Tooltip("The print delay setting. Could make this an option some day, for fast readers.")]
        private float printDelaySetting = 0.2f;
        
        [SerializeField]
        [Tooltip("Default delay setting will be multiplied by this when the character is a punctuation mark")]
        private float punctuationDelayMultiplier = 2f;
        
        [SerializeField] private bool useSymbolsAnimation;
        
        [SerializeField]
        [Tooltip("The library of ShakePreset animations that can be used by this component.")]
        [ShowIf(nameof(useSymbolsAnimation))]
        private ShakeLibrary shakeLibrary;

        [SerializeField]
        [Tooltip("The library of CurvePreset animations that can be used by this component.")]
        [ShowIf(nameof(useSymbolsAnimation))]
        private CurveLibrary curveLibrary;

        [SerializeField]
        [Tooltip("If set, the typer will type text even if the game is paused (Time.timeScale = 0)")]
        private bool useUnscaledTime;

        [SerializeField]
        [Tooltip("Event that's called when the text has finished printing.")]
        private UnityEvent printCompleted = new ();

        [SerializeField]
        [Tooltip("Event called when a character is printed. Inteded for audio callbacks.")]
        private CharacterPrintedEvent characterPrinted = new ();

        private TextMeshProUGUI textComponent;
        private float defaultPrintDelay;
        private List<TypableCharacter> charactersToType;
        private List<TextAnimation> animations;

        /// <summary>
        /// Gets the PrintCompleted callback event.
        /// </summary>
        /// <value>The print completed callback event.</value>
        public UnityEvent PrintCompleted => printCompleted;

        /// <summary>
        /// Gets the CharacterPrinted event, which includes a string for the character that was printed.
        /// </summary>
        /// <value>The character printed event.</value>
        public CharacterPrintedEvent CharacterPrinted => characterPrinted;

        /// <summary>
        /// Gets a value indicating whether this <see cref="TextTyper"/> is currently printing text.
        /// </summary>
        /// <value><c>true</c> if printing; otherwise, <c>false</c>.</value>
        public bool IsTyping { get; private set; }

        private TextMeshProUGUI TextComponent
        {
            get
            {
                if (textComponent == null)
                {
                    textComponent = GetComponent<TextMeshProUGUI>();
                }

                return textComponent;
            }
        }

        /// <summary>
        /// Types the text into the Text component character by character, using the specified (optional) print delay per character.
        /// </summary>
        /// <param name="text">Text to type.</param>
        /// <param name="printDelay">Print delay (in seconds) per character.</param>
        public void TypeText(string text, float printDelay = -1)
        {
            CleanupTyping();

            // Remove all existing TextAnimations
            if (useSymbolsAnimation)
            {
                foreach (var anim in GetComponents<TextAnimation>())
                {
                    Destroy(anim);
                }
            }

            defaultPrintDelay = printDelay > 0 ? printDelay : printDelaySetting;
            ProcessTags(text);

            // Fix Issue-38 by clearing any old textInfo like sprites, so that SubMesh objects don't reshow their contents.
            var textInfo = TextComponent.textInfo;
            textInfo.ClearMeshInfo(false);
            
            TypeTextCharByChar(text);
        }

        /// <summary>
        /// Skips the typing to the end.
        /// </summary>
        public void Skip()
        {
            CleanupTyping();
            TextComponent.maxVisibleCharacters = int.MaxValue;
           UpdateMeshAndAnims();
           OnTypewritingComplete();
        }

        private void CleanupTyping()
        {
            if (IsTyping)
            {
                IsTyping = false;
            }
        }
        
        private async void TypeTextCharByChar(string text)
        {
            IsTyping = true;
            while (IsTyping)
            {
                TextComponent.text = TextTagParser.RemoveCustomTags(text);
                for (var numPrintedCharacters = 0; numPrintedCharacters < charactersToType.Count; ++numPrintedCharacters)
                {
                    TextComponent.maxVisibleCharacters = numPrintedCharacters + 1;
                    UpdateMeshAndAnims();

                    var printedChar = charactersToType[numPrintedCharacters];
                    OnCharacterPrinted(printedChar.ToString()); 
                    await UniTask.Delay(TimeSpan.FromSeconds(printedChar.Delay), !useUnscaledTime);
                }

                IsTyping = false;
                OnTypewritingComplete();
            }
        }

        private void UpdateMeshAndAnims()
        {
            // This must be done here rather than in each TextAnimation's OnTMProChanged
            // b/c we must cache mesh data for all animations before animating any of them

            // Update the text mesh data (which also causes all attached TextAnimations to cache the mesh data)
           TextComponent.ForceMeshUpdate();

            // Force animate calls on all TextAnimations because TMPro has reset the mesh to its base state
            // NOTE: This must happen immediately. Cannot wait until end of frame, or the base mesh will be rendered
            for (var i = 0; i < animations?.Count; i++)
            {
               animations[i].AnimateAllChars();
            }
        }

        /// <summary>
        /// Calculates print delays for every visible character in the string.
        /// Processes delay tags, punctuation delays, and default delays
        /// Also processes shake and curve animations and spawns
        /// the appropriate TextAnimation components
        /// </summary>
        /// <param name="text">Full text string with tags</param>
        private void ProcessTags(string text)
        {
            charactersToType = new List<TypableCharacter>();
            if (useSymbolsAnimation)
            {
                animations = new List<TextAnimation>();
            }
            var textAsSymbolList = TextTagParser.CreateSymbolListFromText(text);

            var printedCharCount = 0;
            var customTagOpenIndex = 0;
            var customTagParam = "";
            var nextDelay = defaultPrintDelay;
            
            foreach (var symbol in textAsSymbolList)
            {
                // Sprite prints a character so we need to throw it out and treat it like a character
                if (symbol.IsTag && !symbol.IsReplacedWithSprite)
                {
                    // TODO - Verification that custom tags are not nested, b/c that will not be handled gracefully
                    if (symbol.Tag.TagType == TextTagParser.CustomTags.Delay)
                    {
                        nextDelay = symbol.Tag.IsClosingTag ? defaultPrintDelay : symbol.GetFloatParameter(defaultPrintDelay);
                    }
                    else if (useSymbolsAnimation && symbol.Tag.TagType is TextTagParser.CustomTags.Anim)
                    {
                        if (symbol.Tag.IsClosingTag)
                        {
                            // Add a TextAnimation component to process this animation
                            TextAnimation anim;
                            if (IsAnimationShake(customTagParam))
                            {
                                anim = gameObject.AddComponent<ShakeAnimation>();
                                ((ShakeAnimation)anim).LoadPreset(this.shakeLibrary, customTagParam);
                            }
                            else if (IsAnimationCurve(customTagParam))
                            {
                                anim = gameObject.AddComponent<CurveAnimation>();
                                ((CurveAnimation)anim).LoadPreset(this.curveLibrary, customTagParam);
                            }
                            else
                            {
                                Debug.LogError("Could not find animation");
                                continue;
                            }
                            
                            anim.UseUnscaledTime = useUnscaledTime;
                            anim.SetCharsToAnimate(customTagOpenIndex, printedCharCount - 1);
                            anim.enabled = true;
                            animations.Add(anim);
                        }
                        else
                        {
                            customTagOpenIndex = printedCharCount;
                            customTagParam = symbol.Tag.Parameter;
                        }
                    }
                }
                else
                {
                    printedCharCount++;

                    var characterToType = new TypableCharacter();
                    if (symbol.IsTag && symbol.IsReplacedWithSprite)
                    {
                        characterToType.IsSprite = true;
                    }
                    else
                    {
                        characterToType.Char = symbol.Character;
                    }

                    characterToType.Delay = nextDelay;
                    if (punctutationCharacters.Contains(symbol.Character))
                    {
                        characterToType.Delay *= punctuationDelayMultiplier;
                    }

                    charactersToType.Add(characterToType);
                }
            }
        }

        private bool IsAnimationShake(string animName)
        {
            return shakeLibrary != null && shakeLibrary.ContainsKey(animName);
        }

        private bool IsAnimationCurve(string animName)
        {
            return curveLibrary != null && curveLibrary.ContainsKey(animName);
        }

        private void OnCharacterPrinted(string printedCharacter)
        {
            if (CharacterPrinted != null)
            {
               CharacterPrinted.Invoke(printedCharacter);
            }
        }

        private void OnTypewritingComplete()
        {
            if (PrintCompleted != null)
            {
               PrintCompleted.Invoke();
            }
        }

        /// <summary>
        /// Event that signals a Character has been printed to the Text component.
        /// </summary>
        [System.Serializable]
        public class CharacterPrintedEvent : UnityEvent<string>
        {
        }

        /// <summary>
        /// This class represents a printed character moment, which should correspond with a
        /// delay in the text typer. It became necessary to make this a class when I had
        /// to account for Sprite tags which are replaced by a sprite that counts as a "visble"
        /// character. These sprites would not be in the Text string stripped of tags,
        /// so this allows us to track and print them with a delay.
        /// </summary>
        private class TypableCharacter
        {
            public char Char { get; set; }

            public float Delay { get; set; }

            public bool IsSprite { get; set; }

            public override string ToString()
            {
                return IsSprite ? "Sprite" : Char.ToString();
            }
        }
    }
}
