from reportlab.lib.pagesizes import A4, landscape
from reportlab.pdfgen import canvas
from reportlab.lib.units import mm

# Coordinates from clarky.txt — already in mm at 150mm chord
upper = [
    (0.0, 0.0), (0.075, 0.35085), (0.15, 0.559065), (0.3, 0.870375),
    (0.6, 1.33857), (1.2, 2.06025), (1.8, 2.678715), (3.0, 3.806025),
    (4.5, 4.953225), (6.0, 5.869245), (7.5, 6.641295), (9.0, 7.313565),
    (12.0, 8.46462), (15.0, 9.449715), (18.0, 10.29306), (21.0, 11.0154),
    (24.0, 11.635605), (27.0, 12.160305), (30.0, 12.5880300), (33.0, 12.921495),
    (36.0, 13.17462), (39.0, 13.3626), (42.0, 13.50024), (45.0, 13.60206),
    (48.0, 13.677855), (51.0, 13.726185), (54.0, 13.74399), (57.0, 13.72818),
    (60.0, 13.67568), (63.0, 13.584855), (66.0, 13.457625), (69.0, 13.296405),
    (72.0, 13.10358), (75.0, 12.88158), (78.0, 12.632175), (81.0, 12.35568),
    (84.0, 12.0522), (87.0, 11.721765), (90.0, 11.364495), (93.0, 10.980825),
    (96.0, 10.57233), (99.0, 10.14069), (102.0, 9.687645), (105.0, 9.214935),
    (108.0, 8.723985), (111.0, 8.215125), (114.0, 7.688475), (117.0, 7.144215),
    (120.0, 6.58254), (123.0, 6.003675), (126.0, 5.40804), (129.0, 4.7961),
    (132.0, 4.168365), (135.0, 3.525375), (138.0, 2.86734), (141.0, 2.193585),
    (144.0, 1.50348), (145.5, 1.15302), (147.0, 0.800025), (148.5, 0.44535),
    (150.0, 0.089895),
]

lower = [
    (0.0, 0.0), (0.075, -0.7005), (0.15, -0.89127), (0.3, -1.171695),
    (0.6, -1.57689), (1.2, -2.14293), (1.8, -2.545995), (3.0, -3.040845),
    (4.5, -3.39084), (6.0, -3.678165), (7.5, -3.90678), (9.0, -4.069155),
    (12.0, -4.268925), (15.0, -4.40679), (18.0, -4.494495), (21.0, -4.53606),
    (24.0, -4.53819), (27.0, -4.50735), (30.0, -4.44984), (33.0, -4.371675),
    (36.0, -4.277715), (39.0, -4.17246), (42.0, -4.06044), (45.0, -3.946185),
    (48.0, -3.833475), (51.0, -3.72264), (54.0, -3.61305), (57.0, -3.50409),
    (60.0, -3.395115), (63.0, -3.28563), (66.0, -3.17562), (69.0, -3.065295),
    (72.0, -2.95479), (75.0, -2.844285), (78.0, -2.73393), (81.0, -2.62371),
    (84.0, -2.51358), (87.0, -2.40348), (90.0, -2.293395), (93.0, -2.183265),
    (96.0, -2.073105), (99.0, -1.96293), (102.0, -1.852725), (105.0, -1.742535),
    (108.0, -1.632345), (111.0, -1.52217), (114.0, -1.411995), (117.0, -1.30182),
    (120.0, -1.191645), (123.0, -1.08147), (126.0, -0.971295), (129.0, -0.86112),
    (132.0, -0.750945), (135.0, -0.64077), (138.0, -0.530595), (141.0, -0.42042),
    (144.0, -0.310245), (145.5, -0.255165), (147.0, -0.200085), (148.5, -0.14499),
    (150.0, -0.089895),
]

CHORD = 150.0  # mm — already scaled

# Page setup: A4 portrait
page_w, page_h = A4
margin_left = 20 * mm
origin_x = margin_left
origin_y = page_h / 2 + 2 * mm  # chord line slightly above center

output_path = "/mnt/user-data/outputs/clarky_15cm_template.pdf"
c = canvas.Canvas(output_path, pagesize=A4)

def to_pt(x_mm, y_mm):
    return origin_x + x_mm * mm, origin_y + y_mm * mm

# --- Grid (10mm) ---
c.setLineWidth(0.3)
c.setStrokeColorRGB(0.88, 0.88, 0.88)
x = origin_x
while x <= origin_x + CHORD * mm + 1:
    c.line(x, origin_y - 30*mm, x, origin_y + 22*mm)
    x += 10 * mm
y = origin_y - 28*mm
while y <= origin_y + 20*mm:
    c.line(origin_x - 3*mm, y, origin_x + CHORD*mm + 3*mm, y)
    y += 10 * mm

