# Construction Occluder Walls v01

Generated: 2026-07-15

Target style: low-resolution pixel art matching the existing KyoumoMushoku muted urban construction-site props. These are wide foreground / midground occluder wall modules intended to hide characters or props behind them.

Generation:
- Built-in image generation on flat `#ff00ff` chroma-key backgrounds.
- Local chroma-key removal from border color.
- Cropped to alpha bounds, resized with nearest-neighbor, and quantized to 32 colors.
- Saved as transparent RGBA PNG.

Final transparent assets:
- `construction-occluder-wall-corrugated-metal-transparent-v01.png` - 192 x 160
- `construction-occluder-wall-plywood-hoarding-transparent-v01.png` - 192 x 160
- `construction-occluder-wall-concrete-panel-transparent-v01.png` - 192 x 160
- `construction-occluder-wall-blue-tarp-transparent-v01.png` - 192 x 160

Notes:
- `*-chromakey-v01.png` files are preserved as generated source images.
- `*-transparent-v01.png` files are the intended import/use assets.
