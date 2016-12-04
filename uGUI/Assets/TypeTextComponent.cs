using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class TypeTextComponent : MonoBehaviour
{
    public delegate void OnComplete();

    private static readonly List<string> uGUITagTypes = new List<string> { "b", "i", "size", "color" };
    private static readonly List<string> customTagTypes = new List<string> { "speed" };

    [SerializeField]
    private float defaultSpeed = 0.05f;

    private Text textComponent;
    private string displayedText;
    private string finalText;
    private float currentSpeed;
    private Coroutine typeTextCoroutine;
    private Stack<RichTextTag> outstandingTags;
    private OnComplete onCompleteCallback;

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

    public void Awake()
    {
        this.outstandingTags = new Stack<RichTextTag>();
    }

    public void SetText(string text, float speed = -1)
    {
        this.defaultSpeed = speed > 0 ? speed : this.defaultSpeed;
        this.finalText = RemoveCustomTags(text);
        this.TextComponent.text = "";

        if (this.typeTextCoroutine != null)
        {
            StopCoroutine(this.typeTextCoroutine);
        }

        this.typeTextCoroutine = StartCoroutine(TypeText(text));
    }

    public void SkipTypeText()
    {
        if (this.typeTextCoroutine != null)
        {
            StopCoroutine(typeTextCoroutine);
            this.typeTextCoroutine = null;
        }

        this.outstandingTags.Clear();
        this.textComponent.text = finalText;

        this.OnTypewritingComplete();
    }

    public IEnumerator TypeText(string text)
    {
        this.displayedText = "";
        this.currentSpeed = this.defaultSpeed;

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
            textComponent.text = this.displayedText;

            this.CloseOutstandingTags();

            yield return new WaitForSeconds(this.currentSpeed);
        }

        this.typeTextCoroutine = null;
        this.OnTypewritingComplete();
    }

    private void ApplyTag(RichTextTag tag)
    {
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

        if (tag.TagType == "speed")
        {
            float speed = 0.0f;
            try
            {
                speed = tag.IsClosignTag ? this.defaultSpeed : float.Parse(tag.Parameter);
            }
            catch
            {
                speed = this.defaultSpeed;
            }

            this.currentSpeed = speed;
        }

        // We only want to add in text of tags for elements that Unity will parse
        // in its RichText enabled Text widget
        if (uGUITagTypes.Contains(tag.TagType))
        {
            displayedText += tag.TagText;
        }
    }

    private void CloseOutstandingTags()
    {
        foreach (var tag in this.outstandingTags)
        {
            // We only need to add back in Unity tags, since they've been
            // added to the text and Unity expects closing tags.
            if (uGUITagTypes.Contains(tag.TagType))
            {
                textComponent.text = string.Concat(textComponent.text, tag.ClosingTagText);
            }
        }
    }

    private string RemoveCustomTags(string text)
    {
        var textWithoutTags = text;
        foreach (var tagType in customTagTypes)
        {
            textWithoutTags = RichTextTag.RemoveTagsFromString(textWithoutTags, tagType);
        }

        return textWithoutTags;
    }

    public bool IsSkippable()
    {
        return typeTextCoroutine != null;
    }

    public void SetOnComplete(OnComplete onComplete)
    {
        onCompleteCallback = onComplete;
    }

    private void OnTypewritingComplete()
    {
        if (this.onCompleteCallback != null)
        {
            this.onCompleteCallback.Invoke();
        }
    }
}

public static class TypeTextComponentUtility
{
    public static void TypeText(this Text label, string text, float speed = 0.05f, TypeTextComponent.OnComplete onComplete = null)
    {
        var typeText = label.GetComponent<TypeTextComponent>();
        if (typeText == null)
        {
            typeText = label.gameObject.AddComponent<TypeTextComponent>();
        }

        typeText.SetText(text, speed);
        typeText.SetOnComplete(onComplete);
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