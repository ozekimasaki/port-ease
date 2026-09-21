var destination = args.Length > 0 ? args[0] : "portban.ico";
var directory = Path.GetDirectoryName(Path.GetFullPath(destination));
if (!string.IsNullOrEmpty(directory))
    Directory.CreateDirectory(directory);

var sizes = new[] { 16, 24, 32, 48, 64, 128, 256 };
var images = sizes.Select(size => Png.Encode(IconArt.Draw(size))).ToArray();
File.WriteAllBytes(destination, Ico.Pack(sizes, images));

internal static class IconArt
{
    public static byte[] Draw(int size)
    {
        var pixels = new byte[size * size * 4];
        var center = (size - 1) / 2.0;
        var radius = size * 0.46;
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var dx = x - center;
                var dy = y - center;
                var distance = Math.Sqrt((dx * dx) + (dy * dy));
                var disc = Coverage(radius - distance, 0.8);
                if (disc <= 0)
                    continue;

                var unit = distance / radius;
                var ring = Math.Max(
                    Math.Max(Ring(unit, 0.34, size), Ring(unit, 0.62, size)),
                    Ring(unit, 0.90, size));
                var hairline = Math.Max(Line(dx, size), Line(dy, size)) * (unit < 0.92 ? 1 : 0);
                var mark = Math.Clamp(Math.Max(ring, hairline * 0.55), 0, 1);

                var red = (byte)Lerp(20, 126, mark);
                var green = (byte)Lerp(36, 196, mark);
                var blue = (byte)Lerp(51, 214, mark);
                var index = ((y * size) + x) * 4;
                pixels[index] = red;
                pixels[index + 1] = green;
                pixels[index + 2] = blue;
                pixels[index + 3] = (byte)(255 * disc);
            }
        }

        var dotX = center + (radius * 0.46);
        var dotY = center - (radius * 0.28);
        var dotRadius = Math.Max(1.15, size * 0.075);
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var dx = x - dotX;
                var dy = y - dotY;
                var cover = Coverage(dotRadius - Math.Sqrt((dx * dx) + (dy * dy)), 0.75);
                if (cover <= 0)
                    continue;

                var index = ((y * size) + x) * 4;
                pixels[index] = (byte)Lerp(pixels[index], 230, cover);
                pixels[index + 1] = (byte)Lerp(pixels[index + 1], 180, cover);
                pixels[index + 2] = (byte)Lerp(pixels[index + 2], 74, cover);
                pixels[index + 3] = (byte)Math.Max(pixels[index + 3], 255 * cover);
            }
        }

        return pixels;
    }

    private static double Ring(double unit, double at, int size)
    {
        var thickness = Math.Max(0.012, 1.15 / size);
        return Coverage(thickness - Math.Abs(unit - at), thickness);
    }

    private static double Line(double delta, int size)
    {
        var thickness = Math.Max(0.55, size * 0.012);
        return Coverage(thickness - Math.Abs(delta), 0.7);
    }

    private static double Coverage(double distance, double softness)
    {
        if (distance >= softness)
            return 1;
        if (distance <= -softness)
            return 0;
        return (distance + softness) / (softness * 2);
    }

    private static double Lerp(double from, double to, double amount) => from + ((to - from) * amount);
}

internal static class Png
{
    public static byte[] Encode(byte[] rgba)
    {
        var size = (int)Math.Sqrt(rgba.Length / 4);
        var raw = new byte[(size * 4 + 1) * size];
        for (var y = 0; y < size; y++)
            Buffer.BlockCopy(rgba, y * size * 4, raw, (y * (size * 4 + 1)) + 1, size * 4);

        using var output = new MemoryStream();
        output.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        WriteChunk(output, "IHDR", Header(size));
        WriteChunk(output, "IDAT", Deflate(raw));
        WriteChunk(output, "IEND", []);
        return output.ToArray();
    }

    private static byte[] Header(int size)
    {
        var header = new byte[13];
        WriteInt(header, 0, size);
        WriteInt(header, 4, size);
        header[8] = 8;
        header[9] = 6;
        return header;
    }

    private static byte[] Deflate(byte[] raw)
    {
        using var output = new MemoryStream();
        using (var zlib = new System.IO.Compression.ZLibStream(output, System.IO.Compression.CompressionLevel.SmallestSize, leaveOpen: true))
            zlib.Write(raw);
        return output.ToArray();
    }

    private static void WriteChunk(Stream output, string type, byte[] data)
    {
        var typeBytes = System.Text.Encoding.ASCII.GetBytes(type);
        Span<byte> length = stackalloc byte[4];
        WriteInt(length, 0, data.Length);
        output.Write(length);
        output.Write(typeBytes);
        output.Write(data);
        Span<byte> crc = stackalloc byte[4];
        WriteInt(crc, 0, Crc32(typeBytes, data));
        output.Write(crc);
    }

    private static int Crc32(byte[] type, byte[] data)
    {
        var crc = 0xFFFFFFFF;
        foreach (var value in type)
            crc = Step(crc, value);
        foreach (var value in data)
            crc = Step(crc, value);
        return (int)~crc;
    }

    private static uint Step(uint crc, byte value)
    {
        crc ^= value;
        for (var bit = 0; bit < 8; bit++)
            crc = (crc & 1) == 1 ? (crc >> 1) ^ 0xEDB88320 : crc >> 1;
        return crc;
    }

    private static void WriteInt(Span<byte> buffer, int offset, int value)
    {
        buffer[offset] = (byte)(value >> 24);
        buffer[offset + 1] = (byte)(value >> 16);
        buffer[offset + 2] = (byte)(value >> 8);
        buffer[offset + 3] = (byte)value;
    }
}

internal static class Ico
{
    public static byte[] Pack(int[] sizes, byte[][] images)
    {
        using var output = new MemoryStream();
        output.WriteByte(0);
        output.WriteByte(0);
        output.WriteByte(1);
        output.WriteByte(0);
        output.WriteByte((byte)images.Length);
        output.WriteByte(0);

        var offset = 6 + (16 * images.Length);
        for (var index = 0; index < images.Length; index++)
        {
            var dimension = sizes[index] >= 256 ? 0 : sizes[index];
            output.WriteByte((byte)dimension);
            output.WriteByte((byte)dimension);
            output.WriteByte(0);
            output.WriteByte(0);
            output.WriteByte(1);
            output.WriteByte(0);
            output.WriteByte(32);
            output.WriteByte(0);
            WriteLittle(output, images[index].Length);
            WriteLittle(output, offset);
            offset += images[index].Length;
        }

        foreach (var image in images)
            output.Write(image);

        return output.ToArray();
    }

    private static void WriteLittle(Stream output, int value)
    {
        output.WriteByte((byte)value);
        output.WriteByte((byte)(value >> 8));
        output.WriteByte((byte)(value >> 16));
        output.WriteByte((byte)(value >> 24));
    }
}
