from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


OUT = Path(__file__).resolve().parents[1] / "player_creature_resource_diagram.png"

W, H = 1600, 980
BG = (255, 255, 255)
BOX_FILL = (248, 251, 255)
HEADER_FILL = (238, 245, 254)
BLUE = (32, 82, 165)
BLACK = (42, 42, 42)
CALL = (0, 88, 190)
TEXT = (0, 0, 0)


def get_font(size: int, bold: bool = False):
    candidates = [
        "C:/Windows/Fonts/arialbd.ttf" if bold else "C:/Windows/Fonts/arial.ttf",
        "C:/Windows/Fonts/segoeuib.ttf" if bold else "C:/Windows/Fonts/segoeui.ttf",
    ]
    for candidate in candidates:
        if Path(candidate).exists():
            return ImageFont.truetype(candidate, size)
    return ImageFont.load_default()


TITLE = get_font(22, True)
BODY = get_font(17)
SMALL = get_font(16)


class Box:
    def __init__(self, name: str, x: int, y: int, w: int, h: int, lines: list[str]):
        self.name = name
        self.x = x
        self.y = y
        self.w = w
        self.h = h
        self.lines = lines

    @property
    def right(self):
        return self.x + self.w

    @property
    def bottom(self):
        return self.y + self.h

    @property
    def cx(self):
        return self.x + self.w // 2

    @property
    def cy(self):
        return self.y + self.h // 2


boxes = {
    "PlayerMovement": Box(
        "PlayerMovement",
        80,
        80,
        330,
        170,
        ["+Move()", "+Run()", "+Jump()", "+SetInputEnabled(bool)"],
    ),
    "PlayerLook": Box(
        "PlayerLook",
        470,
        80,
        310,
        170,
        ["+Look()", "+SetSensitivity()", "+LockLook(bool)"],
    ),
    "ActionScript": Box(
        "ActionScript",
        850,
        80,
        350,
        185,
        ["+ToggleAxe()", "+Attack()", "+AttackPerformed", "-attackImpactDelay"],
    ),
    "Stamina": Box(
        "Stamina",
        1260,
        80,
        280,
        185,
        ["+CurrentStamina", "+Consume(amount)", "+Recover()", "+CanUse(amount)"],
    ),
    "CuttingTreeSystem": Box(
        "CuttingTreeSystem",
        170,
        390,
        360,
        185,
        ["+CheckTreeInRange()", "+RegisterHit()", "+RemoveTreeInstance()", "+SpawnWoodLog()"],
    ),
    "FishingRob": Box(
        "FishingRob",
        620,
        390,
        330,
        185,
        ["+StartFishing()", "+HookFish()", "+ExitFishing()", "-biteDelayRange"],
    ),
    "Boar": Box(
        "Boar",
        1050,
        390,
        330,
        185,
        ["+TakeDamage()", "+Roam()", "+Die()", "+DropMeat()"],
    ),
    "TreeLogPickup": Box(
        "TreeLogPickup",
        110,
        735,
        300,
        145,
        ["#Interact()", "+itemDefinition: WoodLog"],
    ),
    "FishItem": Box(
        "FishItem",
        520,
        735,
        280,
        145,
        ["+itemId: river_fish", "+MaxStack"],
    ),
    "MeatPickup": Box(
        "MeatPickup",
        910,
        735,
        300,
        145,
        ["#Interact()", "+itemDefinition: Meat"],
    ),
    "PlayerInventory": Box(
        "PlayerInventory",
        1260,
        720,
        310,
        175,
        ["+TryAddItem(...)", "+TryConsumeSlot(...)", "+ItemAdded"],
    ),
}


def draw_box(draw: ImageDraw.ImageDraw, b: Box):
    draw.rectangle([b.x, b.y, b.right, b.bottom], fill=BOX_FILL, outline=BLUE, width=3)
    header_h = 48
    draw.rectangle([b.x, b.y, b.right, b.y + header_h], fill=HEADER_FILL, outline=BLUE, width=3)
    tw = draw.textlength(b.name, font=TITLE)
    draw.text((b.x + (b.w - tw) / 2, b.y + 11), b.name, fill=TEXT, font=TITLE)
    y = b.y + header_h + 18
    for item in b.lines:
        draw.text((b.x + 18, y), item, fill=TEXT, font=SMALL)
        y += 23


