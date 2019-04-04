namespace RedBlueGames.Tools.TextTyper.Tests
{
    using UnityEditor;
    using UnityEngine;
    using NUnit.Framework;

    public class TextTagParserTests
    {
        [Test]
        public void RemoveCustomTags_EmptyString_ReturnsEmpty( )
        {
            var textToType = string.Empty;
            var generatedText = TextTagParser.RemoveCustomTags(textToType);

            var expectedText = textToType;

            Assert.AreEqual(expectedText, generatedText);
        }

        [Test]
        public void RemoveCustomTags_OnlyUnityRichTextTags_ReturnsUnityTags( )
        {
            var textToType = "<b><i></i></b>";
            var generatedText = TextTagParser.RemoveCustomTags(textToType);

            var expectedText = textToType;

            Assert.AreEqual(expectedText, generatedText);
        }

        [Test]
        public void RemoveCustomTags_OnlyCustomRichTextTags_ReturnsEmpty( )
        {
            var textToType = "<delay=5></delay><shake=3></shake><curve=sine></curve>";
            var generatedText = TextTagParser.RemoveCustomTags(textToType);

            var expectedText = string.Empty;

            Assert.AreEqual(expectedText, generatedText);
        }
    }
}