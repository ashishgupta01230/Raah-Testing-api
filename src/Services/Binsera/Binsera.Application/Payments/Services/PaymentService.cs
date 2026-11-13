using Binsera.Core.Auth.Domain;
using Binsera.Core.Members.Domain;
using Binsera.Core.Payments.Contracts;
using Binsera.Core.Payments.Domain;
using Binsera.Core.Payments.Enums;
using Binsera.Core.Payments.Models;
using HautSphere.BuildingBlocks.Application.Attributes;
using HautSphere.BuildingBlocks.Domain.Models;
using HautSphere.BuildingBlocks.Persistence.Contracts;
using HautSphere.BuildingBlocks.Persistence.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Binsera.Application.Payments.Services
{
    [AutoRegister(includeInterfaces: true)]
    public class PaymentService(IUnitOfWork unitOfWork) : IPaymentService
    {
        private readonly IRepository<Payment> _paymentRepository = unitOfWork.Repository<Payment>();
        private readonly IRepository<Member> _memberRepository = unitOfWork.Repository<Member>();
        private readonly IRepository<UserLogin> _userRepository = unitOfWork.Repository<UserLogin>();

        public async Task<long> CreatePaymentAsync(CreatePaymentRequest request, long memberId)
        {
            var paymentNo = await GeneratePaymentNoAsync();
            var payment = Payment.Create(request, memberId, paymentNo);

            await _paymentRepository.Save(payment);
            return payment.Id;
        }

        public async Task VerifyPaymentAsync(long paymentId, long adminId, VerifyPaymentRequest request)
        {
            var payment = await _paymentRepository.Table
                .FirstOrDefaultAsync(x => x.Id == paymentId);

            if (payment is null)
                throw new KeyNotFoundException($"Payment with id {paymentId} not found.");

            if (payment.Status != PaymentStatus.Pending)
                throw new InvalidOperationException("Only pending payments can be verified.");

            payment.Verify(adminId, request.Remarks);
            await _paymentRepository.Save(payment);
        }

        public async Task RejectPaymentAsync(long paymentId, long adminId, RejectPaymentRequest request)
        {
            var payment = await _paymentRepository.Table
                .FirstOrDefaultAsync(x => x.Id == paymentId);

            if (payment is null)
                throw new KeyNotFoundException($"Payment with id {paymentId} not found.");

            if (payment.Status != PaymentStatus.Pending)
                throw new InvalidOperationException("Only pending payments can be rejected.");

            payment.Reject(adminId, request.Reason);
            await _paymentRepository.Save(payment);
        }

        public async Task CancelPaymentAsync(long paymentId, long memberId)
        {
            var payment = await _paymentRepository.Table
                .FirstOrDefaultAsync(x => x.Id == paymentId && x.MemberId == memberId);

            if (payment is null)
                throw new KeyNotFoundException($"Payment with id {paymentId} not found.");

            if (payment.Status != PaymentStatus.Pending)
                throw new InvalidOperationException("Only pending payments can be cancelled.");

            payment.Cancel(memberId, "Cancelled by member");
            await _paymentRepository.Save(payment);
        }

        public async Task CancelPaymentByReferenceAsync(ReferenceType referenceType, long referenceId, string reason)
        {
            var payment = await _paymentRepository.Table
                .FirstOrDefaultAsync(x =>
                    x.ReferenceType == referenceType &&
                    x.ReferenceId == referenceId &&
                    x.Status == PaymentStatus.Pending);

            if (payment is null)
                return;

            payment.Cancel(payment.MemberId, reason);
            await _paymentRepository.Save(payment);
        }

        public async Task VerifyPaymentByReferenceAsync(ReferenceType referenceType, long referenceId, long adminId, string remarks)
        {
            var payment = await _paymentRepository.Table
                .FirstOrDefaultAsync(x =>
                    x.ReferenceType == referenceType &&
                    x.ReferenceId == referenceId &&
                    x.Status == PaymentStatus.Pending);

            if (payment is null)
                return;

            payment.Verify(adminId, remarks);
            await _paymentRepository.Save(payment);
        }

        public async Task<PaymentDetailResponse> GetPaymentAsync(long paymentId, long memberId)
        {
            var payment = await _paymentRepository.TableNoTracking
                .Include(x => x.History)
                .FirstOrDefaultAsync(x => x.Id == paymentId && x.MemberId == memberId);

            if (payment is null)
                throw new KeyNotFoundException($"Payment with id {paymentId} not found.");

            return await MapToDetailResponse(payment);
        }

        public async Task<PaymentDetailResponse> GetPaymentByReferenceAsync(
            ReferenceType referenceType, long referenceId, long memberId)
        {
            var payment = await _paymentRepository.TableNoTracking
                .Include(x => x.History)
                .FirstOrDefaultAsync(x =>
                    x.ReferenceType == referenceType &&
                    x.ReferenceId == referenceId);

            if (payment is null)
                throw new KeyNotFoundException(
                    $"Payment not found for {referenceType} with reference id {referenceId}.");

            return await MapToDetailResponse(payment);
        }

        public async Task UpdateGatewayStatusAsync(long paymentId, UpdateGatewayStatusRequest request)
        {
            var payment = await _paymentRepository.Table
                .FirstOrDefaultAsync(x => x.Id == paymentId);

            if (payment is null)
                throw new KeyNotFoundException($"Payment with id {paymentId} not found.");

            payment.UpdateGatewayStatus(
                request.GatewayTransactionId, request.Status, request.Remarks);
            await _paymentRepository.Save(payment);
        }

        public async Task<IPagedList<PaymentListResponse>> GetMemberPaymentsAsync(
            long memberId, PaymentListRequest request, CancellationToken cancellationToken)
        {
            var query = _paymentRepository.TableNoTracking
                .Where(x => x.MemberId == memberId);

            query = ApplyFilters(query, request.ReferenceType, request.Status,
                request.FromDate, request.ToDate);

            var responseQuery = query.Select(payment => new PaymentListResponse
            {
                Id = payment.Id,
                PaymentNo = payment.PaymentNo,
                ReferenceType = payment.ReferenceType,
                ReferenceId = payment.ReferenceId,
                Amount = payment.Amount,
                PaymentMode = payment.PaymentMode,
                PaymentMethod = payment.PaymentMethod,
                Status = payment.Status,
                PaidOn = payment.PaidOn,
                CreatedDate = payment.CreatedDate
            });

            return await responseQuery
                .OrderByDescending(x => x.CreatedDate)
                .ToPagedListAsync(request.Page, request.PageSize, cancellationToken);
        }

        public async Task<IPagedList<AdminPaymentListResponse>> GetAdminPaymentsAsync(
            AdminPaymentListRequest request, CancellationToken cancellationToken)
        {
            var paymentQuery = _paymentRepository.TableNoTracking.AsQueryable();
            var memberQuery = _memberRepository.TableNoTracking;

            paymentQuery = ApplyFilters(paymentQuery, request.ReferenceType,
                request.Status, request.FromDate, request.ToDate);

            if (request.PaymentMode.HasValue)
                paymentQuery = paymentQuery.Where(x => x.PaymentMode == request.PaymentMode.Value);

            var query = from payment in paymentQuery
                        join member in memberQuery on payment.MemberId equals member.Id
                        select new AdminPaymentListResponse
                        {
                            Id = payment.Id,
                            PaymentNo = payment.PaymentNo,
                            MemberId = member.Id,
                            RegistrationNumber = member.LoginID,
                            MemberName = (member.FName ?? "") + " " + (member.LName ?? ""),
                            Mobile = member.Mobile,
                            ReferenceType = payment.ReferenceType,
                            ReferenceId = payment.ReferenceId,
                            Amount = payment.Amount,
                            PaymentMode = payment.PaymentMode,
                            PaymentMethod = payment.PaymentMethod,
                            Status = payment.Status,
                            TransactionReference = payment.TransactionReference,
                            PaidOn = payment.PaidOn,
                            CreatedDate = payment.CreatedDate
                        };

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var search = request.Search.Trim().ToLower();
                query = query.Where(x =>
                    x.MemberName.ToLower().Contains(search) ||
                    x.PaymentNo.ToLower().Contains(search) ||
                    (x.Mobile != null && x.Mobile.ToLower().Contains(search)));
            }

            return await query.OrderByDescending(x => x.CreatedDate)
                .ToPagedListAsync(request.Page, request.PageSize, cancellationToken);
        }

        public async Task<AdminPaymentDetailResponse> GetAdminPaymentDetailsAsync(long paymentId)
        {
            var payment = await _paymentRepository.TableNoTracking
                .Include(x => x.History)
                .FirstOrDefaultAsync(x => x.Id == paymentId);

            if (payment is null)
                throw new KeyNotFoundException($"Payment with id {paymentId} not found.");

            var member = await _memberRepository.TableNoTracking
                .FirstOrDefaultAsync(x => x.Id == payment.MemberId);

            var createdByUser = await _userRepository.TableNoTracking
                .FirstOrDefaultAsync(x => x.Id == payment.CreatedBy);

            var verifiedByUser = payment.VerifiedBy.HasValue
                ? await _userRepository.TableNoTracking
                    .FirstOrDefaultAsync(x => x.Id == payment.VerifiedBy.Value)
                : null;

            var historyResponses = payment.History
                .OrderBy(x => x.ChangedOn)
                .Select(h => new PaymentHistoryResponse
                {
                    FromStatus = h.FromStatus,
                    ToStatus = h.ToStatus,
                    Remarks = h.Remarks,
                    ChangedOn = h.ChangedOn,
                    ChangedByName = null
                }).ToList();

            var changedUserIds = payment.History
                .Where(x => x.ChangedBy > 0)
                .Select(x => x.ChangedBy)
                .Distinct()
                .ToList();

            if (changedUserIds.Count > 0)
            {
                var users = await _userRepository.TableNoTracking
                    .Where(x => changedUserIds.Contains(x.Id))
                    .ToDictionaryAsync(x => x.Id, x => x.UserName);

                foreach (var h in historyResponses)
                {
                    var matchingHistory = payment.History
                        .FirstOrDefault(x =>
                            x.ChangedOn == h.ChangedOn &&
                            x.ToStatus == h.ToStatus &&
                            x.Remarks == h.Remarks);

                    if (matchingHistory != null && users.ContainsKey(matchingHistory.ChangedBy))
                        h.ChangedByName = users[matchingHistory.ChangedBy];
                }
            }

            return new AdminPaymentDetailResponse
            {
                Id = payment.Id,
                PaymentNo = payment.PaymentNo,
                ReferenceType = payment.ReferenceType,
                ReferenceId = payment.ReferenceId,
                Amount = payment.Amount,
                PaymentMode = payment.PaymentMode,
                PaymentMethod = payment.PaymentMethod,
                Status = payment.Status,
                TransactionReference = payment.TransactionReference,
                GatewayTransactionId = payment.GatewayTransactionId,
                MediaFileId = payment.MediaFileId,
                MediaFileUrl = payment.MediaFileId.HasValue
                    ? $"/api/media/view/{payment.MediaFileId}" : null,
                Remarks = payment.Remarks,
                PaidOn = payment.PaidOn,
                VerifiedOn = payment.VerifiedOn,
                CreatedDate = payment.CreatedDate,

                Member = member != null ? new PaymentMemberInfo
                {
                    Id = member.Id,
                    RegNo = member.RegNo,
                    MemberName = (member.FName ?? "") + " " + (member.LName ?? ""),
                    Mobile = member.Mobile,
                    Email = member.EmailID
                } : null,

                History = historyResponses,

                Audit = new AdminPaymentAuditInfo
                {
                    CreatedByName = createdByUser?.UserName,
                    VerifiedByName = verifiedByUser?.UserName
                }
            };
        }

        private static IQueryable<Payment> ApplyFilters(IQueryable<Payment> query,
            ReferenceType? referenceType, PaymentStatus? status,
            DateTime? fromDate, DateTime? toDate)
        {
            if (referenceType.HasValue)
                query = query.Where(x => x.ReferenceType == referenceType.Value);

            if (status.HasValue)
                query = query.Where(x => x.Status == status.Value);

            if (fromDate.HasValue)
                query = query.Where(x => x.CreatedDate >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(x => x.CreatedDate <= toDate.Value);

            return query;
        }

        private async Task<PaymentDetailResponse> MapToDetailResponse(Payment payment)
        {
            var verifiedByUser = payment.VerifiedBy.HasValue
                ? await _userRepository.TableNoTracking
                    .FirstOrDefaultAsync(x => x.Id == payment.VerifiedBy.Value)
                : null;

            var historyResponses = payment.History
                .OrderBy(x => x.ChangedOn)
                .Select(h => new PaymentHistoryResponse
                {
                    FromStatus = h.FromStatus,
                    ToStatus = h.ToStatus,
                    Remarks = h.Remarks,
                    ChangedOn = h.ChangedOn,
                    ChangedByName = null
                }).ToList();

            var changedUserIds = payment.History
                .Where(x => x.ChangedBy > 0)
                .Select(x => x.ChangedBy)
                .Distinct()
                .ToList();

            if (changedUserIds.Count > 0)
            {
                var users = await _userRepository.TableNoTracking
                    .Where(x => changedUserIds.Contains(x.Id))
                    .ToDictionaryAsync(x => x.Id, x => x.UserName);

                foreach (var h in historyResponses)
                {
                    var matchingHistory = payment.History
                        .FirstOrDefault(x =>
                            x.ChangedOn == h.ChangedOn &&
                            x.ToStatus == h.ToStatus &&
                            x.Remarks == h.Remarks);

                    if (matchingHistory != null && users.ContainsKey(matchingHistory.ChangedBy))
                        h.ChangedByName = users[matchingHistory.ChangedBy];
                }
            }

            return new PaymentDetailResponse
            {
                Id = payment.Id,
                PaymentNo = payment.PaymentNo,
                ReferenceType = payment.ReferenceType,
                ReferenceId = payment.ReferenceId,
                Amount = payment.Amount,
                PaymentMode = payment.PaymentMode,
                PaymentMethod = payment.PaymentMethod,
                Status = payment.Status,
                TransactionReference = payment.TransactionReference,
                GatewayTransactionId = payment.GatewayTransactionId,
                MediaFileId = payment.MediaFileId,
                MediaFileUrl = payment.MediaFileId.HasValue
                    ? $"/api/media/view/{payment.MediaFileId}" : null,
                Remarks = payment.Remarks,
                PaidOn = payment.PaidOn,
                VerifiedOn = payment.VerifiedOn,
                VerifiedByName = verifiedByUser?.UserName,
                CreatedDate = payment.CreatedDate,
                History = historyResponses
            };
        }

        private async Task<string> GeneratePaymentNoAsync()
        {
            var today = DateTime.UtcNow.ToString("yyyyMMdd");
            var prefix = $"PAY{today}";
            var lastPayment = await _paymentRepository.TableNoTracking
                .Where(x => x.PaymentNo.StartsWith(prefix))
                .OrderByDescending(x => x.PaymentNo)
                .FirstOrDefaultAsync();

            var sequence = lastPayment != null
                ? int.Parse(lastPayment.PaymentNo.Substring(prefix.Length)) + 1
                : 1;

            return $"{prefix}{sequence:D4}";
        }
    }
}
