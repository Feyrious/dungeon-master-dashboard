# Tile images

Put your own images here to replace the built-in illustrations on the game system tiles.
(Uploading an image in a tile's settings on the overview is easier and also works for
worlds; an uploaded image takes precedence over this folder.) Name each file after the
system's id:

| Tile | File name |
|---|---|
| Dungeons & Dragons 5e | `dnd5e.jpg` |
| Fabula Ultima | `fabula-ultima.jpg` |
| The Expanse | `the-expanse.jpg` |
| KULT: Divinity Lost | `kult.jpg` |
| Tails of Equestria | `tails-of-equestria.jpg` |

`.webp`, `.jpg`, `.jpeg`, `.png` and `.avif` all work. Landscape images around 800×480
look best; the bottom part is darkened so the tile text stays readable.

Images in this folder are git-ignored (they may be publisher art you own) and are only
served to signed-in users. The folder is mounted read-only into the web container, so new
images show up after a page refresh, with no rebuild needed.
