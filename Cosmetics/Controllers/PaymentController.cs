using AutoMapper;
using Cosmetics.DTO.Payment;
using Cosmetics.Enum;
using Cosmetics.Repositories.UnitOfWork;
using Cosmetics.Service.Payment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Net.payOS;

namespace Cosmetics.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class PaymentController : ControllerBase
	{
		private readonly IPaymentService _paymentService;
		private readonly IUnitOfWork _unitOfWork;
		private readonly PayOS _payOS;
		private readonly IMapper _mapper;

		public PaymentController(
			IPaymentService paymentService,
			IUnitOfWork unitOfWork,
			PayOS payOS,
			IMapper mapper)
		{
			_paymentService = paymentService ?? throw new ArgumentNullException(nameof(paymentService));
			_unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
			_payOS = payOS ?? throw new ArgumentNullException(nameof(payOS));
			_mapper = mapper;
		}

		[HttpPost("create-payment-link")]
		public async Task<IActionResult> CreatePayment([FromBody] PaymentRequestDTO request)
		{
			if (!ModelState.IsValid)
			{
				return BadRequest(ModelState);
			}

			try
			{
				var paymentUrl = await _paymentService.CreatePaymentUrlAsync(request);
				return Ok(new { PaymentUrl = paymentUrl });
			}
			catch (Exception ex)
			{
				return StatusCode(500, $"An error occurred while creating the payment URL: {ex.Message}");
			}
		}

		[HttpGet("payment/{transactionId}")]
		public async Task<IActionResult> GetPayment(string transactionId)
		{
			if (transactionId == string.Empty)
				return BadRequest("Transaction ID is required");

			try
			{
				var payment = await _paymentService.GetPaymentByTransactionIdAsync(transactionId);
				if (payment == null)
					return NotFound($"No payment found with Transaction ID: {transactionId}");

				return Ok(payment);
			}
			catch (Exception ex)
			{
				// Log the exception if you have a logging service
				return StatusCode(500, $"An error occurred while retrieving the payment: {ex.Message}");
			}
		}

		[HttpPut("update-payment-status/{transactionId}")]
		public async Task<IActionResult> UpdatePaymentStatus(string transactionId, [FromQuery] int newStatus)
		{
			if (string.IsNullOrEmpty(transactionId))
				return BadRequest("Transaction ID is required");

			// Kiểm tra status đầu vào
			if (!System.Enum.IsDefined(typeof(PaymentStatus), newStatus) ||
				(newStatus != (int)PaymentStatus.Success && newStatus != (int)PaymentStatus.Failed))
			{
				return BadRequest("New status must be either Success (1) or Fail (2)");
			}

			try
			{
				var payment = await _paymentService.GetPaymentByTransactionIdAsync(transactionId);
				if (payment == null)
					return NotFound($"No payment found with Transaction ID: {transactionId}");

				if (payment.Status != PaymentStatus.Pending)
					return BadRequest($"Payment status can only be updated from Pending (0) to Success (1) or Fail (2). Current status: {payment.Status}");

				var updatedPayment = new PaymentResponseDTO
				{
					TransactionId = payment.TransactionId,
					Amount = payment.Amount,
					ResultCode = payment.ResultCode,
					ResponseTime = DateTime.UtcNow,
					Status = (PaymentStatus)newStatus // Sử dụng status từ request
				};

				var success = await _paymentService.UpdatePaymentStatusAsync(updatedPayment);
				if (!success)
					return BadRequest($"Failed to update payment status. Either the status is invalid or the transaction cannot be updated.");
				var OrderId = payment.OrderId;
				if (OrderId != null)
				{
					var order = await _unitOfWork.Orders.GetByIdAsync(OrderId);

					if (order == null) return NotFound();
					if (order == null)
						return NotFound($"No order found for this payment with Transaction ID: {payment.TransactionId}");

					// Cập nhật trạng thái đơn hàng dựa vào trạng thái thanh toán
					if (updatedPayment.Status == PaymentStatus.Success)
					{
						order.Status = OrderStatus.Paid;
					}
					else
					{
						order.Status = OrderStatus.Cancelled;
					}

					await _unitOfWork.Orders.UpdateAsync(order);
					await _unitOfWork.CompleteAsync();
				}
				string statusMessage = updatedPayment.Status == PaymentStatus.Success
					? "Success (1)"
					: "Fail (2)";
				return Ok(new
				{
					Message = $"Payment status updated to {statusMessage} for Transaction ID: {transactionId}",
					UpdatedStatus = updatedPayment.Status,
					ResponseTime = updatedPayment.ResponseTime
				});
			}
			catch (Exception ex)
			{
				return StatusCode(500, $"An error occurred while updating the payment status: {ex.Message}");
			}
		}

		[HttpDelete("{id}")]
		[Authorize]
		public async Task<IActionResult> DeleteOrder(Guid id)
		{
			var order = await _unitOfWork.Orders.GetByIdAsync(id);
			if (order == null) return NotFound();

			_unitOfWork.Orders.Delete(order);
			await _unitOfWork.CompleteAsync();
			return NoContent();
		}
	}
}