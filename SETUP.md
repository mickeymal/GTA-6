# Vice Bay Empire — Setup Guide (for people brand-new to Unity)

Follow these in order. Total time is about **45–60 minutes**, most of which is Unity
downloading in the background. You need ~15 GB of free disk space.

You'll end up pressing **Play** and walking around the city.

---

## Step 1 — Download this project's code

1. Go to the GitHub page for the repo: **github.com/mickeymal/gta-6**
2. Near the top-left there's a **branch dropdown** (it probably says `main`). Click it
   and choose the branch **`claude/vice-bay-stories-game-ohrsu2`**. *(This matters —
   the game is on that branch.)*
3. Click the green **`< > Code`** button → **Download ZIP**.
4. Find the downloaded `.zip` (usually in your Downloads folder), **right-click →
   Extract All / Unzip**. You now have a folder containing an **`Assets`** folder.
   Remember where this is.

*(If you know git, you can instead run
`git clone -b claude/vice-bay-stories-game-ohrsu2 https://github.com/mickeymal/gta-6`,
but the ZIP is easier.)*

---

## Step 2 — Install Unity Hub

"Unity Hub" is the launcher that manages Unity versions and projects.

1. Go to **unity.com/download**.
2. Click **Download for Windows** (or Mac). Run the installer, click through it.
3. Open **Unity Hub**. It will ask you to **sign in / create a Unity account** — make
   a free account.
4. When asked about a license, choose **Get a free Personal license** (also called
   "Unity Personal"). It's free for personal use.

---

## Step 3 — Install the Unity Editor (version 2022.3)

1. In Unity Hub, click **Installs** (left side) → **Install Editor**.
2. Under the **LTS** section pick the newest **2022.3.x** (e.g. `2022.3.62f1`).
   Click **Install**.
   - If it shows "Add modules", you can just click **Continue** — you don't need any
     extra module to *play* the game. (To make a `.exe` later you'd add "Windows Build
     Support", but skip that for now.)
3. Wait for it to download and install (this is the big one — several GB).

---

## Step 4 — Create a new empty 3D project

We'll make a blank project and then add the game's code into it.

1. In Unity Hub, click **Projects** → **New project**.
2. Make sure the Editor version at the top is your **2022.3** install.
3. Choose the template called **3D (Built-In Render Pipeline)** — it may just be called
   **"3D"** or **"3D Core"**. **Do not pick URP or HDRP.**
4. Give it a name like `ViceBay` and pick a location you'll remember.
5. Click **Create project**. Unity opens (first open takes a minute or two).

---

## Step 5 — Copy the game's code into your project

1. Open your new project's folder on disk. Inside it is an **`Assets`** folder.
2. Open the folder you unzipped in Step 1. Inside it is **its own `Assets`** folder
   containing `Scripts` and `Editor`.
3. **Copy the `Scripts` and `Editor` folders** from the unzipped `Assets` **into your
   project's `Assets` folder.**
   - So you end up with `YourProject/Assets/Scripts` and `YourProject/Assets/Editor`.
4. Switch back to the open Unity window. It will notice the new files and spend a few
   seconds **importing/compiling** (spinner in the bottom-right). Wait for it to finish.

---

## Step 6 — Check the input setting

The game uses Unity's classic input. Make sure it's enabled:

1. Top menu: **Edit → Project Settings**.
2. In the left list click **Player**.
3. Expand **Other Settings**, scroll to **Configuration → Active Input Handling**.
4. Set it to **Both** (or **Input Manager (Old)**). **Not** "Input System Package (New)".
5. If it asks to **restart the Editor**, click **Yes** and let it reopen.

---

## Step 7 — Create the game scene

The project includes a one-click scene builder.

1. Look at the **very top menu bar**. There should be a menu called **`ViceBay`**
   (next to Window/Help). *(If it's not there yet, Unity is still compiling — wait a
   few seconds, or see Troubleshooting.)*
2. Click **ViceBay → Create ViceBay_MainScene**.
3. A little popup confirms it made the scene. Click **Let's go**.

---

## Step 8 — Play!

1. Press the **Play button** — the ▶ triangle at the **top-center** of the Unity window.
2. **Click once inside the "Game" view** so it grabs your mouse (the cursor will hide
   and lock — that's normal for a third-person game).
3. The whole city builds itself and you're standing in Vice Bay. Follow the on-screen
   objective — the tutorial starts automatically.
4. To **stop**, press the ▶ Play button again. (Press **Esc** in-game to free the mouse.)

### Controls
| Do this | Keys |
|---|---|
| Move / sprint / jump | `W A S D` / hold `Shift` / `Space` |
| Look around | move the **mouse** |
| Shoot / aim / reload | **Left-click** / **Right-click** / `R` |
| Weapon wheel | hold **`Tab`** |
| Get in / out of a vehicle | walk up, press **`F`** |
| Fly (in plane/heli) | `Space` up, `Ctrl` down |
| Interact — rob, mug, start mission | **`E`** (aim a weapon first to rob/mug) |
| Phone (Dark Web, businesses, map stuff) | **`P`** |

**First thing to try:** follow the glowing marker — the tutorial teaches you
everything (walking, driving, shooting, robbing a store).

---

## Troubleshooting

**The `ViceBay` menu isn't in the menu bar.**
Unity is still compiling, or there's a script error. Look at the **Console**
(menu: **Window → General → Console**). If you see **red errors**, copy them and send
them to me — I'll fix them. If there are no red errors, wait ~30 seconds and check the
menu bar again.

**Everything is bright pink / magenta.**
That means you accidentally made a URP/HDRP project. Easiest fix: make a new project
using the plain **3D (Built-In)** template (Step 4) and copy the code in again.

**The mouse won't move the camera / nothing responds to keys.**
Click once inside the **Game** view first. Also double-check Step 6 (Active Input
Handling must be "Both" or "Input Manager (Old)").

**It runs but looks like plain boxes and capsules.**
That's expected — the world is made of placeholder shapes on purpose so it runs with
zero art. It's fully playable; nicer models can be swapped in later.

**I want an actual `.exe` file.**
After the game runs in the Editor, install "Windows Build Support" for your 2022.3
version in Unity Hub, then use the menu **ViceBay → Build → Windows (.exe)**. See the
Build section of `README.md`.

---

## What you should see when it works

- A stats bar (health, cash, ViceCoin, wanted stars) and a **minimap** in the corners.
- A **mission banner** at the top with your current objective, and a **gold beacon**
  in the world marking where to go.
- Cars driving around, people walking, a bank and a store you can rob, boats at the
  harbor, and a plane + helicopter at the airport.

Have fun. If anything throws an error, paste the red Console text to me and I'll sort it.
