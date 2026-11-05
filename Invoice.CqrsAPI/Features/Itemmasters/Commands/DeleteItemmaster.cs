using Invoice.BAL.Contracts;
using Invoice.CqrsAPI.Cqrs;
using Invoice.Model;

namespace Invoice.CqrsAPI.Features.Itemmasters.Commands;

public sealed record DeleteItemmasterCommand(int Id) : ICommand<Unit>;

public sealed class DeleteItemmasterValidator : RuleValidator<DeleteItemmasterCommand>
{
    public DeleteItemmasterValidator()
    {
        Rule(c => c.Id > 0, "Id", "Id must be greater than zero.");
    }
}

public sealed class DeleteItemmasterHandler : ICommandHandler<DeleteItemmasterCommand, Unit>
{
    private readonly IItemmasterService _items;
    private readonly IStockService _stock;

    public DeleteItemmasterHandler(IItemmasterService items, IStockService stock)
    {
        _items = items;
        _stock = stock;
    }

    public async Task<Unit> HandleAsync(DeleteItemmasterCommand request, CancellationToken cancellationToken)
    {
        _ = await _items.GetByIdAsync(request.Id)
            ?? throw new NotFoundException($"Item with Id {request.Id} was not found.");

        // Business rule: an item that still has stock on hand cannot be deleted.
        var stock = await _stock.GetByItemmasterIdAsync(request.Id);

        if (stock is { OnHandQuantity: > 0 })
        {
            throw new BusinessRuleException(
                $"Item cannot be deleted because {stock.OnHandQuantity:0.##} unit(s) are still in stock.");
        }

        var deleted = await _items.DeleteAsync(request.Id);

        if (!deleted)
        {
            throw new NotFoundException($"Item with Id {request.Id} was not found.");
        }

        return Unit.Value;
    }
}
