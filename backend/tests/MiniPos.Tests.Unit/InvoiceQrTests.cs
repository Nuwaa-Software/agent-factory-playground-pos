using System.Text;
using MiniPos.Domain.Invoicing;

namespace MiniPos.Tests.Unit;

public class InvoiceQrTests
{
    [Fact]
    public void Encodes_five_tlv_records()
    {
        var qr = InvoiceQr.Encode("Acme Store", "310122393500003", new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero),
            11500, 1500);

        var records = Decode(qr);

        records.Should().Equal(
            (1, "Acme Store"), (2, "310122393500003"), (3, "2026-01-02T03:04:05Z"), (4, "115.00"), (5, "15.00"));
    }

    [Theory]
    [InlineData(0, "0.00")]
    [InlineData(5, "0.05")]
    [InlineData(123456, "1234.56")]
    public void Formats_halalas_as_sar(long halalas, string sar) => InvoiceQr.FormatSar(halalas).Should().Be(sar);

    /// <summary>Reads TLV records: 1 tag byte, 1 length byte (= number of UTF-8 bytes), value bytes.</summary>
    internal static List<(int Tag, string Value)> Decode(string base64)
    {
        var bytes = Convert.FromBase64String(base64);
        var records = new List<(int, string)>();
        for (var i = 0; i < bytes.Length;)
        {
            int tag = bytes[i], length = bytes[i + 1];
            records.Add((tag, Encoding.UTF8.GetString(bytes, i + 2, length)));
            i += 2 + length;
        }

        return records;
    }
}
