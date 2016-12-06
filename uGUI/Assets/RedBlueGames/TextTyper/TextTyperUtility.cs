namespace RedBlueGames.Tools.TextTyper
{
    using System.Collections;
    using UnityEngine;
    using UnityEngine.Events;
    using UnityEngine.UI;

    /// <summary>
    /// TextTyperUtility is a helper class that extends Text component to add TextTyper functionality.
    /// </summary>
    public static class TextTyperUtility
    {
        /// <summary>
        /// Types the specified text into the Text component.
        /// </summary>
        /// <param name="label">Text Component label to print to.</param>
        /// <param name="text">Text to type.</param>
        /// <param name="delayPerCharacter">Delay per character.</param>
        /// <param name="onComplete">On complete callback.</param>
        public static void TypeText(this Text label, string text, float delayPerCharacter = 0.05f, UnityAction onComplete = null)
        {
            var typeText = label.GetComponent<TextTyper>();
            if (typeText == null)
            {
                typeText = label.gameObject.AddComponent<TextTyper>();
            }

            typeText.TypeText(text, delayPerCharacter);
            if (onComplete != null)
            {
                typeText.PrintCompleted.AddListener(onComplete);
            }
        }

        /// <summary>
        /// Determines if the TypeText is skippable.
        /// </summary>
        /// <returns><c>true</c> if skippable; otherwise, <c>false</c>.</returns>
        /// <param name="label">Text component label to type into.</param>
        public static bool IsSkippable(this Text label)
        {
            var typeText = label.GetComponent<TextTyper>();
            if (typeText == null)
            {
                typeText = label.gameObject.AddComponent<TextTyper>();
            }

            return typeText.IsSkippable();
        }

        /// <summary>
        /// Skips the text that's being typed into the Text component.
        /// </summary>
        /// <param name="label">Text component label to type into.</param>
        public static void SkipTypeText(this Text label)
        {
            var typeText = label.GetComponent<TextTyper>();
            if (typeText == null)
            {
                typeText = label.gameObject.AddComponent<TextTyper>();
            }

            typeText.Skip();
        }
    }
}