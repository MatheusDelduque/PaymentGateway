using PaymentGateway.Domain.Enums;
using System.Diagnostics.Contracts;

namespace PaymentGateway.Domain
{
    public class Payment
    {
        public Money Value { get; }
        public Money CapturedValue { get; }
        public Money RefundedValue { get; }
        public PaymentMethod PaymentMethod { get; }
        public DateTime PaymentDate { get; }
        public PaymentStatus Status { get; }
        public string Description { get; }
        public Guid TransactionId { get; }

        private Payment(Money value, Money capturedValue, Money refundedValue, PaymentMethod paymentMethod
            , DateTime paymentDate, PaymentStatus status, string description, Guid transactionId)
        {
            Value = value;
            CapturedValue = capturedValue;
            RefundedValue = refundedValue;
            PaymentMethod = paymentMethod;
            PaymentDate = paymentDate;
            Status = status;
            Description = description;
            TransactionId = transactionId;
        }

        public static Payment Create(Money value, PaymentMethod paymentMethod, string description, Guid transactionId)
        {
            if (value.Amount <= 0)
                throw new ArgumentException("O valor do pagamento deve ser maior que zero.", nameof(value));

            if (paymentMethod != PaymentMethod.CreditCard)
                throw new ArgumentException("O método de pagamento deve ser cartão de crédito.", nameof(paymentMethod));

            if (transactionId == Guid.Empty)
                throw new ArgumentException("O ID da transação não pode ser vazio.", nameof(transactionId));

            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("A descrição do pagamento não pode ser nula ou vazia.", nameof(description));

            return new Payment(value, Money.Zero, Money.Zero, paymentMethod, DateTime.UtcNow, PaymentStatus.Created, description, transactionId);
        }

        [Pure]
        public Payment Authorize()
        {
            if (Status != PaymentStatus.Created)
                throw new InvalidOperationException("O pagamento só pode ser autorizado se estiver no status 'Created'.");

            return new Payment(Value, Money.Zero, Money.Zero, PaymentMethod, DateTime.UtcNow, PaymentStatus.Authorized, Description, TransactionId);
        }

        public Payment Capture(Money capturedValue)
        {
            if (Status != PaymentStatus.Authorized)
                throw new InvalidOperationException("O pagamento só pode ser capturado se estiver no status 'Authorized'.");

            if (capturedValue.Amount <= 0)
                throw new ArgumentException("O valor capturado do pagamento deve ser maior que zero.", nameof(capturedValue));

            if (capturedValue > Value)
                throw new InvalidOperationException("O valor capturado não pode ser maior que o valor do pagamento.");

            return new Payment(Value, capturedValue, RefundedValue, PaymentMethod, DateTime.UtcNow, PaymentStatus.Captured, Description, TransactionId);
        }

        public Payment Refund(Money refundedValue)
        {
            if (Status != PaymentStatus.Completed && Status != PaymentStatus.Captured)
                throw new InvalidOperationException("O pagamento só pode ser reembolsado se estiver no status 'Completed' ou 'Captured'.");
            
            if (refundedValue.Amount <= 0) 
                throw new ArgumentException("O valor reembolsado do pagamento deve ser maior que zero.", nameof(refundedValue));

            var newRefundedValue = RefundedValue + refundedValue;

            if (newRefundedValue > CapturedValue)
                throw new InvalidOperationException("O valor reembolsado não pode ser maior que o valor capturado.");

            if (newRefundedValue != CapturedValue)
                return new Payment(Value, CapturedValue, newRefundedValue, PaymentMethod, DateTime.UtcNow, Status, Description, TransactionId);

            return new Payment(Value, CapturedValue, newRefundedValue, PaymentMethod, DateTime.UtcNow, PaymentStatus.Refunded, Description, TransactionId);
        }

        public Payment Complete()
        {
            if (Status != PaymentStatus.Captured)
                throw new InvalidOperationException("O pagamento só pode ser concluído se estiver no status 'Captured'.");

            return new Payment(Value, CapturedValue, RefundedValue, PaymentMethod, DateTime.UtcNow, PaymentStatus.Completed, Description, TransactionId);
        }

        public Payment Cancel()
        {
            if (Status != PaymentStatus.Created && Status != PaymentStatus.Authorized)
                throw new InvalidOperationException("O pagamento só pode ser cancelado se estiver no status 'Created' ou 'Authorized'.");
            return new Payment(Value, CapturedValue, RefundedValue, PaymentMethod, DateTime.UtcNow, PaymentStatus.Canceled, Description, TransactionId);
        }

    }
}
