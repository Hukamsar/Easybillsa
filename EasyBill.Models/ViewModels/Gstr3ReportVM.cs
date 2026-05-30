using System;
using System.Collections.Generic;

namespace EasyBill.Models.ViewModels
{
    public class Gstr3ReportVM
    {
        public DateTime StartBillDate { get; set; }
        public DateTime EndBillDate { get; set; }
        public string GSTIN { get; set; } = string.Empty;
        public string LegalName { get; set; } = string.Empty;
        public int Year { get; set; }
        public string Month { get; set; } = string.Empty;
        public List<Gstr3Section31RowVM> Section31Rows { get; set; } = new();
        public List<Gstr3Section32RowVM> Section32Rows { get; set; } = new();
        public List<Gstr3Section4RowVM> Section4Rows { get; set; } = new();
        public List<Gstr3Section5RowVM> Section5Rows { get; set; } = new();
        public List<Gstr3Section61RowVM> Section61Rows { get; set; } = new();
        public List<Gstr3Section62RowVM> Section62Rows { get; set; } = new();
    }

    public class Gstr3Section31RowVM
    {
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal TaxableValue { get; set; }
        public decimal IntegratedTax { get; set; }
        public decimal CentralTax { get; set; }
        public decimal StateUtTax { get; set; }
        public decimal Cess { get; set; }
    }

    public class Gstr3Section32RowVM
    {
        public string PlaceOfSupply { get; set; } = string.Empty;
        public decimal TotalTaxableValue { get; set; }
        public decimal AmountOfIntegratedTax { get; set; }
    }

    public class Gstr3Section4RowVM
    {
        public string Code { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public decimal IntegratedTax { get; set; }
        public decimal CentralTax { get; set; }
        public decimal StateUtTax { get; set; }
        public decimal Cess { get; set; }
        public bool IsHeader { get; set; }
    }

    public class Gstr3Section5RowVM
    {
        public string NatureOfSupplies { get; set; } = string.Empty;
        public decimal InterStateSupplies { get; set; }
        public decimal IntraStateSupplies { get; set; }
    }

    public class Gstr3Section61RowVM
    {
        public string Description { get; set; } = string.Empty;
        public decimal TaxPayable { get; set; }
        public decimal PaidThroughItcIntegratedTax { get; set; }
        public decimal PaidThroughItcCentralTax { get; set; }
        public decimal PaidThroughItcStateUtTax { get; set; }
        public decimal PaidThroughItcCess { get; set; }
        public decimal TaxPaidTdsTcs { get; set; }
        public decimal TaxPaidCash { get; set; }
        public decimal Interest { get; set; }
        public decimal LateFee { get; set; }
    }

    public class Gstr3Section62RowVM
    {
        public string Details { get; set; } = string.Empty;
        public decimal IntegratedTax { get; set; }
        public decimal CentralTax { get; set; }
        public decimal StateUtTax { get; set; }
    }
}
