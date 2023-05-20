# EndListFactions
My very first Town of Salem mod that I never published. This mod's features have been dissolved into other existing mods, making it obsolete.
The code is being published for preservation and if anyone wishes to improve it in their own way.

The name of this mod derives from the fact that I wanted to kinda overhaul the end list screen to work better, by listing off all the winners, representing factions and also differentiating TTs.

# Notice
⚠️ **This mod is functionally obsolete.** I don't recommend trying to play with it, as it is untested and will conflict with BetterTOS / QOLMod / Improved Water Wheel mod as all of them implement some of what this mod adds.

Additionally, this mod's code may contain traces of the Improved Water Wheel mod in its early stages. This is because I actually started working on that mod inside of this project, as I was thinking of releasing everything at once.
Later I moved the code to a separate project because I wanted to release that mod independently.
I cut out the code of the Improved Water Wheel mod, but I never tested whether it worked, so you may find traces of this code or in the worst case scenario, the mod might not even work.

# License
EndListFactions is licensed under **[GNU Lesser General Public License version 3](https://www.gnu.org/licenses/lgpl-3.0.txt)** (shortened to **LGPLv3**).
You are free to do anything you want, as long as it falls under the terms of this license.

Even though this mod is open-source, due to the nature of Town of Salem modding, I will not be providing any support on how to do so (except explain how my mods work). You're on your own.

# Features
- **TT icons are tinted. TTs names in the end screen are appended with 'TT'.** This is one of my first additions to the mod (along with the .x% shown on roles). I even coded a special (sort-of) algorithm that figured out who the TT was, if the TT never died (because your client couldn't have known who it was then). Sometime later I gave the code of the TT tint to Tuba, which then later got integrated into BetterTOS... though it doesn't seem to work very well in there :/
- **Decimals shown on role odds.** Another small feature, this later became part of the Improved Water Wheel mod. Instead of showing whole numbers, which I thought weren't representative enough, the water wheel now showed one decimal of each percentage, ex. `6.7%`. One of the first things I modded into the game.
- **Day/Night headers.** I had a lot of ideas on improving and overhauling ToS chat to be a lot more readable and easy to navigate. While I thought of overhauling it later (after IWW mod), I added this small thing, because it was frustrating to me how this wasn't in the base game. This was also a feature the code of which I gave to Tuba. Tuba put it into his BetterTOS mod, with which the mod released. However much later Tuba ended up giving that code to Curtis (apparently Curtis modified some functionality according to himself. I don't know what he changed), so it ultimately ended up being a part of QOLMod.
