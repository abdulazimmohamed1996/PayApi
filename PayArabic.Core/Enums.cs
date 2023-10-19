namespace PayArabic.Core;
public enum UserType
{
    SystemAdmin, SuperAdmin, Admin, Vendor, User, Customer
}
public enum EventType
{
    UserRegister, UserForgetPassword, InvoiceCreated, InvoicePaid, UserForgetPasswordRecovery
    , ContactUs
}
public enum Permission
{
    None, View, Add, Edit
}
public enum EntityType
{
    User, Invoice, Deposit, Refund, Order, PaymentLink
}
public enum DepositStatus
{
    Started, Completed, Cancelled
}
public enum PaymentMethodsPaidBy
{
    Vendor, Customer, Split
}
public enum DiscountType
{
    Amount, Percent
}
public enum FeesType
{
    Amount, Percent, AmountAndPercent
}
public enum InvoiceStatus
{
    Unpaid, Paid, Canceled, Deposited, Refunded, Expired
}
public enum IntegrationCode
{
    SMS, Email, Knet, NBKMasterCard, Amex, NBKMasterCardGCC
}
public enum IntegrationType
{
    SMS, Email, PaymentMethod,
}
public enum TransactionRequestType
{
    Authorize, Capture, Refund
}
public enum TransactionType
{
    Invoice, Refund
}
public enum TransactionStatus
{
    Success, Failed, RefundedSuccess, Pending
}
public enum SendType
{
    SMS, Email, Whatsapp, Link
}
public enum InvoiceType
{
    Invoice, Order, PaymentLink, RefundAdjust
}
