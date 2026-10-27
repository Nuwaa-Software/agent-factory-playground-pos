namespace MiniPos.Domain.Companies;

/// <summary>Shared key names and defaults for company settings stored in CompanyConfig.</summary>
public static class CompanySettings
{
    public const string TaxRatePercentKey = "Pos:TaxRatePercent";
    public const decimal TaxRatePercentDefault = 15.0m;

    public const string ReceiptFooterTextKey = "Pos:ReceiptFooterText";
    public const string ReceiptFooterTextDefault = "";

    public const string LowStockThresholdKey = "Pos:LowStockThreshold";
    public const int LowStockThresholdDefault = 10;

    public const string CompanyNameKey = "Pos:CompanyName";
    public const string CompanyNameDefault = "";

    public const string VatRegistrationNumberKey = "Pos:VatRegistrationNumber";
    public const string VatRegistrationNumberDefault = "";
}
