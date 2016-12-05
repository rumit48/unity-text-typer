namespace RedBlueGames.Tools.TextTyper
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Events;
    using UnityEngine.UI;

    /// <summary>
    /// Type text component types out Text one character at a time. Heavily adapted from synchrok's GitHub project.
    /// </summary>
    [RequireComponent(typeof(Text))]
    public class TextTyper : MonoBehaviour
    {
        private static readonly List<string> UnityTagTypes = new List<string> { "b", "i", "size", "color" };
        private static readonly List<string> CustomTagTypes = new List<string> { "speed" };

        [SerializeField]
        [Tooltip("The default delay between characters.")]
        private float defaultPrintDelay = 0.05f;

        [SerializeField]
        [Tooltip("Event that's called when the text has finished printing.")]
        private UnityEvent printCompleted = new UnityEvent();

        [SerializeField]
        [Tooltip("Event called when a character is printed. Inteded for audio callbacks.")]
        private CharacterPrintedEvent characterPrinted = new CharacterPrintedEvent();

        private Text textComponent;
        private string printingText;
        private string displayedText;
        private float currentPrintDelay;
        private Coroutine typeTextCoroutine;
        private Stack<RichTextTag> outstandingTags;

        /// <summary>
        /// Gets the PrintCompleted callback event.
        /// </summary>
        /// <value>The print completed callback event.</value>
        public UnityEvent PrintCompleted
        {
            get
            {
                return this.printCompleted;
            }
        }

        /// <summary>
        /// Gets the CharacterPrinted event, which includes a string for the character that was printed.
        /// </summary>
        /// <value>The character printed event.</value>
        public CharacterPrintedEvent CharacterPrinted
        {
            get
            {
                return this.characterPrinted;
            }
        }

        private Text TextComponent
        {
            get
            {
                if (this.textComponent == null)
                {
                    this.textComponent = this.GetComponent<Text>();
                }

                return this.textComponent;
            }
        }

        /// <summary>
        /// Types the text into the Text component character by character, using the specified (optional) print delay per character.
        /// </summary>
        /// <param name="text">Text to type.</param>
        /// <param name="printDelay">Print delay (in seconds) per character.</param>
        public void TypeText(string text, float printDelay = -1)
        {
            this.defaultPrintDelay = printDelay > 0 ? printDelay : this.defaultPrintDelay;
            this.printingText = text;

            if (this.typeTextCoroutine != null)
            {
                this.StopCoroutine(this.typeTextCoroutine);
            }

            this.typeTextCoroutine = this.StartCoroutine(this.TypeTextCharByChar(text));
        }

        /// <summary>
        /// Skips the typing to the end.
        /// </summary>
        public void Skip()
        {
            if (this.typeTextCoroutine != null)
            {
                this.StopCoroutine(this.typeTextCoroutine);
                this.typeTextCoroutine = null;
            }

            this.outstandingTags.Clear();
            this.TextComponent.text = RemoveCustomTags(this.printingText);

            this.OnTypewritingComplete();
        }

        /// <summary>
        /// Determines whether this instance is skippable.
        /// </summary>
        /// <returns><c>true</c> if this instance is skippable; otherwise, <c>false</c>.</returns>
        public bool IsSkippable()
        {
            return this.typeTextCoroutine != null;
        }

        /// <summary>
        /// Called by Unity when the component is created.
        /// </summary>
        protected void Awake()
        {
            this.outstandingTags = new Stack<RichTextTag>();
        }

        private static string RemoveCustomTags(string text)
        {
            var textWithoutTags = text;
            foreach (var tagType in CustomTagTypes)
            {
                textWithoutTags = RichTextTag.RemoveTagsFromString(textWithoutTags, tagType);
            }

            return textWithoutTags;
        }

        private IEnumerator TypeTextCharByChar(string text)
        {
            this.displayedText = string.Empty;
            this.TextComponent.text = string.Empty;
            this.currentPrintDelay = this.defaultPrintDelay;

            for (var i = 0; i < text.Length; i++)
            {
                // Check for opening nodes
                var remainingText = text.Substring(i, text.Length - i);
                if (RichTextTag.StringStartsWithTag(remainingText))
                {
                    var tag = RichTextTag.ParseNext(remainingText);
                    this.ApplyTag(tag);
                    i += tag.Length - 1;
                    continue;
                }

                var printedCharacter = text[i];
                this.displayedText += printedCharacter;
                this.TextComponent.text = this.displayedText;

                this.CloseOutstandingTags();

                this.OnCharacterPrinted(printedCharacter.ToString());

                yield return new WaitForSeconds(this.currentPrintDelay);
            }

            this.typeTextCoroutine = null;
            this.OnTypewritingComplete();
        }

        private void ApplyTag(RichTextTag tag)
        {
            // Push or Pop the tag from the outstanding tags stack.
            if (!tag.IsClosingTag)
            {
                this.outstandingTags.Push(tag);
            }
            else
            {
                // Pop outstanding tag
                var poppedTag = this.outstandingTags.Pop();
                if (poppedTag.TagType != tag.TagType)
                {
                    var assertionMessage = string.Format(
                                               "Popped TagType [{0}] did not match last outstanding tagType [{1}] " +
                                               "in TypeText. Unity only respects tags that are added as a stack.",
                                               poppedTag.TagType,
                                               tag.TagType);
                    Debug.LogError(assertionMessage);
                }
            }

            // Execute Custom Tags here
            if (tag.TagType == "speed")
            {
                float speed = 0.0f;
                try
                {
                    speed = tag.IsClosingTag ? this.defaultPrintDelay : float.Parse(tag.Parameter);
                }
                catch (System.FormatException e)
                {
                    var warning = string.Format(
                                      "Invalid paramter format found in tag [{0}]. Parameter [{1}] does not parse to a float. Exception: {2}",
                                      tag,
                                      tag.Parameter,
                                      e);
                    Debug.LogWarning(warning, this);
                    speed = this.defaultPrintDelay;
                }

                this.currentPrintDelay = speed;
            }

            // We only want to add in text of tags for elements that Unity will parse
            // in its RichText enabled Text widget
            if (UnityTagTypes.Contains(tag.TagType))
            {
                this.displayedText += tag.TagText;
            }
        }

        private void CloseOutstandingTags()
        {
            foreach (var tag in this.outstandingTags)
            {
                // We only need to add back in Unity tags, since they've been
                // added to the text and Unity expects closing tags.
                if (UnityTagTypes.Contains(tag.TagType))
                {
                    this.textComponent.text = string.Concat(this.textComponent.text, tag.ClosingTagText);
                }
            }
        }

        private void OnCharacterPrinted(string printedCharacter)
        {
            if (this.CharacterPrinted != null)
            {
                this.CharacterPrinted.Invoke(printedCharacter);
            }
        }

        private void OnTypewritingComplete()
        {
            if (this.PrintCompleted != null)
            {
                this.PrintCompleted.Invoke();
            }
        }

        /// <summary>
        /// Event that signals a Character has been printed to the Text component.
        /// </summary>
        [System.Serializable]
        public class CharacterPrintedEvent : UnityEvent<string>
        {
        }
    }
}