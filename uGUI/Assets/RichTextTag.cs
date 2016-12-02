using UnityEngine;
using System.Collections;

public class RichTextTag
{
    private const char OpeningNodeDelimeter = '<';
    private const char CloseNodeDelimeter = '>';
    private const char EndTagDelimeter = '/';
    private const string ParameterDelimeter = "=";

    public RichTextTag(string tagText)
    {
        this.TagText = tagText;
    }

    public string TagText { get; private set; }

    public string TagType
    {
        get
        {
            // Strip start and end tags
            var tagType = this.TagText.Substring(1, this.TagText.Length - 2);
            tagType = tagType.TrimStart(EndTagDelimeter);

            // Strip Parameter
            var parameterDelimeterIndex = tagType.IndexOf(ParameterDelimeter);
            if (parameterDelimeterIndex > 0)
            {
                tagType = tagType.Substring(0, parameterDelimeterIndex);
            }

            return tagType;
        }
    }

    public string Parameter
    {
        get
        {
            var parameterDelimeterIndex = this.TagText.IndexOf(ParameterDelimeter);
            if (parameterDelimeterIndex < 0)
            {
                return string.Empty;
            }

            // Subtract two, one for the delimeter and one for the closing character
            var parameterLength = this.TagText.Length - parameterDelimeterIndex - 2;
            return this.TagText.Substring(parameterDelimeterIndex + 1, parameterLength);
        }
    }

    public bool IsClosignTag { get ; private set; }

    public static bool StringStartsWithTag(string body)
    {
        return body.StartsWith(OpeningNodeDelimeter.ToString());
    }

    public static RichTextTag ParseNext(string body)
    {
        // Trim up to the first delimeter
        var openingDelimeterIndex = body.IndexOf(OpeningNodeDelimeter);

        // No opening delimeter found. Might want to throw.
        if (openingDelimeterIndex < 0)
        {
            return null;
        }

        var closingDelimeterIndex = body.IndexOf(CloseNodeDelimeter);

        // No closingDlimtere found. Might want to throw.
        if (closingDelimeterIndex < 0)
        {
            return null;
        }

        var tagText = body.Substring(openingDelimeterIndex, closingDelimeterIndex - openingDelimeterIndex + 1);
        return new RichTextTag(tagText);
    }

    public override string ToString()
    {
        return string.Format("[RichTextTag: TagText={0}, TagType={1}, IsClosignTag={2}, Parameter={3}]", TagText, TagType, IsClosignTag, Parameter);
    }
}
