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


License and Credits
-------
- **TypeText** is under MIT license. See the [LICENSE](LICENSE) file for more info.
- Typing sound effect (UITextDisplay.wav) provided by @kevinrmabie. Free for others to use, no attribution necessary.
