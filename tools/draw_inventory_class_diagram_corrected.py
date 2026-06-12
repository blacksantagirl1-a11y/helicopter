from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


OUT = Path(__file__).resolve().parents[1] / "inventory_class_diagram_corrected.png"

W, H = 1400, 900
BG = (255, 255, 255)
BOX_FILL = (248, 251, 255)
HEADER_FILL = (238, 245, 254)
BLUE = (32, 82, 165)
LINE = (50, 50, 50)
DASH = (0, 76, 200)
TEXT = (0, 0, 0)


def font(size: int, bold: bool = False):
    paths = [
        "C:/Windows/Fonts/arialbd.ttf" if bold else "C:/Windows/Fonts/arial.ttf",
        "C:/Windows/Fonts/segoeuib.ttf" if bold else "C:/Windows/Fonts/segoeui.ttf",
    ]
    for path in paths:
        if Path(path).exists():
            return ImageFont.truetype(path, size)
    return ImageFont.load_default()


TITLE = font(22, True)
BODY = font(17)
SMALL = font(16)


class Box:
    def __init__(self, name: str, x: int, y: int, w: int, h: int, lines: list[str]):
        self.name = name
        self.x = x
        self.y = y
        self.w = w
        self.h = h
        self.lines = lines

    @property
    def right(self) -> int:
        return self.x + self.w

    @property
    def bottom(self) -> int:
        return self.y + self.h

    @property
    def cx(self) -> int:
        return self.x + self.w // 2

    @property
    def cy(self) -> int:
        return self.y + self.h // 2


boxes = {
    "Interactable": Box(
        "Interactable",
        80,
        55,
        360,
        215,
        ["+CanInteract", "+PromptText", "+BaseInteract(PlayerUI)", "#Interact()", "#PresentInteraction(PlayerUI)"],
    ),
    "InventoryPickup": Box(
        "InventoryPickup",
        520,
        55,
        400,
        215,
        ["-InventoryItemDefinition itemDefinition", "-int amount", "#Interact()"],
    ),
    "Door": Box("Door", 80, 380, 260, 160, ["+Open()", "+Close()", "+Interact()"]),
    "CampingCookingInteractable": Box(
        "CampingCookingInteractable",
        390,
        380,
        360,
        160,
        ["+Interact()", "+EatCookedFood()", "+ReportQuestProgress()"],
    ),
    "MeatPickup": Box("MeatPickup", 810, 380, 250, 160, ["#Interact()"]),
    "TreeLogPickup": Box("TreeLogPickup", 1110, 380, 250, 160, ["#Interact()"]),
    "PlayerInventory": Box(
        "PlayerInventory",
        480,
        660,
        420,
        210,
        [
            "+SlotCount",
            "+Slots",
            "+InventoryChanged",
            "+FeedbackRequested",
            "+ItemAdded",
            "+TryAddItem(...)",
            "+TryConsumeSlot(...)",
            "+TryUseSlot(...)",
        ],
    ),
    "InventoryItemDefinition": Box(
        "InventoryItemDefinition",
        60,
        660,
        340,
        210,
        ["+ItemId", "+DisplayName", "+Description", "+Icon", "+MaxStack", "+TryUse(...)"],
    ),
    "InventoryUIController": Box(
        "InventoryUIController",
        980,
        660,
        340,
        190,
        ["+IsInventoryOpen", "+ToggleInventory()", "+SetInventoryOpen(bool)"],
    ),
}


def draw_box(draw: ImageDraw.ImageDraw, b: Box):
    draw.rectangle([b.x, b.y, b.right, b.bottom], fill=BOX_FILL, outline=BLUE, width=3)
    header_h = 48
    draw.rectangle([b.x, b.y, b.right, b.y + header_h], fill=HEADER_FILL, outline=BLUE, width=3)
    tw = draw.textlength(b.name, font=TITLE)
    draw.text((b.x + (b.w - tw) / 2, b.y + 11), b.name, fill=TEXT, font=TITLE)
    y = b.y + header_h + 18
    for line in b.lines:
        draw.text((b.x + 18, y), line, fill=TEXT, font=SMALL)
        y += 23


