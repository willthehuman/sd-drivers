# Steam Deck Controller Remapper
Steam Deck Controller Remapper allows users to rebind controls directly. (No virtual xbox 360 stuff).

Originally made because it was impossible to play Valorant using other methods (trackpads weren't recognized as mouse)

In other words, this allows you to rebind any button of the steam deck controller to any button of your keyboard/mouse without having to emulate an xbox 360 controller first. 

- BAD: (Steam Deck -> Virtual Xbox 360 Controller -> Emulated Keyboard/Mouse inputs)
- GOOD: (Steam Deck -> *SIMULATED* Keyboard/Mouse inputs)

# How to use
+ Edit config.json, config_axis.json, spammables.json, spammable_axis.json and thresholds.json to your liking. (Please look at buttons.json, axis.json and keys.json inside the content folder for possible values)
+ Launch sd-drivers.exe
+ Activate driver

Note: A "spammable" input means that the key will continuously trigger while the corresponding button is held. This is useful for a walking button or a shooting input.
Note: For some of the axis, a negative value is possible.

# In development 
+ UI with visual feedback
+ Modify bindings directly in the UI
+ Bind Steam Deck axis to other axis
+ Key combos
+ Emulate other controllers
+ And much more!!

# Known bugs
+ Mouse clicks are triggered using a keyboard command. This is why they don't work in many places. Since my focus was to make it work with valorant, mouse clicks work in valorant (in-game, not menus)

# NeptuneControllerButton (config.json + spammables.json)
| Possible values for Deck Buttons |
| ------------- |
|BtnX
|BtnY
|BtnA
|BtnB
|BtnMenu
|BtnOptions
|BtnSteam
|BtnQuickAccess
|BtnDpadUp
|BtnDpadLeft
|BtnDpadRight
|BtnDpadDown
|BtnL1
|BtnR1
|BtnL2
|BtnR2
|BtnL4
|BtnR4
|BtnL5
|BtnR5
|BtnRPadPress
|BtnLPadPress
|BtnRPadTouch
|BtnLPadTouch
|BtnRStickPress
|BtnLStickPress
|BtnRStickTouch
|BtnLStickTouch

# NeptuneControllerAxis (config_axis.json + spammable_axis.json)
| Possible values for Deck Axis |
| ------------- |
|LeftStickX|
|LeftStickY|
|RightStickX|
|RightStickY|
|LeftPadX|
|LeftPadY|
|RightPadX|
|RightPadY|
|LeftPadPressure|
|RightPadPressure|
|L2|
|R2|
|GyroAccelX|
|GyroAccelY|
|GyroAccelZ|
|GyroRoll|
|GyroPitch|
|GyroYaw|
|Q1|
|Q2|
|Q3|
|Q4|

# VirtualKeyCode (config.json + config_axis.json)
| Key        | Code           |
| ------------- |:-------------:|
|None|0
|Lbutton|1
|Rbutton|2
|Cancel|3
|Mbutton|4
|Xbutton1|5
|Xbutton2|6
|Back|8
|Tab|9
|Clear|12
|Return|13
|Shift|16
|Control|17
|Menu|18
|Pause|19
|Capital|20
|Kana|21
|Hangul|21
|Junja|23
|Final|24
|Hanja|25
|Kanji|25
|Escape|27
|Convert|28
|Nonconvert|29
|Accept|30
|Modechange|31
|Space|32
|Prior|33
|Next|34
|End|35
|Home|36
|Left|37
|Up|38
|Right|39
|Down|40
|Select|41
|Print|42
|Execute|43
|PrintScreen|44
|Snapshot|44
|Insert|45
|Delete|46
|Help|47
|Key0|48
|Key1|49
|Key2|50
|Key3|51
|Key4|52
|Key5|53
|Key6|54
|Key7|55
|Key8|56
|Key9|57
|KeyA|65
|KeyB|66
|KeyC|67
|KeyD|68
|KeyE|69
|KeyF|70
|KeyG|71
|KeyH|72
|KeyI|73
|KeyJ|74
|KeyK|75
|KeyL|76
|KeyM|77
|KeyN|78
|KeyO|79
|KeyP|80
|KeyQ|81
|KeyR|82
|KeyS|83
|KeyT|84
|KeyU|85
|KeyV|86
|KeyW|87
|KeyX|88
|KeyY|89
|KeyZ|90
|LeftWin|91
|RightWin|92
|Apps|93
|Sleep|95
|Numpad0|96
|Numpad1|97
|Numpad2|98
|Numpad3|99
|Numpad4|100
|Numpad5|101
|Numpad6|102
|Numpad7|103
|Numpad8|104
|Numpad9|105
|Multiply|106
|Add|107
|Separator|108
|Subtract|109
|Decimal|110
|Divide|111
|F1|112
|F2|113
|F3|114
|F4|115
|F5|116
|F6|117
|F7|118
|F8|119
|F9|120
|F10|121
|F11|122
|F12|123
|F13|124
|F14|125
|F15|126
|F16|127
|F17|128
|F18|129
|F19|130
|F20|131
|F21|132
|F22|133
|F23|134
|F24|135
|NumLock|144
|Scroll|145
|LeftShift|160
|RightShift|161
|LeftControl|162
|RightControl|163
|LeftMenu|164
|RightMenu|165
|BrowserBack|166
|BrowserForward|167
|BrowserRefresh|168
|BrowserStop|169
|BrowserSearch|170
|BrowserFavorites|171
|BrowserHome|172
|VolumeMute|173
|VolumeDown|174
|VolumeUp|175
|MediaNextTrack|176
|MediaPrevTrack|177
|MediaStop|178
|MediaPlayPause|179
|LaunchMail|180
|LaunchMediaSelect|181
|LaunchApp1|182
|LaunchApp2|183
|Oem1|186
|OemPlus|187
|OemComma|188
|OemMinus|189
|OemPeriod|190
|Oem2|191
|Oem3|192
|Oem4|219
|Oem5|220
|Oem6|221
|Oem7|222
|Oem8|223
|Oem102|226
|Processkey|229
|Packet|231
|Attn|246
|Crsel|247
|Exsel|248
|Ereof|249
|Play|250
|Zoom|251
|Noname|252
|Pa1|253
|OemClear|254
		

# Thanks to
These are the libraries that I am using and customizing for my project.
- https://github.com/mKenfenheuer/hidapi.net
- https://github.com/mKenfenheuer/neptune-hidapi.net
