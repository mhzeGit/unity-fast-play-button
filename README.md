# Unity Fast Play Button

A Unity Editor package that adds a **Fast Play** button to the toolbar for quicker iteration. It temporarily disables Domain Reload and Scene Reload when entering Play Mode, then automatically restores your original settings when you exit.

---

## Features

- Dedicated Fast Play button on the Unity toolbar
- Temporarily disables Domain Reload and Scene Reload for faster Play Mode entry
- Automatically restores your original settings when exiting Play Mode
- Standard Play button is unaffected and always uses your normal settings
- Settings are safely restored even if the editor is closed or scripts reload mid-session

---

## Requirements

- Unity 6000.1 or later

---

## Installation

1. Open **Window → Package Manager**
2. Click the **+** button → **Add package from git URL...**
3. Enter:
   ```
   https://github.com/mhzeGit/unity-fast-play-button.git
   ```
4. Click **Add**

---

## How It Works

Once installed, a **Fast Play** button appears on the right side of the Unity toolbar, next to the default Play controls.

| State | Button |
|---|---|
| Editor idle | Fast Play button (normal) |
| Fast playing | Green Stop button |
| Playing normally | Fast Play button disabled |

1. Click **Fast Play** to enter Play Mode with Domain/Scene reload disabled
2. The button turns green and becomes a **Stop** button while fast-playing
3. Click the green **Stop** button to exit — your original settings are restored automatically

---

## Author

Made by [mhze](mailto:mhze.uk@gmail.com)
