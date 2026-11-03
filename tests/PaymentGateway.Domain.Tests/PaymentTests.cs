using PaymentGateway.Domain;
using PaymentGateway.Domain.Enums;

namespace PaymentGateway.Domain.Tests;

public class PaymentTests
{
    [Fact]
    public void CreatePayment_ShouldThrowArgumentException_WhenValueIsZero()
    {
        var value = Money.Of(0);

        var exception = Assert.Throws<ArgumentException>(() => Payment.Create(value, PaymentMethod.CreditCard, "Test", Guid.NewGuid()));

        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void CreatePayment_ShouldThrowArgumentException_WhenTransactionIdIsEmpty()
    {
        var value = Money.Of(100);

        var exception = Assert.Throws<ArgumentException>(() => Payment.Create(value, PaymentMethod.CreditCard, "Test", Guid.Empty));

        Assert.Equal("transactionId", exception.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreatePayment_ShouldThrowArgumentException_WhenDescriptionIsNullOrWhiteSpace(string? description)
    {
        var value = Money.Of(100);

        var exception = Assert.Throws<ArgumentException>(() => Payment.Create(value, PaymentMethod.CreditCard, description!, Guid.NewGuid()));

        Assert.Equal("description", exception.ParamName);
    }

    [Theory]
    [InlineData(PaymentMethod.Pix)]
    [InlineData(PaymentMethod.DebitCard)]
    [InlineData(PaymentMethod.Boleto)]
    [InlineData((PaymentMethod)999)]
    public void CreatePayment_ShouldThrowArgumentException_WhenPaymentMethodIsNotCreditCard(PaymentMethod paymentMethod)
    {
        var value = Money.Of(100);

        var exception = Assert.Throws<ArgumentException>(() => Payment.Create(value, paymentMethod, "Test", Guid.NewGuid()));

        Assert.Equal("paymentMethod", exception.ParamName);
    }

    [Fact]
    public void CreatePayment_ShouldCreatePayment_WhenValidParameters()
    {
        var value = Money.Of(100);
        var paymentMethod = PaymentMethod.CreditCard;
        var description = "Test";
        var transactionId = Guid.NewGuid();

        var payment = Payment.Create(value, paymentMethod, description, transactionId);

        Assert.Multiple(() =>
        {
            Assert.Equal(value, payment.Value);
            Assert.Equal(paymentMethod, payment.PaymentMethod);
            Assert.Equal(description, payment.Description);
            Assert.Equal(transactionId, payment.TransactionId);
            Assert.Equal(PaymentStatus.Created, payment.Status);
            Assert.Equal(Money.Zero, payment.CapturedValue);
            Assert.Equal(Money.Zero, payment.RefundedValue);
        });
    }

    [Fact]
    public void AuthorizePayment_ShouldKeepValue_WhenAuthorized()
    {
        var payment = Payment.Create(Money.Of(100), PaymentMethod.CreditCard, "Test", Guid.NewGuid());
        var authorizedPayment = payment.Authorize();
        Assert.Equal(Money.Of(100), authorizedPayment.Value);
    }

    [Fact]
    public void AuthorizePayment_ShouldSetStatusToAuthorized_WhenStatusIsCreated()
    {
        var payment = PaymentInStatus(PaymentStatus.Created);

        var authorizedPayment = payment.Authorize();

        Assert.Equal(PaymentStatus.Authorized, authorizedPayment.Status);
    }

    [Theory]
    [InlineData(PaymentStatus.Authorized)]
    [InlineData(PaymentStatus.Captured)]
    [InlineData(PaymentStatus.Completed)]
    [InlineData(PaymentStatus.Refunded)]
    [InlineData(PaymentStatus.Canceled)]
    public void AuthorizePayment_ShouldThrowInvalidOperationException_WhenStatusIsNotCreated(PaymentStatus status)
    {
        var payment = PaymentInStatus(status);

        var exception = Assert.Throws<InvalidOperationException>(() => payment.Authorize());

        Assert.Contains("status", exception.Message);
    }

    [Theory]
    [InlineData(130)]
    [InlineData(200)]
    public void CapturePayment_ShouldSetCapturedValue_WhenCapturedValueIsWithinPaymentValue(decimal capturedAmount)
    {
        var payment = Payment.Create(Money.Of(200), PaymentMethod.CreditCard, "Test", Guid.NewGuid());
        var authorizedPayment = payment.Authorize();
        var capturedValue = Money.Of(capturedAmount);

        var capturedPayment = authorizedPayment.Capture(capturedValue);

        Assert.Multiple(() =>
        {
            Assert.Equal(capturedValue, capturedPayment.CapturedValue);
            Assert.Equal(PaymentStatus.Captured, capturedPayment.Status);
        });
    }

    [Fact]
    public void CapturePayment_ShouldThrowInvalidOperationException_WhenCapturedValueIsGreaterThanPaymentValue()
    {
        var payment = Payment.Create(Money.Of(200), PaymentMethod.CreditCard, "Test", Guid.NewGuid());
        var authorizedPayment = payment.Authorize();
        var capturedValue = Money.Of(200.01m);

        Assert.Throws<InvalidOperationException>(() => authorizedPayment.Capture(capturedValue));
    }

    [Theory]
    [InlineData(PaymentStatus.Created)]
    [InlineData(PaymentStatus.Captured)]
    [InlineData(PaymentStatus.Completed)]
    [InlineData(PaymentStatus.Refunded)]
    [InlineData(PaymentStatus.Canceled)]
    public void CapturePayment_ShouldThrowInvalidOperationException_WhenStatusIsNotAuthorized(PaymentStatus status)
    {
        var payment = PaymentInStatus(status);
        var capturedValue = Money.Of(50);

        var exception = Assert.Throws<InvalidOperationException>(() => payment.Capture(capturedValue));

        Assert.Contains("status", exception.Message);
    }

    [Fact]
    public void CapturePayment_ShouldThrowArgumentException_WhenCapturedValueIsZero()
    {
        var payment = Payment.Create(Money.Of(200), PaymentMethod.CreditCard, "Test", Guid.NewGuid());
        var authorizedPayment = payment.Authorize();
        var capturedValue = Money.Of(0);

        Assert.Throws<ArgumentException>(() => authorizedPayment.Capture(capturedValue));
    }

    [Fact]
    public void RefundPayment_ShouldKeepStatus_WhenRefundedValueIsSmallerThanCapturedValue()
    {
        var payment = Payment.Create(Money.Of(200), PaymentMethod.CreditCard, "Test", Guid.NewGuid());
        var authorizedPayment = payment.Authorize();
        var capturedPayment = authorizedPayment.Capture(Money.Of(150));
        var refundedValue = Money.Of(100);
        var refundedPayment = capturedPayment.Refund(refundedValue);    

        Assert.Equal(PaymentStatus.Captured, refundedPayment.Status);
    }

    [Fact]
    public void RefundPayment_ShouldRefunded_WhenRefundedValueIsEqualCapturedValue()
    {
        var payment = Payment.Create(Money.Of(200), PaymentMethod.CreditCard, "Test", Guid.NewGuid());
        var authorizedPayment = payment.Authorize();
        var capturedPayment = authorizedPayment.Capture(Money.Of(200));
        var refundedValue = Money.Of(200);
        var refundedPayment = capturedPayment.Refund(refundedValue);

        Assert.Equal(PaymentStatus.Refunded, refundedPayment.Status);
    }

    [Fact]
    public void RefundPayment_ShouldRefunded_WhenSecondRefundedValueIsEqualCapturedValue()
    {
        var payment = Payment.Create(Money.Of(130), PaymentMethod.CreditCard, "Test", Guid.NewGuid());
        var authorizedPayment = payment.Authorize();
        var capturedPayment = authorizedPayment.Capture(Money.Of(130));
        var refundedValue = Money.Of(80);
        var refundedPayment = capturedPayment.Refund(refundedValue);
        var secondRefundedValue = Money.Of(50);
        var secondRefundedPayment = refundedPayment.Refund(secondRefundedValue);

        Assert.Equal(PaymentStatus.Refunded, secondRefundedPayment.Status);
    }

    [Fact]
    public void RefundPayment_ShouldThrowInvalidOperationException_WhenRefundedValueIsGreaterThanCapturedValue()
    {
        var payment = Payment.Create(Money.Of(200), PaymentMethod.CreditCard, "Test", Guid.NewGuid());
        var authorizedPayment = payment.Authorize();
        var capturedPayment = authorizedPayment.Capture(Money.Of(130));
        var refundedValue = Money.Of(150);

        Assert.Throws<InvalidOperationException>(() => capturedPayment.Refund(refundedValue));
    }

    [Fact]
    public void RefundPayment_ShouldThrowInvalidOperationException_WhenSecondRefundedValueIsGreaterThanCapturedValue()
    {
        var payment = Payment.Create(Money.Of(200), PaymentMethod.CreditCard, "Test", Guid.NewGuid());
        var authorizedPayment = payment.Authorize();
        var capturedPayment = authorizedPayment.Capture(Money.Of(130));
        var refundedValue = Money.Of(100);
        var refundedPayment = capturedPayment.Refund(refundedValue);
        var secondRefundedValue = Money.Of(50);

        Assert.Throws<InvalidOperationException>(() => refundedPayment.Refund(secondRefundedValue));
    }

    [Fact]
    public void RefundPayment_ShouldThrowArgumentException_WhenRefundedValueIsZero()
    {
        var payment = Payment.Create(Money.Of(200), PaymentMethod.CreditCard, "Test", Guid.NewGuid());
        var authorizedPayment = payment.Authorize();
        var capturedPayment = authorizedPayment.Capture(Money.Of(130));
        var refundedValue = Money.Of(0);

        Assert.Throws<ArgumentException>(() => capturedPayment.Refund(refundedValue));
    }

    [Fact]
    public void RefundPayment_ShouldRefunded_WhenPaymentStatusIsComplete()
    {
        var payment = Payment.Create(Money.Of(200), PaymentMethod.CreditCard, "Test", Guid.NewGuid());
        var authorizedPayment = payment.Authorize();
        var capturedPayment = authorizedPayment.Capture(Money.Of(200));
        var completedPayment = capturedPayment.Complete();
        var refundedValue = Money.Of(200);
        var refundedPayment = completedPayment.Refund(refundedValue);

        Assert.Equal(PaymentStatus.Refunded, refundedPayment.Status);
    }

    [Fact]
    public void RefundPayment_ShouldKeepStatus_WhenPaymentStatusIsCompleteAndRefundIsPartial()
    {
        var payment = PaymentInStatus(PaymentStatus.Completed);
        var refundedValue = Money.Of(50);

        var refundedPayment = payment.Refund(refundedValue);

        Assert.Equal(PaymentStatus.Completed, refundedPayment.Status);
    }

    [Theory]
    [InlineData(PaymentStatus.Created)]
    [InlineData(PaymentStatus.Authorized)]
    [InlineData(PaymentStatus.Refunded)]
    [InlineData(PaymentStatus.Canceled)]
    public void RefundPayment_ShouldThrowInvalidOperationException_WhenStatusIsNotCapturedOrCompleted(PaymentStatus status)
    {
        var payment = PaymentInStatus(status);
        var refundedValue = Money.Of(10);

        var exception = Assert.Throws<InvalidOperationException>(() => payment.Refund(refundedValue));

        Assert.Contains("status", exception.Message);
    }

    [Fact]
    public void CompletePayment_ShouldSetStatusToCompleted_WhenStatusIsCaptured()
    {
        var payment = PaymentInStatus(PaymentStatus.Captured);

        var completedPayment = payment.Complete();

        Assert.Equal(PaymentStatus.Completed, completedPayment.Status);
    }

    [Theory]
    [InlineData(PaymentStatus.Created)]
    [InlineData(PaymentStatus.Authorized)]
    [InlineData(PaymentStatus.Completed)]
    [InlineData(PaymentStatus.Refunded)]
    [InlineData(PaymentStatus.Canceled)]
    public void CompletePayment_ShouldThrowInvalidOperationException_WhenStatusIsNotCaptured(PaymentStatus status)
    {
        var payment = PaymentInStatus(status);

        var exception = Assert.Throws<InvalidOperationException>(() => payment.Complete());

        Assert.Contains("status", exception.Message);
    }

    [Theory]
    [InlineData(PaymentStatus.Created)]
    [InlineData(PaymentStatus.Authorized)]
    public void CancelPayment_ShouldSetStatusToCanceled_WhenStatusIsCreatedOrAuthorized(PaymentStatus status)
    {
        var payment = PaymentInStatus(status);

        var canceledPayment = payment.Cancel();

        Assert.Equal(PaymentStatus.Canceled, canceledPayment.Status);
    }

    [Theory]
    [InlineData(PaymentStatus.Captured)]
    [InlineData(PaymentStatus.Completed)]
    [InlineData(PaymentStatus.Refunded)]
    [InlineData(PaymentStatus.Canceled)]
    public void CancelPayment_ShouldThrowInvalidOperationException_WhenStatusIsNotCreatedOrAuthorized(PaymentStatus status)
    {
        var payment = PaymentInStatus(status);

        var exception = Assert.Throws<InvalidOperationException>(() => payment.Cancel());

        Assert.Contains("status", exception.Message);
    }

    private static Payment PaymentInStatus(PaymentStatus status)
    {
        var created = Payment.Create(Money.Of(200), PaymentMethod.CreditCard, "Test", Guid.NewGuid());

        var payment = status switch
        {
            PaymentStatus.Created => created,
            PaymentStatus.Authorized => created.Authorize(),
            PaymentStatus.Captured => created.Authorize().Capture(Money.Of(200)),
            PaymentStatus.Completed => created.Authorize().Capture(Money.Of(200)).Complete(),
            PaymentStatus.Refunded => created.Authorize().Capture(Money.Of(200)).Refund(Money.Of(200)),
            PaymentStatus.Canceled => created.Cancel(),
            _ => throw new ArgumentOutOfRangeException(nameof(status))
        };

        Assert.Equal(status, payment.Status);

        return payment;
    }
}