def label(draw: ImageDraw.ImageDraw, text: str, x: int, y: int, color=CALL):
    pad = 5
    bbox = draw.textbbox((x, y), text, font=BODY)
    draw.rectangle([bbox[0] - pad, bbox[1] - pad, bbox[2] + pad, bbox[3] + pad], fill=BG)
    draw.text((x, y), text, fill=color, font=BODY)


def solid(draw: ImageDraw.ImageDraw, pts: list[tuple[int, int]], color=BLACK, width=2):
    draw.line(pts, fill=color, width=width, joint="curve")


def dashed(draw: ImageDraw.ImageDraw, pts: list[tuple[int, int]], color=CALL, width=2, dash=14, gap=8):
    for (x1, y1), (x2, y2) in zip(pts, pts[1:]):
        dx, dy = x2 - x1, y2 - y1
        dist = (dx * dx + dy * dy) ** 0.5
        if dist == 0:
            continue
        ux, uy = dx / dist, dy / dist
        pos = 0.0
        draw_on = True
        while pos < dist:
            nxt = min(pos + (dash if draw_on else gap), dist)
            if draw_on:
                draw.line([(x1 + ux * pos, y1 + uy * pos), (x1 + ux * nxt, y1 + uy * nxt)], fill=color, width=width)
            pos = nxt
            draw_on = not draw_on


def arrow_head(draw: ImageDraw.ImageDraw, x: int, y: int, direction: str, color=CALL):
    if direction == "right":
        pts = [(x, y), (x - 16, y - 9), (x - 16, y + 9)]
    elif direction == "left":
        pts = [(x, y), (x + 16, y - 9), (x + 16, y + 9)]
    elif direction == "down":
        pts = [(x, y), (x - 9, y - 16), (x + 9, y - 16)]
    elif direction == "up":
        pts = [(x, y), (x - 9, y + 16), (x + 9, y + 16)]
    else:
        raise ValueError(direction)
    draw.polygon(pts, fill=color)


img = Image.new("RGB", (W, H), BG)
d = ImageDraw.Draw(img)

for box in boxes.values():
    draw_box(d, box)

# Player control relationships.
dashed(d, [(410, 165), (470, 165)])
arrow_head(d, 470, 165, "right")
label(d, "camera / hướng nhìn", 415, 130)

dashed(d, [(1200, 175), (1260, 175)])
arrow_head(d, 1260, 175, "right")
label(d, "tiêu hao stamina", 1190, 140)

# Action dispatches into gameplay modules.
dashed(d, [(1025, 265), (1025, 325), (350, 325), (350, 390)])
arrow_head(d, 350, 390, "down")
label(d, "AttackPerformed", 560, 292)

dashed(d, [(1025, 265), (1025, 330), (1215, 330), (1215, 390)])
arrow_head(d, 1215, 390, "down")
label(d, "đánh trúng sinh vật", 1225, 325)

# Stamina also affects fishing.
dashed(d, [(1400, 265), (1400, 345), (785, 345), (785, 390)])
arrow_head(d, 785, 390, "down")
label(d, "kiểm tra / trừ stamina", 980, 315)

# Gameplay produces resources.
dashed(d, [(350, 575), (350, 650), (260, 650), (260, 735)])
arrow_head(d, 260, 735, "down")
label(d, "sinh gỗ", 275, 635)

dashed(d, [(785, 575), (785, 650), (660, 650), (660, 735)])
arrow_head(d, 660, 735, "down")
label(d, "thêm cá", 675, 635)

dashed(d, [(1215, 575), (1215, 650), (1060, 650), (1060, 735)])
arrow_head(d, 1060, 735, "down")
label(d, "rơi thịt", 1075, 635)

# Resources go to inventory.
dashed(d, [(410, 805), (1260, 805)])
arrow_head(d, 1260, 805, "right")
label(d, "nhặt / thêm vào túi đồ", 735, 770)

dashed(d, [(800, 805), (1260, 805)])
arrow_head(d, 1260, 805, "right")

dashed(d, [(1210, 805), (1260, 805)])
arrow_head(d, 1260, 805, "right")

img.save(OUT)
print(OUT)
