using PaymentGateway.Domain;

namespace PaymentGateway.Domain.Tests;

public class MoneyTests
{
    [Fact]
    public void Of_ValorValido_CriaMoney()
    {
        var money = Money.Of(10.50m);

        Assert.Equal(10.50m, money.Amount);
    }

    [Fact]
    public void Of_ValorNegativo_Lanca()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Money.Of(-0.01m));
    }

    [Fact]
    public void Of_MaisDeDuasCasasDecimais_Lanca()
    {
        Assert.Throws<ArgumentException>(() => Money.Of(10.555m));
    }

    [Fact]
    public void Add_SomaOsValores()
    {
        var result = Money.Of(10.10m) + Money.Of(0.20m);

        Assert.Equal(Money.Of(10.30m), result);
    }

    [Fact]
    public void Subtract_ResultadoNegativo_Lanca()
    {
        var money = Money.Of(5m);
        var other = Money.Of(5.01m);

        Assert.Throws<InvalidOperationException>(() => money - other);
    }

    [Fact]
    public void Money_ComMesmoValor_SaoIguais()
    {
        Assert.Equal(Money.Of(100m), Money.Of(100.00m));
    }

    [Fact]
    public void Comparacao_FuncionaComOperadores()
    {
        Assert.True(Money.Of(200m) > Money.Of(130m));
        Assert.True(Money.Of(130m) <= Money.Of(130m));
    }
}