def triangle_head(draw: ImageDraw.ImageDraw, x: int, y: int, direction: str):
    if direction == "up":
        pts = [(x, y), (x - 11, y + 20), (x + 11, y + 20)]
    elif direction == "left":
        pts = [(x, y), (x + 20, y - 11), (x + 20, y + 11)]
    else:
        raise ValueError(direction)
    draw.polygon(pts, outline=LINE, fill=BG)
    draw.line([pts[0], pts[1], pts[2], pts[0]], fill=LINE, width=2)


def line(draw: ImageDraw.ImageDraw, pts: list[tuple[int, int]], color=LINE, width=2):
    draw.line(pts, fill=color, width=width, joint="curve")


def dashed_line(draw: ImageDraw.ImageDraw, pts: list[tuple[int, int]], color=DASH, width=2, dash=12):
    for (x1, y1), (x2, y2) in zip(pts, pts[1:]):
        if x1 == x2:
            step = dash if y2 >= y1 else -dash
            y = y1
            draw_on = True
            while (step > 0 and y < y2) or (step < 0 and y > y2):
                y_next = max(min(y + step, y2), y2) if step < 0 else min(y + step, y2)
                if draw_on:
                    draw.line([(x1, y), (x2, y_next)], fill=color, width=width)
                y = y_next
                draw_on = not draw_on
        elif y1 == y2:
            step = dash if x2 >= x1 else -dash
            x = x1
            draw_on = True
            while (step > 0 and x < x2) or (step < 0 and x > x2):
                x_next = max(min(x + step, x2), x2) if step < 0 else min(x + step, x2)
                if draw_on:
                    draw.line([(x, y1), (x_next, y2)], fill=color, width=width)
                x = x_next
                draw_on = not draw_on
        else:
            draw.line([(x1, y1), (x2, y2)], fill=color, width=width)


def arrow_head(draw: ImageDraw.ImageDraw, x: int, y: int, direction: str, color=LINE):
    if direction == "down":
        pts = [(x, y), (x - 9, y - 15), (x + 9, y - 15)]
    elif direction == "right":
        pts = [(x, y), (x - 15, y - 9), (x - 15, y + 9)]
    elif direction == "left":
        pts = [(x, y), (x + 15, y - 9), (x + 15, y + 9)]
    else:
        raise ValueError(direction)
    draw.polygon(pts, fill=color)


def label(draw: ImageDraw.ImageDraw, text: str, x: int, y: int, color=LINE):
    pad = 5
    bbox = draw.textbbox((x, y), text, font=BODY)
    draw.rectangle([bbox[0] - pad, bbox[1] - pad, bbox[2] + pad, bbox[3] + pad], fill=BG)
    draw.text((x, y), text, fill=color, font=BODY)


img = Image.new("RGB", (W, H), BG)
d = ImageDraw.Draw(img)

for b in boxes.values():
    draw_box(d, b)

# Inheritance: Door, CampingCookingInteractable, InventoryPickup -> Interactable.
line(d, [(210, 380), (210, 325), (260, 325), (260, 290)])
triangle_head(d, 260, 270, "up")

line(d, [(570, 380), (570, 325), (260, 325)])

line(d, [(720, 270), (720, 325), (260, 325)])

# Inheritance: MeatPickup, TreeLogPickup -> InventoryPickup.
line(d, [(935, 380), (935, 320), (720, 320), (720, 290)])
triangle_head(d, 720, 270, "up")

line(d, [(1235, 380), (1235, 320), (720, 320)])

# InventoryPickup references InventoryItemDefinition.
dashed_line(d, [(920, 145), (1270, 145), (1270, 610), (400, 610), (400, 765)], color=DASH)
arrow_head(d, 400, 765, "down", DASH)
label(d, "itemDefinition", 1015, 116, DASH)

# InventoryPickup adds item to PlayerInventory.
line(d, [(720, 270), (720, 600), (690, 600), (690, 660)])
arrow_head(d, 690, 660, "down")
label(d, "thêm vật phẩm", 730, 570)

# PlayerInventory uses item data.
dashed_line(d, [(480, 790), (400, 790)], color=DASH)
arrow_head(d, 400, 790, "left", DASH)
label(d, "dữ liệu item trong slot", 245, 754, DASH)

# PlayerInventory notifies UI.
dashed_line(d, [(900, 765), (980, 765)], color=DASH)
arrow_head(d, 980, 765, "right", DASH)
label(d, "cập nhật UI", 920, 730, DASH)

img.save(OUT)
print(OUT)
