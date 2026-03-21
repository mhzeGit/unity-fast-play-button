# Unity Fast Play Button

A Unity Editor package that adds a **Fast Play** button to the toolbar for quicker iteration. It temporarily disables Domain Reload and Scene Reload when entering Play Mode, then automatically restores your original settings when you exit.

---

## How it works

- Click **Fast Play** to enter Play Mode with Domain/Scene reload disabled (faster entry).
- The button turns green and becomes a **Stop** button while fast-playing.
- When you exit Play Mode, your original Enter Play Mode settings are restored automatically.
- The standard Play button is unaffected and always uses your normal settings.
- If the editor is closed or scripts reload mid-session, settings are still safely restored.

---

## Requirements

- Unity 6000.1 or later

---

## Installation

Install via the Unity Package Manager using the Git URL:

1. Open **Window → Package Manager**
2. Click the **+** button and select **Add package from git URL...**
3. Enter:
   ```
   https://github.com/mhzeGit/unity-fast-play-button.git
   ```
4. Click **Add**

---

## Usage

Once installed, a **Fast Play** button appears on the right side of the Unity toolbar, next to the default Play controls.

| State | Button |
|---|---|
| Editor idle | Fast Play button (normal) |
| Fast playing | Green Stop button |
| Playing normally | Fast Play button disabled |

Click **Fast Play** to start, click the green **Stop** button to exit.

---

## License

MIT
