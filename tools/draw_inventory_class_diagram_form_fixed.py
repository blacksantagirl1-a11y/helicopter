from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


OUT = Path(__file__).resolve().parents[1] / "inventory_class_diagram_form_fixed.png"

W, H = 1600, 1080
BG = (255, 255, 255)
BOX_FILL = (248, 251, 255)
HEADER_FILL = (238, 245, 254)
BLUE = (32, 82, 165)
BLACK = (40, 40, 40)
DASH = (0, 83, 205)
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
    "Interactable": Box(
        "Interactable",
        520,
        45,
        430,
        215,
        ["+CanInteract", "+PromptText", "+BaseInteract(PlayerUI)", "#Interact()", "#PresentInteraction(PlayerUI)"],
    ),
    "Door": Box("Door", 70, 365, 300, 160, ["+Open()", "+Close()", "+Interact()"]),
    "CampingCookingInteractable": Box(
        "CampingCookingInteractable",
        430,
        365,
        410,
        160,
        ["+Interact()", "+EatCookedFood()", "+ReportQuestProgress()"],
    ),
    "InventoryPickup": Box(
        "InventoryPickup",
        1035,
        365,
        440,
        165,
        ["-InventoryItemDefinition itemDefinition", "-int amount", "#Interact()"],
    ),
    "MeatPickup": Box("MeatPickup", 1010, 645, 250, 120, ["#Interact()"]),
    "TreeLogPickup": Box("TreeLogPickup", 1320, 645, 250, 120, ["#Interact()"]),
    "InventoryItemDefinition": Box(
        "InventoryItemDefinition",
        80,
        805,
        385,
        220,
        ["+ItemId", "+DisplayName", "+Description", "+Icon", "+MaxStack", "+TryUse(...)"],
    ),
    "PlayerInventory": Box(
        "PlayerInventory",
        610,
        805,
        390,
        220,
        ["+SlotCount", "+Slots", "+InventoryChanged", "+ItemAdded(...)", "+TryAddItem(...)", "+TryUseSlot(...)"],
    ),
    "InventoryUIController": Box(
        "InventoryUIController",
        1180,
        835,
        360,
        180,
        ["+IsInventoryOpen", "+ToggleInventory()", "+SetInventoryOpen(bool)"],
    ),
}


def draw_box(draw: ImageDraw.ImageDraw, b: Box):
    draw.rectangle([b.x, b.y, b.right, b.bottom], fill=BOX_FILL, outline=BLUE, width=3)
    header_h = 48
    draw.rectangle([b.x, b.y, b.right, b.y + header_h], fill=HEADER_FILL, outline=BLUE, width=3)
    title_w = draw.textlength(b.name, font=TITLE)
    draw.text((b.x + (b.w - title_w) / 2, b.y + 11), b.name, fill=TEXT, font=TITLE)
    y = b.y + header_h + 18
    for line in b.lines:
        draw.text((b.x + 18, y), line, fill=TEXT, font=SMALL)
        y += 23


def label(draw: ImageDraw.ImageDraw, text: str, x: int, y: int, color=DASH):
    pad = 5
    bbox = draw.textbbox((x, y), text, font=BODY)
    draw.rectangle([bbox[0] - pad, bbox[1] - pad, bbox[2] + pad, bbox[3] + pad], fill=BG)
    draw.text((x, y), text, fill=color, font=BODY)


def solid(draw: ImageDraw.ImageDraw, pts: list[tuple[int, int]], color=BLACK, width=2):
    draw.line(pts, fill=color, width=width, joint="curve")


def dashed(draw: ImageDraw.ImageDraw, pts: list[tuple[int, int]], color=DASH, width=2, dash=14, gap=8):
    for (x1, y1), (x2, y2) in zip(pts, pts[1:]):
        dx, dy = x2 - x1, y2 - y1
        dist = (dx * dx + dy * dy) ** 0.5
        if dist == 0:
            continue
        ux, uy = dx / dist, dy / dist
        pos = 0.0
        is_dash = True
        while pos < dist:
            nxt = min(pos + (dash if is_dash else gap), dist)
            if is_dash:
                draw.line([(x1 + ux * pos, y1 + uy * pos), (x1 + ux * nxt, y1 + uy * nxt)], fill=color, width=width)
            pos = nxt
            is_dash = not is_dash


