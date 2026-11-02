namespace PaymentGateway.Domain;

public readonly record struct Money : IComparable<Money>
{
    public decimal Amount { get; }

    private Money(decimal amount) => Amount = amount;

    public static Money Zero => new(0m);

    public static Money Of(decimal amount)
    {
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "O valor não pode ser negativo.");

        if (decimal.Round(amount, 2) != amount)
            throw new ArgumentException("O valor deve ter no máximo 2 casas decimais (centavos).", nameof(amount));

        return new Money(amount);
    }

    public Money Add(Money other) => Of(Amount + other.Amount);

    public Money Subtract(Money other)
    {
        if (other.Amount > Amount)
            throw new InvalidOperationException("O resultado não pode ser negativo.");

        return Of(Amount - other.Amount);
    }

    public int CompareTo(Money other) => Amount.CompareTo(other.Amount);

    public static Money operator +(Money left, Money right) => left.Add(right);
    public static Money operator -(Money left, Money right) => left.Subtract(right);
    public static bool operator >(Money left, Money right) => left.CompareTo(right) > 0;
    public static bool operator <(Money left, Money right) => left.CompareTo(right) < 0;
    public static bool operator >=(Money left, Money right) => left.CompareTo(right) >= 0;
    public static bool operator <=(Money left, Money right) => left.CompareTo(right) <= 0;

    public override string ToString() => Amount.ToString("C", new System.Globalization.CultureInfo("pt-BR"));
}
