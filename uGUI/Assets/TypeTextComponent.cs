using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class TypeTextComponent : MonoBehaviour
{
    public delegate void OnComplete();

    [SerializeField]
    private float _defaultSpeed = 0.05f;

    private Text label;
    private string _currentText;
    private string _finalText;
    private Coroutine _typeTextCoroutine;

    private float currentSpeed;

    private static readonly List<string> uGUITagTypes = new List<string> { "b", "i", "size", "color" };
    private static readonly List<string> customTagTypes = new List<string> { "speed" };
    private OnComplete _onCompleteCallback;

    private Stack<RichTextTag> outstandingTags;

    private void Init()
    {
        if (label == null)
            label = GetComponent<Text>();
    }

    public void Awake()
    {
        Init();

        this.outstandingTags = new Stack<RichTextTag>();
    }

    public void SetText(string text, float speed = -1)
    {
        Init();

        _defaultSpeed = speed > 0 ? speed : _defaultSpeed;
        _finalText = RemoveCustomTags(text);
        label.text = "";

        if (_typeTextCoroutine != null)
        {
            StopCoroutine(_typeTextCoroutine);
        }

        _typeTextCoroutine = StartCoroutine(TypeText(text));
    }

    public void SkipTypeText()
    {
        if (_typeTextCoroutine != null)
            StopCoroutine(_typeTextCoroutine);
        _typeTextCoroutine = null;
        this.outstandingTags.Clear();

        label.text = _finalText;

        if (_onCompleteCallback != null)
            _onCompleteCallback();
    }

    public IEnumerator TypeText(string text)
    {
        _currentText = "";
        this.currentSpeed = _defaultSpeed;

        for (var i = 0; i < text.Length; i++)
        {
            // Check for opening nodes
            // TODO: Make functions pure
            var remainingText = text.Substring(i, text.Length - i);
            if (RichTextTag.StringStartsWithTag(remainingText))
            {
                var tag = RichTextTag.ParseNext(remainingText);
                this.ApplyTag(tag);
                i += tag.TagText.Length - 1;
                continue;
            }

            _currentText += text[i];
            label.text = _currentText;

            this.CloseOutstandingTags();

            yield return new WaitForSeconds(this.currentSpeed);
        }

        _typeTextCoroutine = null;

        if (_onCompleteCallback != null)
            _onCompleteCallback();
    }

    private void ApplyTag(RichTextTag tag)
    {
        if (!tag.IsClosignTag)
        {
            this.outstandingTags.Push(tag);

            if (tag.TagType == "speed")
            {
                float speed = 0.0f;
                try
                {
                    speed = float.Parse(tag.Parameter);
                }
                catch
                {
                    speed = this._defaultSpeed;
                }

                this.currentSpeed = speed;
            }
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

        // We only want to add in text of tags for elements that Unity will parse
        // in its RichText enabled Text widget
        if (uGUITagTypes.Contains(tag.TagType))
        {
            _currentText += tag.TagText;
        }
    }

    private void CloseOutstandingTags()
    {
        foreach (var tag in this.outstandingTags)
        {
            // We only need to add back in Unity tags, since they've been
            // added and Unity expects closing tags.
            if (uGUITagTypes.Contains(tag.TagType))
            {
                label.text = string.Concat(label.text, tag.ClosingTagText);
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
        return _typeTextCoroutine != null;
    }

    public void SetOnComplete(OnComplete onComplete)
    {
        _onCompleteCallback = onComplete;
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