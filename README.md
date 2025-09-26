```markdown
# YTScript — Sony Vegas 12–14 effects toolkit

This repository contains:
- A ready-to-edit Sony Vegas script (C#) that implements a set of "YouTube Poop" / meme-style effects:
  - Stutter Loop
  - Stutter Loop Plus (variable repeats + pitch/time variants)
  - Scrambling / Random Chopping
  - Dance / Rave quick-cut effect
  - Reverse clips
  - Meme Replacements (insert text + GIF/sound placeholders)
  - Stare Down / Mysterious Zooms (simple zoom and crop automation)
  - Ear-Rape (loud transient boosting)
  - Add audio bleep/censor
  - Add audio random sound (sample insert)
  - Panning automation (left-right)
  - Random Tech Text overlays
  - Random "Spadinner" (random weird sample insertion) — configurable folder of samples

How to use
1. Backup your project before running the script.
2. Put the C# script file `ytpscript.cs` into your Vegas Scripts directory:
   - Typically: `C:\Program Files\Vegas\Script Menu\` or `%AppData%\Sony\Vegas\Scripts\`
   - Or compile it into a DLL if you prefer (the script uses ScriptPortal.Vegas API).
3. Launch Vegas and run the script from the Scripts menu.
4. The script will present a small UI to set parameters and then process the selected event(s) or the entire track (depending on options).

Notes
- This is a practical starting implementation. Some Vegas API method/property names can change across versions or require small adjustments depending on whether you target .NET 3.5 or higher. Comments in the script explain where to adjust.
- You should keep a copy of your project; effects (especially splitting, inserting events, reverse audio) modify the project timeline.

License
- MIT — see LICENSE file.
```