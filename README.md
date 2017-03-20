Unity-TextTyper
=========================

TextTyper is a text typing effect component for Unity. TextTyper prints out characters one by one to a uGUI Text component. Adapted by RedBlueGames from synchrok's GitHub project (https://github.com/synchrok/TypeText).

It's easy to find other examples of Text printing components, but TextTyper provides two major differences:
* Correct wrapping as characters are printed
* Support for Rich Text Tags

Features
--------
- **Define Text Speed Per Character**: ```Mayday! <delay=0.05>S.O.S.</delay>```
- **Support for uGUI Rich Text Tags**: ```<b>,<i>,<size>,<color>,...```
- **Additional delay for punctuation**
- **Skip to end**
- **OnComplete Callback**
- **OnCharacterPrinted Callback (for audio)**

Screenshots
--------
![TypeText Screenshot GIF](https://github.com/redbluegames/unity-text-typer/blob/master/README-Images/ss_chat_watermarked.gif)
Image of TextTyper in Sparklite (© RedBlueGames 2016)

The Code
--------
The core of our text typer is a coroutine that’s triggered via the TypeText method. The coroutine has the following steps:
Check next character to see if it’s the start of a Rich Text tag
- If It’s a tag, parse it and apply it. Right now “applying” a tag just means modifying the print delay, but other tags could be added. Add it to a list of tags that need to be closed (because we have not yet reached the corresponding closing tag in our string). Move to next character and repeat
- If it’s not a tag, print it
- Wait for the print delay
- Check if we are complete

The tool also uses RichTextTag.cs, a class that’s used to help with parsing.
There are a few details I left out, but this should give you enough to start reading through the code if you want to know more.

How to Help
-------
The easiest way to help with the project is to just to use it! As you use it, you can submit bugs and feature requests through GitHub issues.

Contributors
-------
**Issue Resporters**
- JonathanGorr

License and Credits
-------
- **TypeText** is under MIT license. See the [LICENSE](LICENSE) file for more info.
- Typing sound effect (UITextDisplay.wav) provided by @kevinrmabie. Free for others to use, no attribution necessary.
