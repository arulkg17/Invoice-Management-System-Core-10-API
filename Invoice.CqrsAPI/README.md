# Invoice.CqrsAPI

CQRS version of Invoice.CoreAPI. Same BAL, same DTOs, same JSON, same routes. Only the way a request travels is different.

    Invoice.CoreAPI :  Controller -> Service (BAL) -> Repository (DAL)
    Invoice.CqrsAPI :  Controller -> Dispatcher -> [Logging] -> [Validation] -> Handler -> Service (BAL) -> Repository (DAL)

No new BAL or DAL. Handlers call the existing `ICategoryService`, `IItemmasterService` ... interfaces.

## Folder map

    Cqrs/        the mini mediator (messages, dispatcher, behaviors, validation) - written once
    Features/    one folder per master / transaction, one file per use case
      Categories  Itemmasters  Customers  Vendors  Users          masters
      PurchaseOrders  Receipts  SalesInvoices                      transactions (+ Approve / Post / Cancel, PDF)
      Stock                                                        read-only (queries only)
      Auth                                                         Login
        <Feature>/Commands/   Create, Update, Delete (+ workflow steps)   change data
        <Feature>/Queries/    GetAll, GetById, GetPaged (+ Pdf)           read data
    Controllers/ thin: build command/query -> dispatcher.SendAsync -> return ApiResponse
    Middleware/  GlobalExceptionMiddleware: one place that maps exceptions to 400 / 401 / 404 / 500
    Extensions/  DI registration of the existing BAL + DAL, JWT

## Where each kind of rule lives

    Validator (Features/.../XxxData.cs)   shape of the request: required, length, range, format
    Handler                               needs the database: exists? unique? allowed in this status?
    BAL service                           money maths, totals, stock, numbering, transactions (unchanged)

## Business rules added by the handlers (not in Invoice.CoreAPI)

    Category    code unique; cannot delete while it has active items
    Itemmaster  category must exist; item code + bar code unique; cannot delete while stock is on hand
    Customer    code unique; cannot delete while it has sales invoices
    Vendor      code unique; cannot delete while it has purchase orders
    User        duplicate user name / email -> 409; cannot delete your own account; password 8+ chars
    PO / SI     vendor / customer and items must exist and be active
    PO / Receipt / SI   edit only in Draft; delete only in Draft or Cancelled; Approve / Post only from Draft;
                        cancel twice is rejected

## Adding the next feature (e.g. Customer)

1. `Features/Customers/Commands/CreateCustomer.cs`: record command + validator + handler (copy CreateCategory.cs).
2. `Features/Customers/Queries/...`: same for reads.
3. `Controllers/CustomerController.cs`: copy CategoryController.
4. Nothing in Program.cs: handlers and validators are found by assembly scanning.

## Run

    dotnet run --project Invoice.CqrsAPI      # http://localhost:5064/swagger  (CoreAPI stays on 5063)
    dotnet test Invoice.CqrsAPI.Test
