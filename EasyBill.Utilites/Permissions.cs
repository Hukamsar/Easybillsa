using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AOne.Utility
{
    public static class Permissions
    {
        [DisplayName("Item Master")]
        [Description("ItemMaster Permissions")]
        public static class ItemMaster
        {
            public const string View = "Permissions.ItemMaster.View";
            public const string Create = "Permissions.ItemMaster.Create";
            public const string Edit = "Permissions.ItemMaster.Edit";
            public const string Delete = "Permissions.ItemMaster.Delete"; 
        }
        [DisplayName("Category Master")]
        [Description("CategoryMaster Permissions")]
        public static class CategoryMaster
        {
            public const string View = "Permissions.CategoryMaster.View";
            public const string Create = "Permissions.CategoryMaster.Create";
            public const string Edit = "Permissions.CategoryMaster.Edit";
            public const string Delete = "Permissions.CategoryMaster.Delete";
        }
        [DisplayName("SubCategory")]
        [Description("SubCategory Permissions")]
        public static class SubCategory
        {
            public const string View = "Permissions.SubCategory.View";
            public const string Create = "Permissions.SubCategory.Create";
            public const string Edit = "Permissions.SubCategory.Edit";
            public const string Delete = "Permissions.SubCategory.Delete";
        }
        [DisplayName("Company")]
        [Description("Company Permissions")]
        public static class Company
        {
            public const string View = "Permissions.Company.View";
            public const string Create = "Permissions.Company.Create";
            public const string Edit = "Permissions.Company.Edit";
            public const string Delete = "Permissions.Company.Delete";
        }
        [DisplayName("HSN")]
        [Description("HSN Permissions")]
        public static class HSN
        {
            public const string View = "Permissions.HSN.View";
            public const string Create = "Permissions.HSN.Create";
            public const string Edit = "Permissions.HSN.Edit";
            public const string Delete = "Permissions.HSN.Delete";
        }
        [DisplayName("Division")]
        [Description("Division Permissions")]
        public static class Division
        {
            public const string View = "Permissions.Division.View";
            public const string Create = "Permissions.Division.Create";
            public const string Edit = "Permissions.Division.Edit";
            public const string Delete = "Permissions.Division.Delete";
        }
        [DisplayName("Customer")]
        [Description("Customer Permission")]
        public static class Customer
        {
            public const string View = "Permissions.Customer.View";
            public const string Create = "Permissions.Customer.Create";
            public const string Edit = "Permissions.Customer.Edit";
            public const string Delete = "Permissions.Customer.Delete";
        }
        [DisplayName("Supplier")]
        [Description("Supplier Permissions")]
        public static class Supplier
        {
            public const string View = "Permissions.Suppliers.View";
            public const string Create = "Permissions.Suppliers.Create";
            public const string Edit = "Permissions.Suppliers.Edit";
            public const string Delete = "Permissions.Suppliers.Delete";
        }
        [DisplayName("Bank")]
        [Description("Bank Permissions")]
        public static class Bank
        {
            public const string View = "Permissions.Bank.View";
            public const string Create = "Permissions.Bank.Create";
            public const string Edit = "Permissions.Bank.Edit";
            public const string Delete = "Permissions.Bank.Delete";
        }
        [DisplayName("AccountGroup")]
        [Description("AccountGroup Permissions")]
        public static class AccountGroup
        {
            public const string View = "Permissions.AccountGroup.View";
            public const string Create = "Permissions.AccountGroup.Create";
            public const string Edit = "Permissions.AccountGroup.Edit";
            public const string Delete = "Permissions.AccountGroup.Delete";
        }
        [DisplayName("Country")]
        [Description("Country Permissions")]
        public static class Country
        {
            public const string View = "Permissions.Country.View";
            public const string Create = "Permissions.Country.Create";
            public const string Edit = "Permissions.Country.Edit";
            public const string Delete = "Permissions.Country.Delete";
        }
        [DisplayName("State")]
        [Description("State Permissions")]
        public static class State
        {
            public const string View = "Permissions.State.View";
            public const string Create = "Permissions.State.Create";
            public const string Edit = "Permissions.State.Edit";
            public const string Delete = "Permissions.State.Delete";
        }
        [DisplayName("City")]
        [Description("City Permissions")]
        public static class City
        {
            public const string View = "Permissions.City.View";
            public const string Create = "Permissions.City.Create";
            public const string Edit = "Permissions.City.Edit";
            public const string Delete = "Permissions.City.Delete";
        }
        [DisplayName("Currency")]
        [Description("Currency Permissions")]
        public static class Currency
        {
            public const string View = "Permissions.Currency.View";
            public const string Create = "Permissions.Currency.Create";
            public const string Edit = "Permissions.Currency.Edit";
            public const string Delete = "Permissions.Currency.Delete";
        }
        [DisplayName("Employee")]
        [Description("Employee Permissions")]
        public static class Employee
        {
            public const string View = "Permissions.Employee.View";
            public const string Create = "Permissions.Employee.Create";
            public const string Edit = "Permissions.Employee.Edit";
            public const string Delete = "Permissions.Employee.Delete";
        }
        [DisplayName("Department")]
        [Description("Department Permissions")]
        public static class Department
        {
            public const string View = "Permissions.Department.View";
            public const string Create = "Permissions.Department.Create";
            public const string Edit = "Permissions.Department.Edit";
            public const string Delete = "Permissions.Department.Delete";
        }
        [DisplayName("Designation")]
        [Description("Designation Permissions")]
        public static class Designation
        {
            public const string View = "Permissions.Designation.View";
            public const string Create = "Permissions.Designation.Create";
            public const string Edit = "Permissions.Designation.Edit";
            public const string Delete = "Permissions.Designation.Delete";
        }
        [DisplayName("Offers")]
        [Description("Offers Permissions")]
        public static class Offers
        {
            public const string View = "Permissions.Offers.View";
            public const string Create = "Permissions.Offers.Create";
            public const string Edit = "Permissions.Offers.Edit";
            public const string Delete = "Permissions.Offers.Delete";
        }
        [DisplayName("ModeOfPayment")]
        [Description("ModeOfPayment Permissions")]
        public static class ModeOfPayment
        {
            public const string View = "Permissions.ModeOfPayment.View";
            public const string Create = "Permissions.ModeOfPayment.Create";
            public const string Edit = "Permissions.ModeOfPayment.Edit";
            public const string Delete = "Permissions.ModeOfPayment.Delete";
        }
        [DisplayName("OpeningStock")]
        [Description("OpeningStock Permissions")]
        public static class OpeningStock
        {
            public const string View = "Permissions.OpeningStock.View";
            public const string Create = "Permissions.OpeningStock.Create";
            public const string Edit = "Permissions.OpeningStock.Edit";
            public const string Delete = "Permissions.OpeningStock.Delete";
        }
        [DisplayName("Roles")]
        [Description("Roles Permissions")]
        public static class Roles
        {
            public const string View = "Permissions.Roles.View";
            public const string Create = "Permissions.Roles.Create";
            public const string Edit = "Permissions.Roles.Edit";
            public const string Delete = "Permissions.Roles.Delete";
            public const string ManagePermissions = "Permissions.Roles.Permissions";
            public const string ManageNavigation = "Permissions.Roles.Navigation";
            public const string CopyRole = "Permissions.Roles.CopyRole";
        }
        [DisplayName("Users")]
        [Description("Users Permissions")]
        public static class Users
        {
            public const string View = "Permissions.Users.View";
            public const string Create = "Permissions.Users.Create";
            public const string Edit = "Permissions.Users.Edit";
            public const string Delete = "Permissions.Users.Delete"; 
            public const string ManageRoles = "Permissions.Users.ManageRoles";
            public const string RestPassword = "Permissions.Users.RestPassword";
            public const string Active = "Permissions.Users.Active";
        }

        
        [DisplayName("Company Registration")]
        [Description("Company Registration Permissions")]
        public static class CompanyRegistration
        {
            public const string View = "Permissions.CompanyRegistration.View";
            public const string Create = "Permissions.CompanyRegistration.Create";
            public const string Edit = "Permissions.CompanyRegistration.Edit";
            public const string Delete = "Permissions.CompanyRegistration.Delete";
            public const string Search = "Permissions.CompanyRegistration.Search";
            public const string DatabaseBackup = "Permissions.CompanyRegistration.DatabaseBackup";
        }
        [DisplayName("TermCondition")]
        [Description("TermCondition Permissions")]
        public static class TermCondition
        {
            public const string View = "Permissions.TermCondition.View";
            public const string Create = "Permissions.TermCondition.Create";
            public const string Edit = "Permissions.TermCondition.Edit";
            public const string Delete = "Permissions.TermCondition.Delete"; 
        }
        [DisplayName("Sales")]
        [Description("Sales Permissions")]
        public static class Sales
        {
            public const string View = "Permissions.Sales.View";
            public const string Create = "Permissions.Sales.Create";
            public const string Edit = "Permissions.Sales.Edit";
            public const string Delete = "Permissions.Sales.Delete";
        }
        [DisplayName("Sales Order")]
        [Description("SalesOrder Permissions")]
        public static class SalesOrder
        {
            public const string View = "Permissions.SalesOrder.View";
            public const string Create = "Permissions.SalesOrder.Create";
            public const string Edit = "Permissions.SalesOrder.Edit";
            public const string Delete = "Permissions.SalesOrder.Delete";
        }
        [DisplayName("StockReturn Order")]
        [Description("StockReturn Permissions")]
        public static class StockReturn
        {
            public const string View = "Permissions.StockReturn.View";
            public const string Create = "Permissions.StockReturn.Create";
            public const string Edit = "Permissions.StockReturn.Edit";
            public const string Delete = "Permissions.StockReturn.Delete";
        }
        [DisplayName("StockIssue Order")]
        [Description("StockIssue Permissions")]
        public static class StockIssue
        {
            public const string View = "Permissions.StockIssue.View";
            public const string Create = "Permissions.StockIssue.Create";
            public const string Edit = "Permissions.StockIssue.Edit";
            public const string Delete = "Permissions.StockIssue.Delete";
        }
        [DisplayName("PurchaseEntry")]
        [Description("PurchaseEntry Permission")]
        public static class PurchaseEntry
        {
            public const string View = "Permissions.PurchaseEntry.View";
            public const string Create = "Permissions.PurchaseEntry.Create";
            public const string Edit = "Permissions.PurchaseEntry.Edit";
            public const string Delete = "Permissions.PurchaseEntry.Delete";
        }
        [DisplayName("PurchaseReturn")]
        [Description("PurchaseReturn Permission")]
        public static class PurchaseReturn
        {
            public const string View = "Permissions.PurchaseReturn.View";
            public const string Create = "Permissions.PurchaseReturn.Create";
            public const string Edit = "Permissions.PurchaseReturn.Edit";
            public const string Delete = "Permissions.PurchaseReturn.Delete";
        }
        [DisplayName("PurchaseOrder")]
        [Description("PurchaseOrder Permission")]
        public static class PurchaseOrder
        {
            public const string View = "Permissions.PurchaseOrder.View";
            public const string Create = "Permissions.PurchaseOrder.Create";
            public const string Edit = "Permissions.PurchaseOrder.Edit";
            public const string Delete = "Permissions.PurchaseOrder.Delete";
        }
        [DisplayName("PurchaseChallan")]
        [Description("PurchaseChallan Permission")]
        public static class PurchaseChallan
        {
            public const string View = "Permissions.PurchaseChallan.View";
            public const string Create = "Permissions.PurchaseChallan.Create";
            public const string Edit = "Permissions.PurchaseChallan.Edit";
            public const string Delete = "Permissions.PurchaseChallan.Delete";
        }
        [DisplayName("StockReceive")]
        [Description("StockReceive Permission")]
        public static class StockReceive
        {
            public const string View = "Permissions.StockReceive.View";
            public const string Create = "Permissions.StockReceive.Create";
            public const string Edit = "Permissions.StockReceive.Edit";
            public const string Delete = "Permissions.StockReceive.Delete";
        }
        [DisplayName("PaymentVoucherCategory")]
        [Description("PaymentVoucherCategory Permission")]
        public static class PaymentVoucherCategory
        {
            public const string View = "Permissions.PaymentVoucherCategory.View";
            public const string Create = "Permissions.PaymentVoucherCategory.Create";
            public const string Edit = "Permissions.PaymentVoucherCategory.Edit";
            public const string Delete = "Permissions.PaymentVoucherCategory.Delete";
        }
        [DisplayName("PaymentVoucher")]
        [Description("PaymentVoucher Permission")]
        public static class PaymentVoucher
        {
            public const string View = "Permissions.PaymentVoucher.View";
            public const string Create = "Permissions.PaymentVoucher.Create";
            public const string Edit = "Permissions.PaymentVoucher.Edit";
            public const string Delete = "Permissions.PaymentVoucher.Delete";
        }
        [DisplayName("ReceiveVoucher")]
        [Description("ReceiveVoucher Permission")]
        public static class ReceiveVoucher
        {
            public const string View = "Permissions.ReceiveVoucher.View";
            public const string Create = "Permissions.ReceiveVoucher.Create";
            public const string Edit = "Permissions.ReceiveVoucher.Edit";
            public const string Delete = "Permissions.ReceiveVoucher.Delete";
        }
        [DisplayName("Books")]
        [Description("Books Permissions")]
        public static class Books
        {
            public const string CashBookView = "Permissions.CashBook.View";
            public const string BankBookView = "Permissions.BankBook.View";
            public const string DayBookView = "Permissions.DayBook.View";
            public const string SalesBookView = "Permissions.SalesBook.View";
            public const string PurchaseBookView = "Permissions.PurchaseBook.View";
        }
        [DisplayName("Contra")]
        [Description("Contra Permission")]
        public static class Contra
        {
            public const string View = "Permissions.Contra.View";
            public const string Create = "Permissions.Contra.Create";
            public const string Edit = "Permissions.Contra.Edit";
            public const string Delete = "Permissions.Contra.Delete";
        }
        
        /// <summary>
        /// Returns a list of Permissions.
        /// </summary>
        /// <returns></returns>
        public static List<string> GetRegisteredPermissions()
        {
            var permissions = new List<string>();
            foreach (var prop in typeof(Permissions).GetNestedTypes().SelectMany(c => c.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)))
            {
                var propertyValue = prop.GetValue(null);
                if (propertyValue is not null)
                    permissions.Add((string)propertyValue);
            }
            return permissions;
        }

    }
}