def inheritance_triangle_up(draw: ImageDraw.ImageDraw, x: int, y: int):
    # Hollow triangle points to superclass above.
    pts = [(x, y), (x - 13, y + 23), (x + 13, y + 23)]
    draw.polygon(pts, fill=BG, outline=BLACK)
    draw.line([pts[0], pts[1], pts[2], pts[0]], fill=BLACK, width=2)


def arrow_head(draw: ImageDraw.ImageDraw, x: int, y: int, direction: str, color=DASH):
    if direction == "right":
        pts = [(x, y), (x - 16, y - 9), (x - 16, y + 9)]
    elif direction == "left":
        pts = [(x, y), (x + 16, y - 9), (x + 16, y + 9)]
    elif direction == "down":
        pts = [(x, y), (x - 9, y - 16), (x + 9, y - 16)]
    else:
        raise ValueError(direction)
    draw.polygon(pts, fill=color)


img = Image.new("RGB", (W, H), BG)
d = ImageDraw.Draw(img)

for box in boxes.values():
    draw_box(d, box)

# UML inheritance: child classes connect to a shared bus; hollow triangle points to parent.
inheritance_triangle_up(d, boxes["Interactable"].cx, boxes["Interactable"].bottom)
solid(d, [(boxes["Interactable"].cx, boxes["Interactable"].bottom + 23), (boxes["Interactable"].cx, 315)])
solid(d, [(220, 365), (220, 315), (boxes["Interactable"].cx, 315)])
solid(d, [(635, 365), (635, 315), (boxes["Interactable"].cx, 315)])
solid(d, [(1255, 365), (1255, 315), (boxes["Interactable"].cx, 315)])
label(d, "kế thừa Interactable", 760, 285, BLACK)

inheritance_triangle_up(d, boxes["InventoryPickup"].cx, boxes["InventoryPickup"].bottom)
solid(d, [(boxes["InventoryPickup"].cx, boxes["InventoryPickup"].bottom + 23), (boxes["InventoryPickup"].cx, 595)])
solid(d, [(1135, 645), (1135, 595), (boxes["InventoryPickup"].cx, 595)])
solid(d, [(1445, 645), (1445, 595), (boxes["InventoryPickup"].cx, 595)])
label(d, "kế thừa InventoryPickup", 1295, 565, BLACK)

# Corrected runtime/data relationships.
# InventoryPickup owns/references itemDefinition. Route around the middle, away from boxes and other arrows.
dashed(d, [(1035, 445), (910, 445), (910, 740), (270, 740), (270, 805)])
arrow_head(d, 270, 805, "down", DASH)
label(d, "itemDefinition", 760, 415, DASH)

# InventoryPickup interaction adds item into PlayerInventory.
dashed(d, [(1255, 530), (1255, 700), (805, 700), (805, 805)])
arrow_head(d, 805, 805, "down", DASH)
label(d, "Interact() -> TryAddItem()", 920, 670, DASH)

# Camping/cooking consumes or uses items from inventory.
dashed(d, [(635, 525), (635, 730), (700, 730), (700, 805)])
arrow_head(d, 700, 805, "down", DASH)
label(d, "dùng / tiêu hao vật phẩm", 445, 700, DASH)

# Inventory slots store item definitions.
dashed(d, [(610, 930), (465, 930)])
arrow_head(d, 465, 930, "left", DASH)
label(d, "slot chứa itemDefinition", 375, 895, DASH)

# Inventory notifies UI.
dashed(d, [(1000, 930), (1180, 930)])
arrow_head(d, 1180, 930, "right", DASH)
label(d, "InventoryChanged", 1045, 895, DASH)

img.save(OUT)
print(OUT)
