using Cosmetics.DTO.Payment;
using Cosmetics.Enum;
using Cosmetics.Models;
using Cosmetics.Repositories.UnitOfWork;
using Net.payOS;
using Net.payOS.Types;
using System.Globalization;

namespace Cosmetics.Service.Payment
{
	public class PayOSService : IPaymentService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly PayOS _payOS;

		public PayOSService(IUnitOfWork unitOfWork, PayOS payOS)
		{
			_unitOfWork = unitOfWork ?? throw new ArgumentException(nameof(unitOfWork));
			_payOS = payOS ?? throw new ArgumentNullException(nameof(payOS));
		}

		public async Task<string> CreatePaymentUrlAsync(PaymentRequestDTO request)
		{
			var order = await _unitOfWork.Orders.GetInformationById(request.OrderId);
			if (order == null)
			{
				throw new Exception($"Order with ID {request.OrderId} not found");
			}

			if(order.TotalAmount <= 0)
			{
				throw new Exception("Order amount must be greater than zero");
			}


			var orderCode = DateTime.UtcNow.Ticks % 1000000000;

			var transactionId = orderCode.ToString();

			var paymentTransaction = new PaymentTransaction
			{
				PaymentTransactionId = Guid.NewGuid(),
				OrderId = request.OrderId,
				PaymentMethod = order.PaymentMethod,
				Amount = order.TotalAmount ?? 0,
				Status = PaymentStatus.Pending,
				TransactionDate = DateTime.UtcNow,
				TransactionId = transactionId,
			};

			await _unitOfWork.PaymentTransactions.AddAsync(paymentTransaction);
			await _unitOfWork.CompleteAsync();

			var orderCodeStr = orderCode.ToString();
			var maxCodeLength = Math.Min(orderCodeStr.Length, 14); // Để lại chỗ cho "Thanh toan " (11 ký tự)
			var shortDescription = $"Thanh toan {orderCodeStr.Substring(0, maxCodeLength)}";
			if (shortDescription.Length > 25)
			{
				shortDescription = shortDescription.Substring(0, 25);
			}

			var items = order.OrderDetails.Select(od =>
				new ItemData(
					od.Product.Name,
					od.Quantity,
					(int)((od.UnitPrice ?? 0) * od.Quantity)
				)
			).ToList();

			var paymentData = new PaymentData(
			orderCode: orderCode,
			amount: (int)order.TotalAmount!,
			description: shortDescription,
			items: items,
			returnUrl: "https://cosmotopia.vercel.app/HandlePayment",
			cancelUrl: "https://cosmotopia.vercel.app/HandlePayment"
            );

			var paymentResponse = await _payOS.createPaymentLink(paymentData);
			return paymentResponse.checkoutUrl;
		}

		public async Task<PaymentResponseDTO> GetPaymentByTransactionIdAsync(string transactionId)
		{
			var paymentEntity = await _unitOfWork.PaymentTransactions.GetByTransactionIdAsync(transactionId);
			if (paymentEntity == null)
				return null;

			return new PaymentResponseDTO
			{
				OrderId = paymentEntity.OrderId,
				TransactionId = paymentEntity.TransactionId,
				Amount = paymentEntity.Amount,
				Status = paymentEntity.Status,
				ResultCode = paymentEntity.ResultCode.HasValue ? paymentEntity.ResultCode.Value : 0, // Default to 0 if null
				ResponseTime = string.IsNullOrEmpty(paymentEntity.ResponseTime)
					? DateTime.UtcNow // Default to current time if null
					: DateTime.ParseExact(paymentEntity.ResponseTime, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
			};
		}

		public async Task<bool> HandlePaymentResponseAsync(PaymentResponseDTO response)
		{
			var transaction = await _unitOfWork.PaymentTransactions.GetByTransactionIdAsync(response.TransactionId);
			if(transaction == null)
			{
				return false;
			}

			transaction.Status = response.Status;
			transaction.ResultCode = response.ResultCode;
			transaction.ResponseTime = response.ResponseTime.ToString("yyyy-MM-dd HH:mm:ss");
			transaction.Amount = response.Amount;
			transaction.RequestId = response.RequestId;

			await _unitOfWork.PaymentTransactions.UpdateAsync(transaction);
			await _unitOfWork.CompleteAsync();

			return true;
		}

		public async Task<bool> UpdatePaymentStatusAsync(PaymentResponseDTO payment)
		{
			var paymentEntity = await _unitOfWork.PaymentTransactions.GetByTransactionIdAsync(payment.TransactionId);
			if (paymentEntity == null)
			{
				return false;
			}

			if (paymentEntity.Status != PaymentStatus.Pending)
			{
				return false;
			}

			if (payment.Status != PaymentStatus.Success && payment.Status != PaymentStatus.Failed)
			{
				return false;
			}

			paymentEntity.Status = payment.Status;
			paymentEntity.ResponseTime = payment.ResponseTime.ToString("yyyy-MM-dd HH:mm:ss");

			await _unitOfWork.PaymentTransactions.UpdateAsync(paymentEntity);
			await _unitOfWork.CompleteAsync();

			return true;
		}
	}
}
