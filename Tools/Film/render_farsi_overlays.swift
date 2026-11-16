import AppKit
import Foundation

let output = URL(fileURLWithPath: CommandLine.arguments[1], isDirectory: true)
try FileManager.default.createDirectory(at: output, withIntermediateDirectories: true)
let width = 1280, height = 720
func rect(_ x: CGFloat, _ y: CGFloat, _ w: CGFloat, _ h: CGFloat) -> NSRect {
    NSRect(x: x, y: CGFloat(height) - y - h, width: w, height: h)
}
func color(_ r: CGFloat, _ g: CGFloat, _ b: CGFloat, _ a: CGFloat = 1) -> NSColor {
    NSColor(calibratedRed: r, green: g, blue: b, alpha: a)
}
let ivory = color(0.96, 0.945, 0.91)
let cyan = color(0.51, 0.86, 0.89)
func text(_ value: String, _ area: NSRect, _ size: CGFloat, _ bold: Bool = false, _ ink: NSColor = ivory) {
    let paragraph = NSMutableParagraphStyle()
    paragraph.alignment = .right
    paragraph.baseWritingDirection = .rightToLeft
    paragraph.lineBreakMode = .byClipping
    let font = NSFont(name: bold ? "Arial-BoldMT" : "ArialMT", size: size) ?? NSFont.systemFont(ofSize: size)
    (value as NSString).draw(in: area, withAttributes: [
        .font: font, .foregroundColor: ink, .paragraphStyle: paragraph
    ])
}
func render(_ name: String, _ draw: () -> Void) throws {
    let bitmap = NSBitmapImageRep(bitmapDataPlanes: nil, pixelsWide: width, pixelsHigh: height,
        bitsPerSample: 8, samplesPerPixel: 4, hasAlpha: true, isPlanar: false,
        colorSpaceName: .deviceRGB, bytesPerRow: 0, bitsPerPixel: 0)!
    NSGraphicsContext.saveGraphicsState()
    NSGraphicsContext.current = NSGraphicsContext(bitmapImageRep: bitmap)
    NSColor.clear.setFill()
    NSRect(x: 0, y: 0, width: width, height: height).fill(using: .copy)
    draw()
    NSGraphicsContext.restoreGraphicsState()
    try bitmap.representation(using: .png, properties: [:])!.write(to: output.appendingPathComponent(name + ".png"))
}
try render("title_fa") {
    color(0.027, 0.055, 0.078, 0.69).setFill()
    NSRect(x: 0, y: 0, width: width, height: height).fill()
    text("وارلاین / رلهٔ گسسته", rect(90, 188, 1096, 48), 25, true, color(0.79, 0.83, 0.84))
    cyan.setFill(); rect(1127, 245, 59, 4).fill()
    text("نخستین تماس", rect(90, 268, 1096, 100), 68, true)
    text("قسمت ۰۱", rect(90, 362, 1096, 58), 32)
    text("فصل اول   ·   واکنش نخست", rect(90, 610, 1096, 52), 23, true, cyan)
}
let labels: [(String, String, String)] = [
    ("RADIO", "مرکز اعزام منطقه", "کانال رادیویی"),
    ("DALIA", "سرگرد دالیا رحیم", "فرماندهی میدانی"),
    ("SAMIRA", "مهندس سمیرا حداد", "زیرساخت شهری"),
    ("ARIA", "آریا", "دستیار ارتباط شهری"),
    ("COMMANDER", "فرمانده", "کانال فرماندهی")
]
for (id, name, role) in labels {
    try render("label_" + id) {
        color(0.047, 0.086, 0.11, 0.86).setFill()
        NSBezierPath(roundedRect: rect(676, 587, 560, 99), xRadius: 7, yRadius: 7).fill()
        cyan.setFill(); rect(1232, 599, 4, 75).fill()
        text(name, rect(697, 591, 520, 49), 32, true)
        text(role, rect(697, 639, 520, 39), 22, true, cyan)
    }
}
print("Rendered six Persian overlays with native right-to-left shaping.")