# --- Chord line (dashed) ---
c.setLineWidth(0.7)
c.setStrokeColorRGB(0.55, 0.55, 0.55)
c.setDash(4, 3)
c.line(origin_x, origin_y, origin_x + CHORD*mm, origin_y)
c.setDash()

# --- % chord tick marks ---
c.setLineWidth(0.6)
c.setStrokeColorRGB(0.55, 0.55, 0.55)
c.setFillColorRGB(0.4, 0.4, 0.4)
c.setFont("Helvetica", 6)
for pct in range(0, 110, 10):
    tx = origin_x + pct/100 * CHORD * mm
    c.line(tx, origin_y - 2*mm, tx, origin_y + 2*mm)
    c.drawCentredString(tx, origin_y + 3*mm, f"{pct}%")

# --- Airfoil profile ---
c.setLineWidth(1.5)
c.setStrokeColorRGB(0, 0, 0)
c.setFillColorRGB(0, 0, 0)

# Upper surface
path = c.beginPath()
px, py = to_pt(*upper[0])
path.moveTo(px, py)
for pt in upper[1:]:
    path.lineTo(*to_pt(*pt))
c.drawPath(path, stroke=1, fill=0)

# Lower surface
path = c.beginPath()
px, py = to_pt(*lower[0])
path.moveTo(px, py)
for pt in lower[1:]:
    path.lineTo(*to_pt(*pt))
c.drawPath(path, stroke=1, fill=0)

# --- Dimension line ---
dim_y = origin_y - 10*mm
c.setLineWidth(0.7)
c.setStrokeColorRGB(0.3, 0.3, 0.3)
c.setFillColorRGB(0.3, 0.3, 0.3)
c.line(origin_x, dim_y, origin_x + CHORD*mm, dim_y)
c.line(origin_x, dim_y - 2*mm, origin_x, dim_y + 2*mm)
c.line(origin_x + CHORD*mm, dim_y - 2*mm, origin_x + CHORD*mm, dim_y + 2*mm)
c.setFont("Helvetica", 8)
c.drawCentredString(origin_x + CHORD/2*mm, dim_y - 5*mm, "150 mm  (chord)")

# Max thickness annotation
tx = origin_x + 0.36 * CHORD * mm  # ~36% chord where thickness peaks
ty_top = origin_y + 13.74 * mm
c.setLineWidth(0.5)
c.setStrokeColorRGB(0.5, 0.5, 0.5)
c.setDash(2, 2)
c.line(tx, origin_y, tx, ty_top)
c.setDash()
c.setFont("Helvetica", 7)
c.setFillColorRGB(0.35, 0.35, 0.35)
c.drawString(tx + 1.5*mm, origin_y + 6*mm, "max t = 13.7 mm")
c.drawString(tx + 1.5*mm, origin_y + 3.5*mm, "(at 36% chord)")

# --- Title ---
c.setFillColorRGB(0, 0, 0)
c.setFont("Helvetica-Bold", 14)
c.drawString(margin_left, page_h - 18*mm, "Clark Y Airfoil — Build Template")
c.setFont("Helvetica", 9)
c.drawString(margin_left, page_h - 25*mm,
    "Chord: 150 mm    Max thickness: 13.7 mm (9.1%) at 36% chord    Source: clarky.txt (Selig UIUC database)")
c.setFont("Helvetica-Oblique", 8)
c.setFillColorRGB(0.4, 0.4, 0.4)
c.drawString(margin_left, page_h - 31*mm,
    "Print at 100% / actual size — no scaling. Verify chord line = 150 mm with a ruler before cutting.")

# --- Instruction box ---
c.setStrokeColorRGB(0.75, 0.75, 0.75)
c.setFillColorRGB(0.97, 0.97, 0.97)
box_x = margin_left
box_y = 12*mm
box_w = 200*mm
box_h = 24*mm
c.roundRect(box_x, box_y, box_w, box_h, 3*mm, stroke=1, fill=1)
c.setFillColorRGB(0, 0, 0)
c.setFont("Helvetica-Bold", 8)
c.drawString(box_x + 4*mm, box_y + box_h - 7*mm, "How to build from this template:")
c.setFont("Helvetica", 7.5)
lines = [
    "1. Cut out the profile or trace the outline onto your rib material.",
    "2. The lower surface curves slightly downward near the leading edge (first ~24mm), then rises back to the chord line.",
    "3. From x = 27mm onward the lower surface is nearly flat — use a straight edge along the baseline when building.",
    "4. Leading edge is a tight curve — sand or cut carefully around x = 0.",
]
for i, line in enumerate(lines):
    c.drawString(box_x + 4*mm, box_y + box_h - 14*mm - i*5*mm, line)

c.save()
print("Done:", output_path)
