namespace HRMS.Domain.Enums;

public enum OrgLevel
{
    Headquarters = 0,
    Branch = 1,
    Department = 2,
    ProductionLine = 3,
    Team = 4,
    Supplier = 5
}

public enum EmployeeType
{
    Internal = 0,
    ThirdParty = 1
}

public enum SalaryMode
{
    Hourly = 0,
    Fixed = 1,
    PieceRate = 2,
    Mixed = 3
}

public enum WorkShiftType
{
    Weekday = 0,
    Weekend = 1,
    Holiday = 2
}

public enum ApprovalStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}

public enum BillStatus
{
    Draft = 0,
    Confirmed = 1,
    Invoiced = 2,
    Paid = 3
}

public enum ExpenseType
{
    Reimbursement = 0,
    Purchase = 1,
    Other = 2
}

public enum ExpenseStatus
{
    Draft = 0,
    Submitted = 1,
    Approved = 2,
    Rejected = 3,
    Paid = 4
}
