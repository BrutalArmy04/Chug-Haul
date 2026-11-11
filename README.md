# Chug & Haul

A low-poly 3D train tycoon. Buy railway lines between cities, put trains on them, and earn coins from every delivery to grow your network.

> **Early prototype.** Chug & Haul is a university team project in active development. What you can play today is a small test map: buying lines, watching trains run and earning coins. Cargo, wagons, train upgrades and a full map are still to come.

## What you can do right now

- **Explore the map** with a tilted camera you can pan and zoom.
- **Buy a line** between two cities. Each line has a price, and you start with 50 coins.
- **Watch your train** shuttle back and forth along the line you bought.
- **Earn coins** every time a train reaches the end of its line. Longer lines pay more.

The test map has four cities (Bucharest, Brașov, Sibiu and Cluj) and four lines to buy.

## Controls

| Action | Input |
|---|---|
| Pan the camera | `W` `A` `S` `D` or arrow keys, or drag with the right or middle mouse button |
| Zoom | Mouse scroll wheel |
| Highlight a city or line | Hover with the mouse |
| Buy a line | Left-click the faded track between two cities |

Unbought lines show a faded track. Once you buy one, the track turns solid and a train starts running on it. Your coin balance is shown on screen, with a popup each time you spend or earn.

## How to play it

There is no downloadable build yet. To try the prototype, open the project in Unity:

1. Install **Unity 6000.6.4f1** through Unity Hub. Other versions are not supported.
2. Install [Git LFS](https://git-lfs.com/), then clone the repository:
   ```
   git lfs install
   git clone https://github.com/BrutalArmy04/Chug-Haul.git
   ```
3. Add the cloned folder in Unity Hub and open it. The first import takes a few minutes.
4. In the Project window, open **`Assets/Scenes/matei`** and press **Play**.

## Coming next

- Cargo types (passengers, mail, food and more) and wagons you choose per trip
- Train physics: heavier trains accelerate and brake more slowly
- A larger map with more cities and industries
- New trains and upgrades bought with coins, unlocked as you progress
- Saving and loading, music and sound effects

## Built with

Unity 6 (6000.6.4f1) with the Universal Render Pipeline, written in C#.

## Credits

3D models by [Kenney](https://kenney.nl) (CC0) and [Quaternius](https://poly.pizza/u/Quaternius) (CC0). Map of Romania by Andrein on [Wikimedia Commons](https://commons.wikimedia.org/wiki/File:Blank_map_of_Romania.svg) (public domain).

Sound effects and music from [Freesound](https://freesound.org) and [OpenGameArt](https://opengameart.org). The following are used under Creative Commons Attribution licences:

- *Canadian and US train horn synthesized* by chungus43A (CC BY 4.0)
- *steam train horn 02* by eliasheuninck (CC BY 4.0)
- *TRNSteam_Portland Holiday Express Train Passby* by tim.kahn (CC BY 4.0)
- *Bossa Nova Remix* by migfus20, congusbongus and FrancisLeeMusic (CC BY 3.0 / 4.0)

The full list of assets, sources and licences is in [`Assets/Art/ArtSource/CREDITS.md`](Assets/Art/ArtSource/CREDITS.md).
