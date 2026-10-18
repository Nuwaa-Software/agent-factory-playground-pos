using System.Globalization;
using System.Text;

namespace MiniPos.Domain.Invoicing;

/// <summary>
/// ZATCA-style simplified-invoice QR payload: TLV (tag, length, value) records, Base64-encoded.
/// Tags: 1 seller name, 2 VAT number, 3 timestamp (ISO 8601), 4 invoice total incl. VAT, 5 VAT total.
/// Each record is one tag byte, one length byte, then the UTF-8 value bytes.
/// </summary>
public static class InvoiceQr
{
    public static string Encode(string sellerName, string vatNumber, DateTimeOffset timestamp, long totalHalalas,
        long vatHalalas)
    {
        using var buffer = new MemoryStream();
        Write(buffer, 1, sellerName);
        Write(buffer, 2, vatNumber);
        Write(buffer, 3, timestamp.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture));
        Write(buffer, 4, FormatSar(totalHalalas));
        Write(buffer, 5, FormatSar(vatHalalas));
        return Convert.ToBase64String(buffer.ToArray());
    }

    public static string FormatSar(long halalas) =>
        (halalas / 100m).ToString("0.00", CultureInfo.InvariantCulture);

    private static void Write(MemoryStream buffer, byte tag, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        buffer.WriteByte(tag);
        buffer.WriteByte((byte)bytes.Length);
        buffer.Write(bytes);
    }
}
