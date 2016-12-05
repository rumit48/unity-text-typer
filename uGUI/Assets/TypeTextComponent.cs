using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

/// <summary>
/// Type text component types out Text one character at a time.
/// </summary>
[RequireComponent(typeof(Text))]
public class TypeTextComponent : MonoBehaviour
{
    private static readonly List<string> UnityTagTypes = new List<string> { "b", "i", "size", "color" };
    private static readonly List<string> CustomTagTypes = new List<string> { "speed" };

    [SerializeField]
    private float defaultPrintDelay = 0.05f;

    [SerializeField]
    private UnityEvent printCompleted;

    private Text textComponent;
    private string printingText;
    private string displayedText;
    private float currentPrintDelay;
    private Coroutine typeTextCoroutine;
    private Stack<RichTextTag> outstandingTags;

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

    public UnityEvent PrintCompleted
    {
        get
        {
            return this.printCompleted;
        }
    }

    /// <summary>
    /// Called by Unity when the component is created.
    /// </summary>
    protected void Awake()
    {
        this.outstandingTags = new Stack<RichTextTag>();
    }

    public void SetText(string text, float printDelay = -1)
    {
        this.defaultPrintDelay = printDelay > 0 ? printDelay : this.defaultPrintDelay;
        this.printingText = text;

        if (this.typeTextCoroutine != null)
        {
            this.StopCoroutine(this.typeTextCoroutine);
        }

        this.typeTextCoroutine = this.StartCoroutine(this.TypeText(text));
    }

    public void SkipTypeText()
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

    public bool IsSkippable()
    {
        return this.typeTextCoroutine != null;
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

    private IEnumerator TypeText(string text)
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

            this.displayedText += text[i];
            this.TextComponent.text = this.displayedText;

            this.CloseOutstandingTags();

            yield return new WaitForSeconds(this.currentPrintDelay);
        }

        this.typeTextCoroutine = null;
        this.OnTypewritingComplete();
    }

    private void ApplyTag(RichTextTag tag)
    {
        // Push or Pop the tag from the outstanding tags stack.
        if (!tag.IsClosignTag)
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
                speed = tag.IsClosignTag ? this.defaultPrintDelay : float.Parse(tag.Parameter);
            }
            catch
            {
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

    private void OnTypewritingComplete()
    {
        if (this.PrintCompleted != null)
        {
            this.PrintCompleted.Invoke();
        }
    }
}

public static class TypeTextComponentUtility
{
    public static void TypeText(this Text label, string text, float speed = 0.05f, UnityAction onComplete = null)
    {
        var typeText = label.GetComponent<TypeTextComponent>();
        if (typeText == null)
        {
            typeText = label.gameObject.AddComponent<TypeTextComponent>();
        }

        typeText.SetText(text, speed);
        typeText.PrintCompleted.AddListener(onComplete);
    }

    public static bool IsSkippable(this Text label)
    {
        var typeText = label.GetComponent<TypeTextComponent>();
        if (typeText == null)
        {
            typeText = label.gameObject.AddComponent<TypeTextComponent>();
        }

        return typeText.IsSkippable();
    }

    public static void SkipTypeText(this Text label)
    {
        var typeText = label.GetComponent<TypeTextComponent>();
        if (typeText == null)
        {
            typeText = label.gameObject.AddComponent<TypeTextComponent>();
        }

        typeText.SkipTypeText();
    }
}