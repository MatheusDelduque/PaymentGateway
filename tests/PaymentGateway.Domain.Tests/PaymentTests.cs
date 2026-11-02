using PaymentGateway.Domain;
using PaymentGateway.Domain.Enums;

namespace PaymentGateway.Domain.Tests;

public class PaymentTests
{
    [Fact]
    public void CreatePayment_ShouldThrowArgumentException_WhenValueIsZero()
    {
        var value = Money.Of(0);

        Assert.Throws<ArgumentException>(() => Payment.Create(value, PaymentMethod.CreditCard, "Test", Guid.NewGuid()));
    }

    [Fact]
    public void CreatePayment_ShouldThrowArgumentException_WhenTransactionIdIsEmpty()
    {
        var value = Money.Of(100);

        Assert.Throws<ArgumentException>(() => Payment.Create(value, PaymentMethod.CreditCard, "Test", Guid.Empty));
    }

    [Fact]
    public void CreatePayment_ShouldThrowArgumentException_WhenDescriptionIsNullOrEmpty()
    {
        var value = Money.Of(100);

        Assert.Throws<ArgumentException>(() => Payment.Create(value, PaymentMethod.CreditCard, null, Guid.NewGuid()));
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
    public void AuthorizePayment_ShouldThrowInvalidOperationException_WhenAlreadyAuthorized()
    {
        var payment = Payment.Create(Money.Of(100), PaymentMethod.CreditCard, "Test", Guid.NewGuid());
        var authorizedPayment = payment.Authorize();

        Assert.Throws<InvalidOperationException>(() => authorizedPayment.Authorize());
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

    [Fact]
    public void CapturePayment_ShouldThrowInvalidOperationException_WhenAlreadyCaptured()
    {
        var payment = Payment.Create(Money.Of(200), PaymentMethod.CreditCard, "Test", Guid.NewGuid());
        var authorizedPayment = payment.Authorize();
        var capturedPayment = authorizedPayment.Capture(Money.Of(150));
        var secondCapturedValue = Money.Of(50);

        Assert.Throws<InvalidOperationException>(() => capturedPayment.Capture(secondCapturedValue));
    }

    [Fact]
    public void CapturePayment_ShouldThrowInvalidOperationException_WhenCapturedStatusIsDifferentOfExpected()
    {
        var payment = Payment.Create(Money.Of(200), PaymentMethod.CreditCard, "Test", Guid.NewGuid());
        var capturedValue = Money.Of(200);

        Assert.Throws<InvalidOperationException>(() => payment.Capture(capturedValue));
    }

    [Fact]
    public void CapturePayment_ShouldArgumentException_WhenCapturedValueIsZero()
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

}
